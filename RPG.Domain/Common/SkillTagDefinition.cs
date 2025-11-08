namespace RPG.Domain.Common;

/// <summary>
///     Defines standard tags for skills.
///     Tags are used for categorization, filtering, and gameplay mechanics.
/// </summary>
public static class SkillTagDefinition
{
    // Type tags
    public const string Offensive = "offensive";
    public const string Defensive = "defensive";
    public const string Utility = "utility";
    public const string Passive = "passive";
    public const string Active = "active";
    public const string Toggle = "toggle";
    public const string Channeled = "channeled";

    // Target type tags
    public const string SingleTarget = "single-target";
    public const string AreaOfEffect = "area-of-effect";
    public const string SelfOnly = "self-only";
    public const string NoTarget = "no-target";
    public const string GroundTarget = "ground-target";
    public const string Cone = "cone";
    public const string Line = "line";
    public const string Circle = "circle";

    // Damage type tags
    public const string Physical = "physical";
    public const string Magical = "magical";
    public const string Fire = "fire";
    public const string Ice = "ice";
    public const string Lightning = "lightning";
    public const string Poison = "poison";
    public const string Holy = "holy";
    public const string Shadow = "shadow";
    public const string Nature = "nature";

    // Effect tags
    public const string Damage = "damage";
    public const string Healing = "healing";
    public const string Buff = "buff";
    public const string Debuff = "debuff";
    public const string Stun = "stun";
    public const string Slow = "slow";
    public const string Root = "root";
    public const string Silence = "silence";
    public const string Disarm = "disarm";
    public const string Blind = "blind";
    public const string Fear = "fear";
    public const string Taunt = "taunt";
    public const string Shield = "shield";
    public const string DamageOverTime = "damage-over-time";
    public const string HealOverTime = "heal-over-time";

    // Movement tags
    public const string RequiresStanding = "requires-standing";
    public const string CastWhileMoving = "cast-while-moving";
    public const string Immobilizes = "immobilizes";
    public const string Teleport = "teleport";
    public const string Dash = "dash";
    public const string Knockback = "knockback";
    public const string Pull = "pull";

    // Resource tags
    public const string CostsMana = "costs-mana";
    public const string CostsHealth = "costs-health";
    public const string CostsEnergy = "costs-energy";
    public const string CostsRage = "costs-rage";
    public const string GeneratesResource = "generates-resource";
    public const string NoResourceCost = "no-resource-cost";

    // Cooldown tags
    public const string ShortCooldown = "short-cooldown";
    public const string MediumCooldown = "medium-cooldown";
    public const string LongCooldown = "long-cooldown";
    public const string NoCooldown = "no-cooldown";
    public const string GlobalCooldown = "global-cooldown";

    // Special tags
    public const string Interruptible = "interruptible";
    public const string Uninterruptible = "uninterruptible";
    public const string RequiresWeapon = "requires-weapon";
    public const string RequiresMelee = "requires-melee";
    public const string RequiresRanged = "requires-ranged";
    public const string Ultimate = "ultimate";
    public const string Combo = "combo";
    public const string Chain = "chain";
    public const string Stackable = "stackable";
    public const string Dispellable = "dispellable";
    public const string Cleansable = "cleansable";
}
