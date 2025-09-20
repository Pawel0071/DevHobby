using RPG.GameServer.Protos;

namespace RPG.GameServer.Domain.Types;

public enum CharacterClass { Warrior, Mage, Warlock, Paladin, Shaman, Monk, Assassin, Druid }
public enum NpcType { Vendor, QuestGiver, Healer, Informant }
public enum MonsterType { Undead, Demon, Beast, Construct, Human, Ghost }

public enum Effect
{
    Poisoned,
    Frozen,
    Burning,
    Silenced,
    Stunned,
    Cursed
}

public enum SkillType
{
    // 🔥 Ofensywne
    MeleeAttack,        // Atak wręcz
    RangedAttack,       // Atak dystansowy
    MagicDamage,        // Czar ofensywny
    AreaOfEffect,       // Obrażenia obszarowe
    DamageOverTime,     // Efekt typu poison, burn

    // 🛡️ Defensywne
    Shield,             // Tarcza magiczna lub fizyczna
    Dodge,              // Unik
    ArmorBoost,         // Wzmocnienie obrony
    ResistanceBoost,    // Odporność na żywioły

    // ✨ Wsparcie
    Heal,               // Leczenie
    ManaRestore,        // Odzyskiwanie many
    Buff,               // Wzmocnienie statystyk
    Debuff,             // Osłabienie przeciwnika
    Summon,             // Przyzwanie jednostki

    // 🌀 Kontrola
    Stun,               // Ogłuszenie
    Freeze,             // Zamrożenie
    Slow,               // Spowolnienie
    Knockback,          // Odepchnięcie

    // 🧠 Specjalne
    Passive,            // Umiejętność pasywna
    Aura,               // Efekt obszarowy aktywny
    Teleport,           // Przemieszczenie
    Trap,               // Pułapka
    Curse,              // Klątwa

    // 🎭 Interakcyjne
    Charm,              // Zauroczenie NPC
    Reveal,             // Ujawnienie ukrytych przeciwników
    Detect,             // Wykrycie pułapek lub magii
}


public class Item
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public ItemType Type { get; set; }
    public Dictionary<string, int> Modifiers { get; set; }
    public int RequiredLevel { get; set; }
}

public class EquipmentSlots
{
    public Item Head { get; set; }
    public Item Chest { get; set; }
    public Item Weapon { get; set; }
    public Item Shield { get; set; }
    public Item Boots { get; set; }
    public Item Gloves { get; set; }
    public List<Item> Rings { get; set; }
    public Item Amulet { get; set; }
}

public class LootDrop
{
    public string ItemId { get; set; }
    public float DropChance { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
}