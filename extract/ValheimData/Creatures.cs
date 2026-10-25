using System.Text.Json.Serialization;

/// <summary>Animals, monsters and bosses: every prefab under Characters with a Character component.</summary>
static class Creatures
{
    public static List<Creature> Extract(Context ctx)
    {
        var creatures = new Dictionary<string, Creature>();
        foreach (var prefab in ctx.PrefabsIn("Characters"))
        {
            var character = prefab.Component("m_faction", "m_health");
            if (character is null || !character["m_name"].Str.StartsWith('$') || creatures.ContainsKey(prefab.Id))
            {
                continue;
            }

            var name = ctx.Localize(character["m_name"].Str, prefab.Id);
            if (name.StartsWith('$'))
            {
                continue; // no English text: an internal variant such as a spirit-caller summon
            }

            var drops = prefab.Component("m_drops")?["m_drops"].Items
                .Select(d => (Item: ctx.ResolveItem(d["m_prefab"]), Node: d))
                .Where(d => d.Item is not null)
                .Select(d => new Drop(
                    d.Item!,
                    d.Node["m_amountMin"].Int,
                    d.Node["m_amountMax"].Int,
                    d.Node["m_chance"].Num,
                    d.Node["m_levelMultiplier"].Bool))
                .ToList();

            var tameable = prefab.Component("m_tamingTime");
            var attacks = character["m_defaultItems"].Items
                .Concat(character["m_randomWeapon"].Items)
                .Select(a => ctx.Prefabs.Resolve(a))
                .Where(a => a is not null)
                .Select(a => a!.Value.Prefab)
                .Select(p => new Attack(p.Id, Context.NonZero(p.Component("m_itemData")?["m_itemData"]["m_shared"]["m_damages"] ?? YNode.Missing)))
                .Where(a => a.Damage is not null)
                .ToList();

            var modifiers = character["m_damageModifiers"].Entries
                .Where(e => e.Value.Int != 0)
                .ToDictionary(e => e.Key[2..], e => ctx.Enums.Name("DamageModifier", e.Value.Int));

            creatures[prefab.Id] = new Creature(
                prefab.Id,
                name,
                character["m_name"].Str,
                ctx.Enums.Name("Faction", character["m_faction"].Int),
                character["m_boss"].Bool,
                character["m_health"].Num,
                character["m_walkSpeed"].Num,
                character["m_runSpeed"].Num,
                character["m_flying"].Bool,
                tameable is null ? null : new Taming(tameable["m_tamingTime"].Num, tameable["m_fedDuration"].Num),
                modifiers.Count > 0 ? modifiers : null,
                attacks.Count > 0 ? attacks : null,
                drops is { Count: > 0 } ? drops : null);
        }
        return creatures.Values.ToList();
    }
}

record Drop(ItemRef Item, int Min, int Max, double Chance, bool ScalesWithLevel);
record Taming(double TamingTime, double FedDuration);
record Attack(string Id, Dictionary<string, double>? Damage);
record Creature(
    string Id,
    string Name,
    [property: JsonIgnore] string Token,
    string Faction,
    bool Boss,
    double Health,
    double WalkSpeed,
    double RunSpeed,
    bool Flying,
    Taming? Taming,
    Dictionary<string, string>? DamageModifiers,
    List<Attack>? Attacks,
    List<Drop>? Drops);
