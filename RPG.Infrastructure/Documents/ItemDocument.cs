using MongoDB.Bson.Serialization.Attributes;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;
using System.Text.Json.Serialization;

namespace RPG.Infrastructure.Documents;

public class ItemDocument : IPersistenceModel
{
    public static string CollectionName => "Items";
    
    // Keep for backward compatibility
    public static string ItemCollection { get; } = "Items";

    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string TypeCode { get; set; } = default!;
    public ItemRarity Rarity { get; set; }
    public int RequiredLevel { get; set; }
    public int StackSize { get; set; }

    public List<string> Tags { get; set; } = new();

    // Komponenty jako dane
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, int>? Modifiers { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SocketNo { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Guid>? SkillIds { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? QuestId { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? StepId { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EquipmentSlot>? EquipmentSlots { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsTwoHanded { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SupportsDualWield { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsUniqueEquip { get; set; }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? UsedInItemIds { get; set; }
}
