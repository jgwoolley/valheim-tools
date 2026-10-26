using System.Text.RegularExpressions;

/// <summary>
/// Where lore comes from: runestones, rune tablets and vegvisirs (prefabs under world/Props/RuneStones), the locations
/// they sit in, and where those locations generate (biome, how many, distance from the world center).
/// </summary>
static partial class Lore
{
    [GeneratedRegex(@"(?:rune(?:stone|tablet)|vegvisir)_\w+", RegexOptions.IgnoreCase)]
    private static partial Regex StoneName();

    [GeneratedRegex(@"lore_[a-z0-9_]+", RegexOptions.IgnoreCase)]
    private static partial Regex LoreToken();

    /// <summary>
    /// One entry per lore text (runestone and rune-tablet texts, with where each can be found), plus one per vegvisir
    /// (which location it reveals). A runestone prefab lists default texts, but the location prefab it sits in can override
    /// them (the Ashlands runestone's base prefab still lists Mistlands texts), so an override found in a location wins.
    /// </summary>
    public static List<LoreSource> Extract(Context ctx)
    {
        var placements = ReadPlacements(ctx);
        var locationFiles = ScanLocationPrefabs(ctx);

        var textSources = new Dictionary<string, LoreSource>(StringComparer.OrdinalIgnoreCase);
        var vegvisirs = new List<LoreSource>();
        foreach (var prefab in ctx.PrefabsIn(Path.Combine("world", "Props", "RuneStones")))
        {
            var runestone = prefab.Component("m_randomTexts", "m_pinName");
            var vegvisir = prefab.Component("m_locations", "m_hoverName");
            if (runestone is null && vegvisir is null)
            {
                continue;
            }

            var containing = locationFiles.Where(l => l.Value.Stones.Contains(prefab.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            var foundIn = containing
                .SelectMany(l => placements.TryGetValue(l.Key.ToLowerInvariant(), out var p) && p.Count > 0
                    ? p
                    : [new LoreLocation(l.Key, [], null, false, null, null)])
                .DistinctBy(p => (p.Name, string.Join(",", p.Biomes), p.Quantity))
                .ToList();

            if (vegvisir is not null)
            {
                vegvisirs.Add(new LoreSource(
                    prefab.Id,
                    "Vegvisir",
                    null,
                    null,
                    [],
                    vegvisir["m_locations"].Items.Select(l => new RevealedLocation(l["m_locationName"].Str, NullIfEmpty(l["m_pinName"].Str))).ToList(),
                    foundIn));
                continue;
            }

            var kind = prefab.Id.StartsWith("RuneTablet", StringComparison.OrdinalIgnoreCase) ? "Rune tablet" : "Runestone";
            var defaults = new List<string>();
            if (runestone!["m_text"].Str.Length > 0)
            {
                defaults.Add(runestone["m_text"].Str.TrimStart('$'));
            }
            defaults.AddRange(runestone["m_randomTexts"].Items.Select(t => t["m_text"].Str.TrimStart('$')).Where(t => t.Length > 0));

            void Add(string token, List<LoreLocation> places)
            {
                if (!textSources.TryGetValue(token, out var source))
                {
                    textSources[token] = source = new LoreSource(token, kind, null, null, ["$" + token], null, []);
                }
                foreach (var place in places.Where(p => !source.FoundIn.Any(q => q.Name == p.Name && q.Biomes.SequenceEqual(p.Biomes) && q.Quantity == p.Quantity)))
                {
                    source.FoundIn.Add(place);
                }
            }

            if (containing.Count == 0)
            {
                defaults.ForEach(t => Add(t, []));
            }
            foreach (var location in containing)
            {
                var places = placements.TryGetValue(location.Key.ToLowerInvariant(), out var p) && p.Count > 0
                    ? p
                    : [new LoreLocation(location.Key, [], null, false, null, null)];
                var overrides = location.Value.LoreTokens.Where(t => !Regex.IsMatch(t, "_(label|topic|description)$")).ToList();
                foreach (var token in overrides.Count > 0 ? overrides : defaults)
                {
                    Add(token.ToLowerInvariant(), places);
                }
            }
        }
        return textSources.Values.Concat(vegvisirs).ToList();
    }

    static string? NullIfEmpty(string value) => value.Length > 0 ? value : null;

    /// <summary>Every world-generation placement of a location prefab (lowercased name), from the ZoneSystem and the extra location lists.</summary>
    static Dictionary<string, List<LoreLocation>> ReadPlacements(Context ctx)
    {
        var result = new Dictionary<string, List<LoreLocation>>();
        var files = new List<string> { Path.Combine(ctx.AssetsDir, "Systems", "_ZoneSystem.prefab") };
        files.AddRange(Directory.EnumerateFiles(Path.Combine(ctx.AssetsDir, "Systems", "LocationLists"), "*.prefab"));
        foreach (var file in files)
        {
            foreach (var list in ctx.Prefabs.Load(file).Documents.Values.Where(d => d.Has("m_locations")))
            {
                foreach (var entry in list["m_locations"].Items)
                {
                    var name = entry["m_prefabName"].Str;
                    if (name.Length == 0 || !entry["m_enable"].Bool)
                    {
                        continue;
                    }
                    var placement = new LoreLocation(
                        name,
                        ctx.Enums.FlagNames("Biome", entry["m_biome"].Int),
                        entry["m_quantity"].Int,
                        entry["m_unique"].Bool,
                        entry["m_minDistance"].Num > 0 ? entry["m_minDistance"].Num : null,
                        entry["m_maxDistance"].Num > 0 ? entry["m_maxDistance"].Num : null);
                    if (!result.TryGetValue(name.ToLowerInvariant(), out var placements))
                    {
                        result[name.ToLowerInvariant()] = placements = [];
                    }
                    placements.Add(placement);
                }
            }
        }
        return result;
    }

    /// <summary>The runestone/tablet/vegvisir prefabs and `lore_*` texts each location prefab (by file name) mentions. Location prefabs are huge, so this reads them line by line.</summary>
    static Dictionary<string, LocationContents> ScanLocationPrefabs(Context ctx)
    {
        var found = new Dictionary<string, LocationContents>();
        var dir = Path.Combine(ctx.AssetsDir, "world", "Locations");
        foreach (var file in Directory.EnumerateFiles(dir, "*.prefab", SearchOption.AllDirectories))
        {
            var contents = new LocationContents();
            foreach (var line in File.ReadLines(file))
            {
                if (line.Contains("une", StringComparison.OrdinalIgnoreCase) || line.Contains("vegvisir", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (Match m in StoneName().Matches(line))
                    {
                        contents.Stones.Add(m.Value);
                    }
                }
                if (line.Contains("lore_", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (Match m in LoreToken().Matches(line))
                    {
                        contents.LoreTokens.Add(m.Value.ToLowerInvariant());
                    }
                }
            }
            if (contents.Stones.Count > 0)
            {
                found[Path.GetFileNameWithoutExtension(file)] = contents;
            }
        }
        return found;
    }

    sealed class LocationContents
    {
        public HashSet<string> Stones { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> LoreTokens { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

record RevealedLocation(string Location, string? PinName);
record LoreLocation(string Name, List<string> Biomes, int? Quantity, bool Unique, double? MinDistance, double? MaxDistance);
record LoreSource(string Id, string Kind, string? Label, string? Topic, List<string> Texts, List<RevealedLocation>? Reveals, List<LoreLocation> FoundIn);
