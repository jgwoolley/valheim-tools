/// <summary>
/// What stations turn into what: smelters, kilns, spinning wheels and the like (Smelter), cooking
/// stations and ovens (CookingStation) and the fermenter. These aren't recipes; each station
/// component carries its own `m_conversion` list.
/// </summary>
static class Conversions
{
    public static List<Conversion> Extract(Context ctx)
    {
        var conversions = new List<Conversion>();
        foreach (var prefab in ctx.PrefabsIn(Path.Combine("GameElements", "Pieces")))
        {
            var piece = prefab.Component("m_category", "m_resources");
            var converter = prefab.Component("m_conversion");
            if (piece is null || converter is null)
            {
                continue;
            }

            var station = new Station(prefab.Id, ctx.Localize(piece["m_name"].Str, prefab.Id));
            var entries = converter["m_conversion"].Items;

            // Which kind of station this is shows in which fields its component has.
            string method;
            Func<YNode, double> seconds;
            Func<YNode, int> produced = _ => 1;
            ItemRef? fuel = null;
            var fuelAmount = 0;
            if (converter.Has("m_fermentationDuration"))
            {
                method = "Fermenting";
                var duration = converter["m_fermentationDuration"].Num;
                seconds = _ => duration;
                produced = e => Math.Max(1, e["m_producedItems"].Int);
            }
            else if (converter.Has("m_secPerProduct"))
            {
                method = "Smelting";
                var perProduct = converter["m_secPerProduct"].Num;
                seconds = _ => perProduct;
                fuel = ctx.ResolveItem(converter["m_fuelItem"]);
                fuelAmount = converter["m_fuelPerProduct"].Int;
            }
            else if (entries.Count > 0 && entries[0].Has("m_cookTime"))
            {
                method = "Cooking";
                seconds = e => e["m_cookTime"].Num;
            }
            else
            {
                continue;
            }

            foreach (var entry in entries)
            {
                var from = ctx.ResolveItem(entry["m_from"]);
                var to = ctx.ResolveItem(entry["m_to"]);
                if (from is null || to is null)
                {
                    continue;
                }
                conversions.Add(new Conversion(
                    station,
                    method,
                    from,
                    to,
                    produced(entry),
                    seconds(entry),
                    fuel is not null && fuelAmount > 0 ? new Fuel(fuel, fuelAmount) : null));
            }
        }
        return conversions;
    }
}

record Fuel(ItemRef Item, int Amount);
record Conversion(Station Station, string Method, ItemRef From, ItemRef To, int Amount, double Seconds, Fuel? Fuel);
