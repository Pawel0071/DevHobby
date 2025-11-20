using System.Text.Json;
using Microsoft.Extensions.Logging;
using RPG.Domain.Enums;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Npcs.NpcComponents;
using RPG.Domain.Models.Skills;

namespace RPG.WorldSeeder.Seeders;

internal static class NpcComponentFactory
{
    public static INpcComponent? Create(
        NpcComponentSeedModel model,
        IReadOnlyDictionary<Guid, Skill> skills,
        IReadOnlyDictionary<Guid, Item> items,
        NpcSeedModel context,
        Npc npc,
        JsonSerializerOptions options,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(model.Type)) return null;
        switch (model.Type.Trim().ToLowerInvariant())
        {
            case "dialogue":
            {
                var component = new DialogueComponent();
                try
                {
                    var root = model.Properties;
                    if (root.TryGetProperty("dialogueScript", out var scriptProp)) component.DialogueScript = scriptProp.GetString() ?? string.Empty;
                    if (root.TryGetProperty("greetingText", out var greetProp)) component.GreetingText = greetProp.GetString() ?? string.Empty;
                    if (root.TryGetProperty("farewellText", out var farewellProp)) component.FarewellText = farewellProp.GetString() ?? string.Empty;
                    if (root.TryGetProperty("scriptParameters", out var paramsProp) && paramsProp.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in paramsProp.EnumerateObject())
                        {
                            component.ScriptParameters[p.Name] = p.Value.ValueKind switch
                            {
                                JsonValueKind.Number when p.Value.TryGetInt32(out var iv) => iv,
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                _ => p.Value.ToString()
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed parsing dialogue component for NPC {NpcName}", context.Name);
                }
                return component;
            }
            case "questgiver":
                return JsonSerializer.Deserialize<QuestGiverComponent>(model.Properties.GetRawText(), options);
            case "trainer":
            {
                var data = JsonSerializer.Deserialize<TrainerComponentSeedModel>(model.Properties.GetRawText(), options);
                if (data == null) return null;
                var component = new TrainerComponent { Specialization = data.Specialization ?? string.Empty };
                if (data.TeachableSkills != null)
                {
                    var container = component.GetSkillsContainer();
                    foreach (var entry in data.TeachableSkills)
                    {
                        if (!skills.TryGetValue(entry.SkillId, out var skill))
                        {
                            logger.LogWarning("Trainer component on NPC {NpcName} unknown skill {SkillId}", context.Name, entry.SkillId);
                            continue;
                        }
                        if (!Enum.TryParse<SkillAvailability>(entry.Availability, true, out var availability)) availability = SkillAvailability.Available;
                        container.LearnSkill(skill, availability);
                    }
                }
                return component;
            }
            case "merchant":
            {
                var data = JsonSerializer.Deserialize<MerchantComponentSeedModel>(model.Properties.GetRawText(), options);
                if (data == null) return null;
                var component = new MerchantComponent
                {
                    GoldAmount = data.GoldAmount,
                    GlobalPriceModifier = data.GlobalPriceModifier,
                    PriceModifiers = data.PriceModifiers != null ? new Dictionary<string, float>(data.PriceModifiers, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                };
                if (data.Inventory != null)
                {
                    foreach (var slot in data.Inventory)
                    {
                        if (slot.Slot < 0 || slot.Slot >= component.MerchantInventory.Count)
                        {
                            logger.LogWarning("Merchant component on NPC {NpcName} invalid slot {Slot}", context.Name, slot.Slot);
                            continue;
                        }
                        if (!items.TryGetValue(slot.ItemId, out var item))
                        {
                            logger.LogWarning("Merchant component on NPC {NpcName} unknown item {ItemId}", context.Name, slot.ItemId);
                            continue;
                        }
                        component.MerchantInventory[slot.Slot].Item = item;
                        component.MerchantInventory[slot.Slot].Quantity = slot.Quantity;
                    }
                }
                return component;
            }
            case "combat":
            {
                var data = JsonSerializer.Deserialize<CombatComponentSeedModel>(model.Properties.GetRawText(), options);
                if (data == null) return null;
                var component = new CombatComponent { AggroRange = data.AggroRange, LeashRange = data.LeashRange, AiBehaviorScript = data.AiBehaviorScript ?? string.Empty };
                if (data.Stats != null)
                {
                    foreach (var kvp in data.Stats)
                    {
                        if (!Enum.TryParse<StatsProperty>(kvp.Key, true, out var stat))
                        {
                            logger.LogWarning("Combat component on NPC {NpcName} unknown stat {Stat}", context.Name, kvp.Key);
                            continue;
                        }
                        npc.BaseStats[stat] = kvp.Value;
                        npc.ModifiedStats[stat] = kvp.Value;
                    }
                }
                if (data.Skills != null)
                {
                    foreach (var entry in data.Skills)
                    {
                        if (!skills.TryGetValue(entry.SkillId, out var skill))
                        {
                            logger.LogWarning("Combat component on NPC {NpcName} unknown skill {SkillId}", context.Name, entry.SkillId);
                            continue;
                        }
                        if (!Enum.TryParse<SkillAvailability>(entry.Availability, true, out var availability)) availability = SkillAvailability.Available;
                        npc.Skills[skill] = availability;
                    }
                }
                return component;
            }
            case "lootable":
            {
                var data = JsonSerializer.Deserialize<LootableComponentSeedModel>(model.Properties.GetRawText(), options);
                if (data == null) return null;
                var component = new LootableComponent { ExperienceReward = data.ExperienceReward, GoldReward = data.GoldReward };
                if (data.LootTable != null)
                {
                    var container = component.GetLootContainer();
                    foreach (var entry in data.LootTable)
                    {
                        if (entry.Slot < 0 || entry.Slot >= container.LootSlots.Count)
                        {
                            logger.LogWarning("Lootable component on NPC {NpcName} invalid slot {Slot}", context.Name, entry.Slot);
                            continue;
                        }
                        var slot = container.LootSlots[entry.Slot];
                        if (items.TryGetValue(entry.ItemId, out var item))
                        {
                            slot.Item = item;
                            slot.MinQuantity = entry.MinQuantity;
                            slot.MaxQuantity = entry.MaxQuantity;
                            slot.DropChance = entry.DropChance;
                        }
                        else
                        {
                            logger.LogWarning("Lootable component on NPC {NpcName} unknown item {ItemId}", context.Name, entry.ItemId);
                        }
                    }
                }
                return component;
            }
            case "respawn":
            {
                var data = JsonSerializer.Deserialize<RespawnComponentSeedModel>(model.Properties.GetRawText(), options);
                if (data == null) return null;
                if (data.RespawnTimeSeconds > 0)
                    npc.RespawnAt = DateTime.UtcNow.AddSeconds(data.RespawnTimeSeconds);
                if (data.RespawnLocation != null)
                    npc.SpawnLocation = data.RespawnLocation.ToDomain();
                return null; // metadata only
            }
            default:
                return null;
        }
    }
}
