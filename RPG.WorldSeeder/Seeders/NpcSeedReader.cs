using System.Text.Json;
using Microsoft.Extensions.Logging;
using RPG.Domain.Enums;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Npcs.NpcComponents;
using RPG.Domain.Models.Skills;

namespace RPG.WorldSeeder.Seeders;

internal static class NpcSeedReader
{
    public static async Task<IReadOnlyList<Npc>> LoadAsync(
        string rootPath,
        JsonSerializerOptions options,
        ILogger logger,
        IReadOnlyDictionary<Guid, Skill> skills,
        IReadOnlyDictionary<Guid, Item> items,
        CancellationToken ct)
    {
        var folder = Path.Combine(rootPath, "Npcs");
        if (!Directory.Exists(folder))
        {
            logger.LogWarning("Seed NPC folder missing: {Folder}", folder);
            return new List<Npc>();
        }
        var result = new List<Npc>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await using var stream = File.OpenRead(file);
                var model = await JsonSerializer.DeserializeAsync<NpcSeedModel>(stream, options, ct).ConfigureAwait(false);
                if (model == null) continue;
                var npc = Npc.Create(model.Name, model.DisplayName, model.SpawnLocation.ToDomain(), model.WorldId, model.Tags?.ToHashSet(StringComparer.OrdinalIgnoreCase));
                typeof(Npc).GetProperty("Id")!.SetValue(npc, model.Id);
                npc.Description = model.Description ?? string.Empty;
                npc.Level = model.Level;
                npc.LastUpdated = model.LastUpdated ?? DateTime.UtcNow;
                npc.RespawnAt = model.RespawnAt;
                npc.Components.Clear();
                npc.CurrentHealth = model.CurrentHealth;
                npc.MaxHealth = model.MaxHealth;
                npc.CurrentResource = model.CurrentResource;
                npc.MaxResource = model.MaxResource;
                if (model.BaseStats != null)
                {
                    foreach (var kvp in model.BaseStats)
                    {
                        if (Enum.TryParse<StatsProperty>(kvp.Key, true, out var stat)) npc.BaseStats[stat] = kvp.Value;
                    }
                }
                if (model.ModifiedStats != null)
                {
                    foreach (var kvp in model.ModifiedStats)
                    {
                        if (Enum.TryParse<StatsProperty>(kvp.Key, true, out var stat)) npc.ModifiedStats[stat] = kvp.Value;
                    }
                }
                if (model.Skills != null)
                {
                    foreach (var entry in model.Skills)
                    {
                        if (!skills.TryGetValue(entry.SkillId, out var skill)) continue;
                        if (!Enum.TryParse<SkillAvailability>(entry.Availability, true, out var availability)) availability = SkillAvailability.Available;
                        npc.Skills[skill] = availability;
                    }
                }
                if (model.ActiveSkills != null)
                {
                    foreach (var entry in model.ActiveSkills)
                    {
                        if (!skills.TryGetValue(entry.SkillId, out var skill)) continue;
                        npc.ActiveSkills[skill] = entry.LastUsed ?? DateTime.UtcNow;
                    }
                }
                if (model.CurrentLocation != null) npc.CurrentLocation = model.CurrentLocation.ToDomain();
                if (model.Components != null)
                {
                    foreach (var componentSeed in model.Components)
                    {
                        var component = NpcComponentFactory.Create(componentSeed, skills, items, model, npc, options, logger);
                        if (component != null) npc.Components.Add(component);
                    }
                }
                result.Add(npc);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deserialize NPC seed file {File}", file);
            }
        }
        logger.LogInformation("Loaded {Count} NPCs", result.Count);
        return result;
    }
}
