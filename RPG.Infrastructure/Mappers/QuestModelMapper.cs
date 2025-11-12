using System.Text.Json;
using RPG.Domain.Entities.Quests;
using RPG.Domain.Entities.Quests.QuestComponents;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Enums; // TagTarget
using RPG.Abstractions; // TagComponentMap + TagComponentHelper

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Quest domain entity and QuestDocument
///     Components are serialized to JSON for flexible storage
/// </summary>
public class QuestModelMapper : IModelMapper<Quest, QuestDocument>
{
    private readonly ILogger<QuestModelMapper> _logger;
    private readonly LocationMapper _locationMapper;

    public QuestModelMapper(ILogger<QuestModelMapper> logger, LocationMapper locationMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
    }

    public QuestDocument ToPersistence(Quest entity)
    {
        _logger.Debug($"Converting Quest to QuestDocument. Id={entity.Id}, Title={entity.Title}");
        // synchronize tags from components before persisting (merge)
        var derived = TagComponentHelper.ResolveComponentTags(entity.Components.Select(c => c.GetType()), TagTarget.Quest);
        foreach (var t in derived) entity.Tags.Add(t);
        return new QuestDocument
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            QuestGiverName = entity.QuestGiverName,
            QuestGiverId = entity.QuestGiverId,
            StartLocation = _locationMapper.ToDocument(entity.StartLocation),
            TurnInLocation =
                entity.TurnInLocation != null ? _locationMapper.ToDocument(entity.TurnInLocation) : null,
            Tags = entity.Tags.ToList(),
            Components = entity.Components.Select(c => new ComponentData
            {
                Type = c.GetType().Name, Data = JsonSerializer.Serialize(c, c.GetType())
            }).ToList()
        };
    }

    public Quest ToDomain(QuestDocument document)
    {
        _logger.Debug($"Converting QuestDocument to Quest. Id={document.Id}, Title={document.Title}");
        var startLocation = _locationMapper.ToEntity(document.StartLocation);
        var quest = Quest.Create(
            document.Title,
            document.Description,
            document.QuestGiverName,
            startLocation,
            document.Tags.ToHashSet());

        // Preserve ID from document using reflection
        typeof(Quest).GetProperty("Id")!.SetValue(quest, document.Id);

        quest.QuestGiverId = document.QuestGiverId;
        quest.TurnInLocation = document.TurnInLocation != null
            ? _locationMapper.ToEntity(document.TurnInLocation)
            : null;

        // Deserialize components
        foreach (var componentData in document.Components)
        {
            var component = DeserializeComponent(componentData);
            if (component != null) quest.Components.Add(component);
        }

        // Auto-add missing components based on tags
        var requiredTypes = TagComponentMap.GetRequiredComponentTypes(quest.Tags, TagTarget.Quest);
        foreach (var type in requiredTypes)
        {
            if (quest.Components.Any(c => c.GetType() == type)) continue;
            var empty = Activator.CreateInstance(type) as IQuestComponent;
            if (empty != null) quest.Components.Add(empty);
        }

        // Ensure tags reflect present components (merge)
        var resolved = TagComponentHelper.ResolveComponentTags(quest.Components.Select(c => c.GetType()), TagTarget.Quest);
        foreach (var t in resolved) quest.Tags.Add(t);

        return quest;
    }

    public Quest ToEntity(QuestDocument document) => ToDomain(document);

    private IQuestComponent? DeserializeComponent(ComponentData componentData)
    {
        return componentData.Type switch
        {
            // Objectives
            nameof(KillObjectiveComponent) => JsonSerializer.Deserialize<KillObjectiveComponent>(componentData.Data),
            nameof(CollectObjectiveComponent) => JsonSerializer.Deserialize<CollectObjectiveComponent>(componentData
                .Data),
            nameof(DeliverObjectiveComponent) => JsonSerializer.Deserialize<DeliverObjectiveComponent>(componentData
                .Data),
            nameof(ExploreObjectiveComponent) => JsonSerializer.Deserialize<ExploreObjectiveComponent>(componentData
                .Data),
            nameof(InteractObjectiveComponent) => JsonSerializer.Deserialize<InteractObjectiveComponent>(componentData
                .Data),

            // Requirements
            nameof(LevelRequirementComponent) => JsonSerializer.Deserialize<LevelRequirementComponent>(componentData
                .Data),
            nameof(PrerequisiteQuestsComponent) => JsonSerializer.Deserialize<PrerequisiteQuestsComponent>(componentData
                .Data),
            nameof(ClassRequirementComponent) => JsonSerializer.Deserialize<ClassRequirementComponent>(componentData
                .Data),

            // Rewards
            nameof(BasicRewardsComponent) => JsonSerializer.Deserialize<BasicRewardsComponent>(componentData.Data),
            nameof(ItemRewardsComponent) => JsonSerializer.Deserialize<ItemRewardsComponent>(componentData.Data),
            nameof(ReputationRewardsComponent) => JsonSerializer.Deserialize<ReputationRewardsComponent>(componentData
                .Data),
            nameof(SkillRewardsComponent) => JsonSerializer.Deserialize<SkillRewardsComponent>(componentData.Data),

            // Quest Properties
            nameof(RepeatableQuestComponent) =>
                JsonSerializer.Deserialize<RepeatableQuestComponent>(componentData.Data),
            nameof(TimeLimitComponent) => JsonSerializer.Deserialize<TimeLimitComponent>(componentData.Data),
            nameof(QuestChainComponent) => JsonSerializer.Deserialize<QuestChainComponent>(componentData.Data),

            _ => null
        };
    }
}
