using System.Text.RegularExpressions;

/// <summary>
/// A node in a parsed Unity YAML document: a mapping, a list, or a scalar string.
/// Lookups on missing keys return <see cref="Missing"/> instead of throwing, so chains like
/// <c>doc["m_itemData"]["m_shared"]["m_name"].Str</c> are safe.
/// </summary>
sealed class YNode
{
    public static readonly YNode Missing = new(null);

    static readonly Regex ReferencePattern = new(@"fileID: (-?\d+), guid: ([0-9a-f]+)");

    readonly object? raw;

    public YNode(object? raw) => this.raw = raw;

    public bool Exists => raw is not null;
    public YNode this[string key] => raw is Dictionary<string, YNode> map && map.TryGetValue(key, out var node) ? node : Missing;
    public bool Has(string key) => raw is Dictionary<string, YNode> map && map.ContainsKey(key);
    public string Str => raw as string ?? "";
    public double Num => double.TryParse(Str, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : 0;
    public int Int => (int)Num;
    public bool Bool => Num != 0;
    public IReadOnlyList<YNode> Items => raw as List<YNode> ?? [];
    public IEnumerable<KeyValuePair<string, YNode>> Entries => raw as Dictionary<string, YNode> ?? [];

    /// <summary>For a `{fileID: N, guid: G, type: 2}` reference, the file id and guid; null if empty or not a reference.</summary>
    public (string FileId, string Guid)? Reference
    {
        get
        {
            var match = ReferencePattern.Match(Str);
            return match.Success ? (match.Groups[1].Value, match.Groups[2].Value) : null;
        }
    }
}

/// <summary>Parser for the regular subset of YAML that Unity writes (2-space maps, lists at key indent, inline `{}` refs).</summary>
static class UnityYaml
{
    static readonly Regex DocumentSplit = new(@"^--- !u!\d+ &(-?\d+)[^\n]*\n", RegexOptions.Multiline);
    static readonly Regex KeyLine = new(@"^([^\s:{\[][^:]*?):(?: (.*))?$");

    /// <summary>Parses a whole .prefab/.asset file into its documents, keyed by fileID. Each value is the document's body (the node under `MonoBehaviour:`, `GameObject:` etc).</summary>
    public static Dictionary<string, YNode> ParseFile(string text)
    {
        var documents = new Dictionary<string, YNode>();
        var matches = DocumentSplit.Matches(text);
        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var root = ParseDocument(text[start..end]);
            var body = root.Entries.FirstOrDefault().Value ?? YNode.Missing;
            documents[matches[i].Groups[1].Value] = body;
        }
        return documents;
    }

    static YNode ParseDocument(string body)
    {
        var lines = new List<(int Indent, string Text)>();
        foreach (var raw in body.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var trimmed = line.TrimStart(' ');
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }
            lines.Add((line.Length - trimmed.Length, trimmed));
        }
        if (lines.Count == 0)
        {
            return YNode.Missing;
        }
        var pos = 0;
        return ParseBlock(lines, ref pos, lines[0].Indent);
    }

    static YNode ParseBlock(List<(int Indent, string Text)> lines, ref int pos, int indent) =>
        IsListItem(lines[pos].Text) ? ParseList(lines, ref pos, indent) : ParseMap(lines, ref pos, indent);

    static bool IsListItem(string text) => text == "-" || text.StartsWith("- ");

    static YNode ParseMap(List<(int Indent, string Text)> lines, ref int pos, int indent)
    {
        var map = new Dictionary<string, YNode>();
        while (pos < lines.Count && lines[pos].Indent == indent && !IsListItem(lines[pos].Text))
        {
            var match = KeyLine.Match(lines[pos].Text);
            pos++;
            if (!match.Success)
            {
                continue; // wrapped scalar continuation; nothing we read uses these
            }

            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value.Trim();
            if (value.Length > 0)
            {
                map[key] = new YNode(value);
            }
            else if (pos < lines.Count && lines[pos].Indent > indent)
            {
                map[key] = ParseBlock(lines, ref pos, lines[pos].Indent);
            }
            else if (pos < lines.Count && lines[pos].Indent == indent && IsListItem(lines[pos].Text))
            {
                map[key] = ParseList(lines, ref pos, indent);
            }
            else
            {
                map[key] = new YNode("");
            }
        }
        return new YNode(map);
    }

    static YNode ParseList(List<(int Indent, string Text)> lines, ref int pos, int indent)
    {
        var items = new List<YNode>();
        while (pos < lines.Count && lines[pos].Indent == indent && IsListItem(lines[pos].Text))
        {
            var rest = lines[pos].Text.Length > 2 ? lines[pos].Text[2..] : "";
            if (!rest.StartsWith('{') && KeyLine.IsMatch(rest))
            {
                // "- key: value" opens a mapping whose further keys sit two columns right of the dash.
                lines[pos] = (indent + 2, rest);
                items.Add(ParseMap(lines, ref pos, indent + 2));
            }
            else
            {
                items.Add(new YNode(rest.Trim()));
                pos++;
            }
        }
        return new YNode(items);
    }
}

/// <summary>A parsed prefab file: every component document in it, keyed by fileID.</summary>
sealed class Prefab(string path, Dictionary<string, YNode> documents)
{
    public string Id { get; } = Path.GetFileNameWithoutExtension(path);
    public string FilePath { get; } = path;
    public Dictionary<string, YNode> Documents { get; } = documents;

    /// <summary>The first component that has all of the given top-level fields, which identifies its script without needing script GUIDs.</summary>
    public YNode? Component(params string[] requiredKeys) =>
        Documents.Values.FirstOrDefault(doc => requiredKeys.All(doc.Has));
}

/// <summary>Resolves Unity guid references to parsed prefabs and ScriptableObject assets (both are loaded as <see cref="Prefab"/>s).</summary>
sealed class PrefabIndex
{
    readonly Dictionary<string, string> pathsByGuid = new();
    readonly Dictionary<string, Prefab> loaded = new();

    public PrefabIndex(string assetsDir)
    {
        var metas = Directory.EnumerateFiles(assetsDir, "*.prefab.meta", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(assetsDir, "GameElements"), "*.asset.meta", SearchOption.AllDirectories));
        foreach (var meta in metas)
        {
            foreach (var line in File.ReadLines(meta))
            {
                if (line.StartsWith("guid: "))
                {
                    pathsByGuid[line[6..].Trim()] = meta[..^".meta".Length];
                    break;
                }
            }
        }
        Console.WriteLine($"Indexed {pathsByGuid.Count} prefabs and assets");
    }

    public Prefab Load(string path)
    {
        if (!loaded.TryGetValue(path, out var prefab))
        {
            prefab = new Prefab(path, UnityYaml.ParseFile(File.ReadAllText(path)));
            loaded[path] = prefab;
        }
        return prefab;
    }

    /// <summary>The prefab a reference points at, plus the specific component document if its fileID names one.</summary>
    public (Prefab Prefab, YNode? Document)? Resolve(YNode reference)
    {
        if (reference.Reference is not var (fileId, guid) || !pathsByGuid.TryGetValue(guid, out var path))
        {
            return null;
        }
        var prefab = Load(path);
        return (prefab, prefab.Documents.GetValueOrDefault(fileId));
    }
}
