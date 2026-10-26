/// <summary>Steam/in-game achievements: the AchievementItems assets, each with what triggers it.</summary>
static class Achievements
{
    public static List<Achievement> Extract(Context ctx)
    {
        var dir = Path.Combine(ctx.AssetsDir, "Scripts", "gui", "Achievements", "AchievementItems");
        var assets = Directory.EnumerateFiles(dir, "*.asset")
            .Select(f => ctx.Prefabs.Load(f))
            .Select(p => (Prefab: p, Doc: p.Component("m_id", "m_name")))
            .Where(a => a.Doc is not null)
            .ToList();

        var idByGuid = new Dictionary<string, string>();
        var achievements = new List<Achievement>();
        foreach (var (prefab, doc) in assets)
        {
            var triggers = new List<Trigger>();

            var stats = doc!["m_statTrigger"].Items
                .Select(t => new Target(ctx.Enums.Name("PlayerStatType", t["m_stat"].Int), t["m_amountAboveEquals"].Num, t["m_stat"].Int))
                .ToList();
            if (stats.Count > 0)
            {
                triggers.Add(new Trigger("stat", stats, null));
            }

            // m_enemyStatsTriggers, m_itemCraftTriggers, ...: lists of {m_stat: "$token", m_amount}.
            foreach (var (key, node) in doc.Entries.Where(e => e.Key.EndsWith("Triggers") && e.Key != "m_otherAchievementTriggers" && e.Value.Items.Count > 0))
            {
                var targets = node.Items.Select(t => new Target(t["m_stat"].Str, t["m_amount"].Num)).ToList();
                triggers.Add(new Trigger(key["m_".Length..^"Triggers".Length], targets, null));
            }

            var others = doc["m_otherAchievementTriggers"].Items
                .Select(r => ctx.Prefabs.Resolve(r))
                .Where(r => r is not null)
                .Select(r => r!.Value.Prefab.Id)
                .ToList();
            if (others.Count > 0)
            {
                triggers.Add(new Trigger("achievement", [], others));
            }

            achievements.Add(new Achievement(
                doc["m_id"].Str,
                ctx.Localize(doc["m_name"].Str, doc["m_id"].Str),
                ctx.Localize(doc["m_description"].Str) is { Length: > 0 } d ? d : null,
                doc["m_isSecret"].Bool,
                doc["m_difficultyRequirement"].Int,
                triggers));
        }
        return achievements.OrderBy(a => a.Id, StringComparer.Ordinal).ToList();
    }
}

/// <summary>One requirement. For `stat` triggers `Index` is the PlayerStatType value, which is also the slot in a save's stats array.</summary>
record Target(string Name, double Amount, int? Index = null);
record Trigger(string Kind, List<Target> Targets, List<string>? Achievements);
record Achievement(string Id, string Name, string? Description, bool Secret, int Difficulty, List<Trigger> Triggers);
