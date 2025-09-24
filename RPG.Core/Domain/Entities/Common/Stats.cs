namespace RPG.Core.Domain.Entities.Common;

public class Stats
{
    // melle atack powers
    public int Strength { get; set; }
    // range atack powers
    public int Agility { get; set; }
    // magic atack powers
    public int Intelligence { get; set; }
    // health and mana regeneration
    public int Wisdom { get; set; }
    // critical chance, miss chance, bartering
    public int Dexterity{ get; set; }
    // health points, life steal, health regeneration
    public int Vitality { get; set; }

    public int MagicResist { get; set; }
    public int NatureResist { get; set; }
    public int MisticResist { get; set; }

    public int Armor { get; set; }

    public int CritChance { get; set; }
    public int HitChance { get; set; }
    public int AttackSpeed { get; set; }
    public int MoveSpeed { get; set; }    
}