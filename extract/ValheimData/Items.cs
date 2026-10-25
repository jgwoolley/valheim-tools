using System.Text.Json.Serialization;

/// <summary>Weapons, armor, food, materials and the like: every ItemDrop prefab under GameElements/Items.</summary>
static class Items
{
    public static List<Item> Extract(Context ctx)
    {
        var items = new List<Item>();
        foreach (var prefab in ctx.PrefabsIn(Path.Combine("GameElements", "Items")))
        {
            var shared = prefab.Component("m_itemData")?["m_itemData"]["m_shared"];
            if (shared is null || !shared.Exists || !shared["m_name"].Str.StartsWith('$'))
            {
                continue;
            }

            var name = ctx.Localize(shared["m_name"].Str, prefab.Id);
            if (name.StartsWith('$'))
            {
                continue; // no English text: an unreleased or test item
            }

            // Unity serializes every field for every item, so e.g. a piece of meat still has an armor
            // value (its default). Only report stats that mean something for the item's type.
            var itemType = ctx.Enums.Name("ItemType", shared["m_itemType"].Int);
            var isWeapon = Weapons.Contains(itemType);
            var isArmor = Armor.Contains(itemType);
            var isEquipment = isWeapon || isArmor || itemType is "Shield" or "Tool" or "Torch";
            var isFood = itemType == "Consumable" &&
                (shared["m_food"].Num > 0 || shared["m_foodStamina"].Num > 0 || shared["m_foodEitr"].Num > 0);
            var skill = shared["m_skillType"].Int;

            items.Add(new Item(
                prefab.Id,
                name,
                shared["m_name"].Str,
                ctx.Localize(shared["m_description"].Str) is { Length: > 0 } d ? d : null,
                itemType,
                shared["m_weight"].Num,
                Positive(shared["m_value"].Int),
                shared["m_maxStackSize"].Int,
                shared["m_maxQuality"].Int,
                isEquipment ? Positive(shared["m_maxDurability"].Num) : null,
                isWeapon || itemType == "Tool" ? Context.NonZero(shared["m_damages"]) : null,
                isWeapon || itemType == "Tool" ? Context.NonZero(shared["m_damagesPerLevel"]) : null,
                isArmor ? Positive(shared["m_armor"].Num) : null,
                isArmor ? Positive(shared["m_armorPerLevel"].Num) : null,
                isWeapon || itemType == "Shield" ? Positive(shared["m_blockPower"].Num) : null,
                isWeapon || itemType == "Shield" ? Positive(shared["m_deflectionForce"].Num) : null,
                isFood
                    ? new Food(
                        shared["m_food"].Num,
                        shared["m_foodStamina"].Num,
                        shared["m_foodEitr"].Num,
                        shared["m_foodBurnTime"].Num,
                        shared["m_foodRegen"].Num)
                    : null,
                skill != 0 && (isWeapon || itemType == "Tool") ? ctx.Enums.Name("SkillType", skill) : null,
                isWeapon || itemType == "Tool" ? Positive(shared["m_toolTier"].Int) : null,
                isArmor && shared["m_movementModifier"].Num is var m && m != 0 ? m : null,
                shared["m_setName"].Str is { Length: > 0 } set ? set : null,
                shared["m_setName"].Str.Length > 0 ? Positive(shared["m_setSize"].Int) : null,
                ctx.ResolveStatusEffect(shared["m_setStatusEffect"]),
                ctx.ResolveStatusEffect(shared["m_equipStatusEffect"]),
                itemType == "Consumable" ? ctx.ResolveStatusEffect(shared["m_consumeStatusEffect"]) : null,
                itemType == "Trinket" ? Positive(shared["m_maxAdrenaline"].Num) : null,
                itemType == "Trinket" ? ctx.ResolveStatusEffect(shared["m_fullAdrenalineSE"]) : null,
                isArmor ? Resistances(ctx, shared["m_damageModifiers"]) : null));
        }
        return items;
    }

    static readonly HashSet<string> Weapons = ["OneHandedWeapon", "TwoHandedWeapon", "TwoHandedWeaponLeft", "Bow", "Ammo", "AmmoNonEquipable"];
    static readonly HashSet<string> Armor = ["Helmet", "Chest", "Legs", "Shoulder", "Utility"];

    /// <summary>Armor's damage-type resistances, e.g. {"Frost": "Resistant"}.</summary>
    static Dictionary<string, string>? Resistances(Context ctx, YNode modifiers)
    {
        var result = modifiers.Items
            .Where(m => m["m_modifier"].Int != 0)
            .ToDictionary(
                m => ctx.Enums.Name("DamageType", m["m_type"].Int),
                m => ctx.Enums.Name("DamageModifier", m["m_modifier"].Int));
        return result.Count > 0 ? result : null;
    }

    static int? Positive(int value) => value > 0 ? value : null;
    static double? Positive(double value) => value > 0 ? value : null;
}

record Food(double Health, double Stamina, double Eitr, double BurnTime, double Regen);
record Item(
    string Id,
    string Name,
    [property: JsonIgnore] string Token,
    string? Description,
    string Type,
    double Weight,
    int? Value,
    int MaxStack,
    int MaxQuality,
    double? MaxDurability,
    Dictionary<string, double>? Damage,
    Dictionary<string, double>? DamagePerLevel,
    double? Armor,
    double? ArmorPerLevel,
    double? BlockPower,
    double? DeflectionForce,
    Food? Food,
    string? Skill,
    int? ToolTier,
    double? MovementModifier,
    string? Set,
    int? SetSize,
    StatusEffect? SetEffect,
    StatusEffect? EquipEffect,
    StatusEffect? ConsumeEffect,
    double? Adrenaline,
    StatusEffect? AdrenalineEffect,
    Dictionary<string, string>? Resistances);
