/// <summary>Crafting recipes: the `Recipe` assets under GameElements/Recipes.</summary>
static class Recipes
{
    public static List<Recipe> Extract(Context ctx)
    {
        var recipes = new List<Recipe>();
        var dir = Path.Combine(ctx.AssetsDir, "GameElements", "Recipes");
        foreach (var file in Directory.EnumerateFiles(dir, "*.asset", SearchOption.AllDirectories))
        {
            var doc = UnityYaml.ParseFile(File.ReadAllText(file)).Values.FirstOrDefault();
            if (doc is null || !doc.Has("m_craftingStation") || !doc.Has("m_resources"))
            {
                continue;
            }

            var id = Path.GetFileNameWithoutExtension(file);
            var item = ctx.ResolveItem(doc["m_item"]);
            if (item is null)
            {
                Console.Error.WriteLine($"Skipping {id}: no result item (unused placeholder recipe)");
                continue;
            }

            var station = ctx.ResolveStation(doc["m_craftingStation"]);
            var resources = doc["m_resources"].Items
                .Select(r => (Item: ctx.ResolveItem(r["m_resItem"]), Node: r))
                .Where(r => r.Item is not null)
                .Select(r => new Resource(
                    r.Item!,
                    r.Node["m_amount"].Int,
                    r.Node["m_amountPerLevel"].Int,
                    r.Node["m_recover"].Bool,
                    r.Node["m_upgraderResource"].Bool))
                .ToList();

            recipes.Add(new Recipe(
                id,
                item,
                doc["m_amount"].Int,
                doc["m_enabled"].Bool,
                station,
                station is null ? 0 : doc["m_minStationLevel"].Int,
                ctx.ResolveStation(doc["m_repairStation"]),
                doc["m_requireOnlyOneIngredient"].Bool,
                doc["m_noCraftOnlyUpgrade"].Bool,
                resources));
        }
        recipes.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return recipes;
    }
}

record Resource(ItemRef Item, int Amount, int AmountPerLevel, bool Recover, bool UpgraderResource);
record Recipe(
    string Id,
    ItemRef Item,
    int Amount,
    bool Enabled,
    Station? Station,
    int MinStationLevel,
    Station? RepairStation,
    bool RequireOnlyOneIngredient,
    bool NoCraftOnlyUpgrade,
    List<Resource> Resources);
