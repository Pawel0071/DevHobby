using RPG.Domain.Common.Interfaces;

namespace RPG.Domain.Common;

public sealed class ErrorCodeDefinition : IDictionaryEntry<ErrorCodeDefinition>
{
    public static readonly ErrorCodeDefinition None = new() { Code = "none" };
    public static readonly ErrorCodeDefinition Unknown = new() { Code = "unknown" };

    public static readonly ErrorCodeDefinition InvalidOperation =
        new() { Code = "invalid_operation", Message = "Operacja niedozwolona", Category = "Logic" };

    public static readonly ErrorCodeDefinition ItemNotFound = new()
    {
        Code = "item_not_found", Message = "Nie znaleziono przedmiotu", Category = "Inventory"
    };

    public static readonly ErrorCodeDefinition StackLimitReached = new()
    {
        Code = "stack_limit", Message = "Limit stosu przekroczony", Category = "Inventory"
    };

    public static readonly ErrorCodeDefinition NoFreeSlot =
        new() { Code = "no_free_slot", Message = "Brak wolnego slotu", Category = "Inventory" };

    public static readonly ErrorCodeDefinition ItemNotEquippable = new()
    {
        Code = "item_not_equippable", Message = "Przedmiot nie może być wyposażony", Category = "Equipment"
    };

    public static readonly ErrorCodeDefinition EquipmentMetadataMissing = new()
    {
        Code = "equipment_metadata_missing", Message = "Brak danych o wyposażeniu", Category = "Equipment"
    };

    public static readonly ErrorCodeDefinition EquipmentSlotMismatch = new()
    {
        Code = "equipment_slot_mismatch", Message = "Nieprawidłowy slot wyposażenia", Category = "Equipment"
    };

    public static readonly ErrorCodeDefinition UniqueEquipViolation = new()
    {
        Code = "unique_equip_violation", Message = "Przedmiot może być założony tylko raz", Category = "Equipment"
    };

    public static readonly ErrorCodeDefinition SkillRequirementLevelNotMet = new()
    {
        Code = "skill_level_requirement", Message = "Zbyt niski poziom postaci", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition SkillRequirementClassMismatch = new()
    {
        Code = "skill_class_mismatch", Message = "Niewłaściwa klasa postaci", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition SkillRequirementWeaponMissing = new()
    {
        Code = "skill_weapon_missing", Message = "Brak wymaganego uzbrojenia", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition SkillRequirementStatNotMet = new()
    {
        Code = "skill_stat_requirement", Message = "Niewystarczające atrybuty", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition SkillRequirementResourceInsufficient = new()
    {
        Code = "skill_resource_requirement", Message = "Za mało zasobów", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition SkillPrerequisiteMissing = new()
    {
        Code = "skill_prerequisite_missing", Message = "Brak wymaganych umiejętności", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition SkillAlreadyKnown = new()
    {
        Code = "skill_already_known", Message = "Umiejętność jest już znana", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition SkillNotKnown = new()
    {
        Code = "skill_not_known", Message = "Umiejętność nie jest znana", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition SkillUnavailable = new()
    {
        Code = "skill_unavailable", Message = "Umiejętność jest niedostępna", Category = "Skill"
    };

    public static readonly ErrorCodeDefinition AlreadyMaxLevel = new() { Code = "char_max_lecel" };
    public string? Message { get; init; } // np. "Operacja niedozwolona"
    public string? Category { get; init; } // np. "Logic", "Inventory", "Combat"
    public required string Code { get; init; } // np. "invalid_operation"

    public static IEnumerable<ErrorCodeDefinition> Predefined =>
    [
        None,
        InvalidOperation,
        ItemNotFound,
        StackLimitReached,
        NoFreeSlot,
        ItemNotEquippable,
        EquipmentMetadataMissing,
        EquipmentSlotMismatch,
        UniqueEquipViolation,
        SkillRequirementLevelNotMet,
        SkillRequirementClassMismatch,
        SkillRequirementWeaponMissing,
        SkillRequirementStatNotMet,
        SkillRequirementResourceInsufficient,
        SkillPrerequisiteMissing,
        SkillAlreadyKnown,
        SkillNotKnown,
        SkillUnavailable
    ];
}
