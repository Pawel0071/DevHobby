using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RPG.Domain.Enums;
using RPG.Domain.Models.Items;

namespace RPG.Infrastructure.Models;

public class ItemDocument : IPersistenceModel
{
    public static string CollectionName => "Items";

    [BsonId]
    [BsonRepresentation(BsonType.String)]
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
