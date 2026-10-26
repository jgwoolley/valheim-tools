using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

/// <summary>Everything the extractors share: the prefab index, English text and enum names.</summary>
sealed class Context
{
    public string AssetsDir { get; }
    public PrefabIndex Prefabs { get; }
    public GameEnums Enums { get; }
    readonly Dictionary<string, string> localization;

    public Context(string assetsDir, string? gameAssemblyPath)
    {
        AssetsDir = assetsDir;
        Prefabs = new PrefabIndex(assetsDir);
        Enums = new GameEnums(gameAssemblyPath);
        localization = LoadEnglishLocalization(Path.Combine(assetsDir, "Resources", "localization.txt"));
    }

    /// <summary>Replaces every `$token` in a name or description with its English text.</summary>
    public string Localize(string text, string fallback = "")
    {
        if (text.Length == 0)
        {
            return fallback;
        }
        // Some names are several tokens joined by spaces, e.g. "$item_upgrader_tier4 $item_upgrader_name".
        return Regex.Replace(text, @"\$(\w+)", m => localization.GetValueOrDefault(m.Groups[1].Value, m.Value));
    }

    /// <summary>Every prefab file under a folder of the exported project (relative to the Assets dir).</summary>
    public IEnumerable<Prefab> PrefabsIn(string folder) =>
        Directory.EnumerateFiles(Path.Combine(AssetsDir, folder), "*.prefab", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(Prefabs.Load);

    public ItemRef? ResolveItem(YNode reference)
    {
        if (Prefabs.Resolve(reference) is not var (prefab, _))
        {
            return null;
        }
        var shared = prefab.Component("m_itemData")?["m_itemData"]["m_shared"];
        if (shared is null || !shared.Exists)
        {
            return null;
        }
        return new ItemRef(
            prefab.Id,
            Localize(shared["m_name"].Str, prefab.Id),
            shared["m_maxStackSize"].Int,
            shared["m_maxQuality"].Int);
    }

    public Station? ResolveStation(YNode reference)
    {
        if (Prefabs.Resolve(reference) is not var (prefab, document))
        {
            return null;
        }
        var station = document ?? prefab.Component("m_discoverRange", "m_name");
        return station is null ? null : new Station(prefab.Id, Localize(station["m_name"].Str, prefab.Id));
    }

    public StatusEffect? ResolveStatusEffect(YNode reference)
    {
        if (Prefabs.Resolve(reference) is not var (prefab, document))
        {
            return null;
        }
        var effect = document ?? prefab.Component("m_tooltip", "m_ttl");
        if (effect is null || !effect["m_name"].Str.StartsWith('$'))
        {
            return null;
        }
        return new StatusEffect(
            Localize(effect["m_name"].Str, prefab.Id),
            Localize(effect["m_tooltip"].Str) is { Length: > 0 } tooltip ? tooltip : null,
            effect["m_ttl"].Num is > 0 and var ttl ? ttl : null);
    }

    /// <summary>English text for every localization key (without the `$`) except those starting with one of the given prefixes.</summary>
    public Dictionary<string, string> TokensExcept(params string[] excludedPrefixes) =>
        localization.Where(e => !excludedPrefixes.Any(p => e.Key.StartsWith(p, StringComparison.Ordinal)))
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToDictionary(e => e.Key, e => e.Value);

    /// <summary>The non-zero fields of a Unity damage/modifier struct, e.g. `m_slash: 50` becomes `slash: 50`.</summary>
    public static Dictionary<string, double>? NonZero(YNode fields)
    {
        var result = fields.Entries
            .Where(e => e.Value.Num != 0)
            .ToDictionary(e => e.Key.StartsWith("m_") ? e.Key[2..] : e.Key, e => e.Value.Num);
        return result.Count > 0 ? result : null;
    }

    static Dictionary<string, string> LoadEnglishLocalization(string path)
    {
        var map = new Dictionary<string, string>();
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Warning: {path} not found; names will be raw tokens");
            return map;
        }

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false });
        while (csv.Read())
        {
            var key = csv.GetField(0);
            var english = csv.GetField(1);
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(english))
            {
                map[key] = english;
            }
        }
        return map;
    }
}

record ItemRef(string Id, string Name, int? MaxStack, int? MaxQuality);
record Station(string Id, string Name);
record StatusEffect(string Name, string? Tooltip, double? Duration);
