namespace RPG.Core.Interfaces;

public interface IAttackable
{
    int CurrentHealth { get; set; }
    int MaxHealth { get; set; }
    void ReceiveDamage(int amount);
    bool IsAlive => CurrentHealth > 0;
}