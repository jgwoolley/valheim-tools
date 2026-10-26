// Reads the Unity project exported by AssetRipper (extract/rip-assets.sh) and writes
// Valheim's game data (recipes, items, buildings, creatures) as JSON files.
//
// Usage: dotnet run --project extract/ValheimData -- <ripped Assets dir> <output dir> [build id] [assembly_valheim.dll]

using System.Text.Json;
using System.Text.Json.Serialization;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ValheimData <ripped Assets dir> <output dir> [build id] [assembly_valheim.dll]");
    return 1;
}

var assetsDir = Path.GetFullPath(args[0]);
var outputDir = Path.GetFullPath(args[1]);
var buildId = args.Length > 2 ? args[2] : null;
var assemblyPath = args.Length > 3 ? args[3] : null;

if (!Directory.Exists(assetsDir))
{
    Console.Error.WriteLine($"Assets directory not found: {assetsDir}");
    return 1;
}

var ctx = new Context(assetsDir, assemblyPath);
Directory.CreateDirectory(outputDir);

var options = new JsonSerializerOptions
{
    WriteIndented = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

void Write<T>(string name, List<T> rows)
{
    var path = Path.Combine(outputDir, name + ".json");
    File.WriteAllText(path, JsonSerializer.Serialize(new Dictionary<string, object?> { ["buildId"] = buildId, [name] = rows }, options));
    Console.WriteLine($"Wrote {rows.Count} {name} to {path}");
}

// Plain English text for ids and localization tokens, so the save viewer can show real names.
var items = Items.Extract(ctx);
var pieces = Pieces.Extract(ctx);
var creatures = Creatures.Extract(ctx);
var targets =
    items.Select(i => (i.Id, i.Name, i.Token, Page: "items"))
        .Concat(pieces.Select(p => (p.Id, p.Name, p.Token, Page: "buildings")))
        .Concat(creatures.Select(c => (c.Id, c.Name, c.Token, Page: "creatures")))
        .ToList();
var names = new Dictionary<string, object?>
{
    ["buildId"] = buildId,
    ["tokens"] = ctx.TokensExcept("menu_", "settings_", "language_", "button_", "hud_", "loadscreen_", "inventory_"),
    // id -> English name, plus which wiki page ("items", "buildings", "creatures") an id belongs to
    ["prefabs"] = targets.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First().Name),
    ["pages"] = targets.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First().Page),
    // localization token (without the $) -> prefab id, e.g. "item_wood" -> "Wood"
    ["tokenIds"] = targets.Where(t => t.Token.StartsWith('$'))
        .GroupBy(t => t.Token[1..]).ToDictionary(g => g.Key, g => g.First().Id),
};
File.WriteAllText(Path.Combine(outputDir, "names.json"), JsonSerializer.Serialize(names, options));
Console.WriteLine("Wrote names.json");

Write("recipes", Recipes.Extract(ctx));
Write("items", items);
Write("pieces", pieces);
Write("creatures", creatures);
Write("conversions", Conversions.Extract(ctx));
return 0;
