using System.Text.Json;
using Microsoft.Extensions.Logging;
using RPG.Domain.Models.Skills;

namespace RPG.WorldSeeder.Seeders;

internal static class SkillSeedReader
{
    public static async Task<IReadOnlyDictionary<Guid, Skill>> LoadAsync(
        string rootPath,
        JsonSerializerOptions options,
        ILogger logger,
        CancellationToken ct)
    {
        var folder = Path.Combine(rootPath, "Skills");
        if (!Directory.Exists(folder))
        {
            logger.LogWarning("Seed skills folder missing: {Folder}", folder);
            return new Dictionary<Guid, Skill>();
        }
        var result = new Dictionary<Guid, Skill>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await using var stream = File.OpenRead(file);
                var model = await JsonSerializer.DeserializeAsync<SkillSeedModel>(stream, options, ct).ConfigureAwait(false);
                if (model == null) continue;
                var skill = Skill.Create(model.Name, model.Description ?? string.Empty);
                typeof(Skill).GetProperty("Id")!.SetValue(skill, model.Id);
                skill.IconId = model.IconId ?? string.Empty;
                skill.Tags = model.Tags?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
                skill.Components.Clear();
                if (model.Components != null)
                {
                    foreach (var c in model.Components)
                    {
                        var inst = SkillComponentsFactory.Create(c);
                        if (inst is not null) skill.Components.Add(inst);
                    }
                }
                result[model.Id] = skill;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deserialize skill seed file {File}", file);
            }
        }
        logger.LogInformation("Loaded {Count} skills", result.Count);
        return result;
    }
}
