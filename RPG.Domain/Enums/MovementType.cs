namespace RPG.Domain.Enums;

public enum MovementType
{
    None,
    Dash, // Quick forward movement
    Teleport, // Instant position change
    Blink, // Short-range teleport
    Knockback, // Push target away
    Pull, // Pull target closer
    Leap, // Jump to location
    Charge, // Rush to target
    Slow, // Reduce movement speed
    Immobilize // Cannot move
}
