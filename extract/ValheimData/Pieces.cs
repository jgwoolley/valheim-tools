using System.Text.Json.Serialization;

/// <summary>Buildable pieces, found through the piece tables of the Hammer, Hoe, Cultivator and Feaster.</summary>
static class Pieces
{
    public static List<Piece> Extract(Context ctx)
    {
        var pieces = new Dictionary<string, Piece>();
        foreach (var tool in ctx.PrefabsIn(Path.Combine("GameElements", "Items", "tools")))
        {
            var table = ctx.Prefabs.Resolve(tool.Component("m_itemData")?["m_itemData"]["m_shared"]["m_buildPieces"] ?? YNode.Missing);
            var listed = table?.Prefab.Component("m_pieces")?["m_pieces"].Items;
            if (listed is null)
            {
                continue;
            }

            var toolName = ctx.Localize(tool.Component("m_itemData")?["m_itemData"]["m_shared"]["m_name"].Str ?? "", tool.Id);
            foreach (var entry in listed)
            {
                if (ctx.Prefabs.Resolve(entry) is not var (prefab, _))
                {
                    continue;
                }
                var piece = prefab.Component("m_category", "m_resources");
                if (piece is null || !piece["m_enabled"].Bool || pieces.ContainsKey(prefab.Id))
                {
                    continue;
                }

                var name = ctx.Localize(piece["m_name"].Str, prefab.Id);
                if (name.StartsWith('$'))
                {
                    continue; // no English text
                }

                var health = (prefab.Component("m_worn", "m_health") ?? prefab.Component("m_health"))?["m_health"].Num;
                pieces[prefab.Id] = new Piece(
                    prefab.Id,
                    name,
                    piece["m_name"].Str,
                    ctx.Localize(piece["m_description"].Str) is { Length: > 0 } d ? d : null,
                    ctx.Enums.Name("PieceCategory", piece["m_category"].Int),
                    toolName,
                    piece["m_resources"].Items
                        .Select(r => (Item: ctx.ResolveItem(r["m_resItem"]), Amount: r["m_amount"].Int))
                        .Where(r => r.Item is not null)
                        .Select(r => new PieceResource(r.Item!, r.Amount))
                        .ToList(),
                    ctx.ResolveStation(piece["m_craftingStation"]),
                    piece["m_comfort"].Int is var c and > 0 ? c : null,
                    health is > 0 ? health : null);
            }
        }
        return pieces.Values.ToList();
    }
}

record PieceResource(ItemRef Item, int Amount);
record Piece(
    string Id,
    string Name,
    [property: JsonIgnore] string Token,
    string? Description,
    string Category,
    string Tool,
    List<PieceResource> Resources,
    Station? Station,
    int? Comfort,
    double? Health);
