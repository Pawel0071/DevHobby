namespace RPG.Domain.Enums;

public enum CrowdControlType
{
    Stun, // Cannot move or cast
    Root, // Cannot move, can cast
    Silence, // Can move, cannot cast
    Disarm, // Cannot use weapon abilities
    Blind, // Reduced accuracy
    Fear, // Runs away randomly
    Charm, // Fights for enemy
    Sleep, // Incapacitated until damage
    Slow, // Reduced movement speed
    Snare, // Movement impaired
    Taunt, // Forced to attack specific target
    Polymorph // Transformed, cannot act
}
