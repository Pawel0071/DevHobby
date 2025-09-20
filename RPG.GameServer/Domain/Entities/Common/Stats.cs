using RPG.GameServer.Domain.Types;

namespace RPG.GameServer.Domain.Entities.Common;

public class Stats
{
    // 📦 Główne statystyki
    public int Strength { get; set; } = 0;     // wpływa na obrażenia i możliwość noszenia ekwipunku
    public int Dexterity { get; set; } = 0;    // wpływa na trafienie, blok, obronę
    public int Vitality { get; set; } = 0;     // wpływa na Life i Stamina (Life pomijamy)
    public int Energy { get; set; } = 0;       // wpływa na Manę (pomijamy)

    // 🛡️ Statystyki pochodne
    public int Stamina => Vitality * 1 + 25;   // energia do biegania
    public int AttackRating => Dexterity * 5; // szansa na trafienie
    public int Defense => Dexterity * 2;      // szansa na unik
    public int BlockChance => Dexterity / 2;  // szansa na blok tarczą

    // 🔥 Odporności żywiołowe (max 75% bez gearu)
    public int FireResist { get; set; } = 0;
    public int ColdResist { get; set; } = 0;
    public int LightningResist { get; set; } = 0;
    public int PoisonResist { get; set; } = 0;

    // ✨ Modyfikatory specjalne
    public int MagicFind { get; set; } = 0;           // % szansa na lepszy loot
    public int FasterCastRate { get; set; } = 0;      // % szybsze rzucanie czarów
    public int FasterHitRecovery { get; set; } = 0;   // % szybsze wyjście z ogłuszenia
    public int FasterRunWalk { get; set; } = 0;       // % szybsze poruszanie się
    public int IncreasedAttackSpeed { get; set; } = 0;// % szybsze ataki
    public int LifeLeech { get; set; } = 0;           // % życia odzyskiwanego przy ataku
    public int ManaLeech { get; set; } = 0;           // % many odzyskiwanej przy ataku
    public int DamageReduction { get; set; } = 0;     // % redukcji obrażeń fizycznych
    public int CannotBeFrozen { get; set; } = 0;      // 1 = aktywne, 0 = nie

    // 🧠 Utility
    public bool IsImmuneTo(Effect effect)
    {
        return effect switch
        {
            Effect.Frozen => CannotBeFrozen == 1,
            _ => false
        };
    }
}