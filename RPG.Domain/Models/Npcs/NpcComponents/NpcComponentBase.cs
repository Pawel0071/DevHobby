// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Domain/Models/Npcs/NpcComponents/NpcComponentBase.cs
using System;

namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Shared base helper that wires <see cref="INpcComponent"/> lifecycle plumbing so feature components stay lean.
/// </summary>
public abstract class NpcComponentBase : INpcComponent
{
    public Guid OwnerId { get; private set; }
    public Npc? Owner { get; private set; }
    public bool IsAttached => Owner is not null;

    public abstract string ComponentName { get; }
    public abstract string ComponentType { get; }

    public virtual void Attach(Npc owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        OwnerId = owner.Id;
    }

    public virtual void Detach()
    {
        Owner = null;
        OwnerId = Guid.Empty;
    }

    public virtual void Tick(TimeSpan deltaTime)
    {
        // Most components are data-only; override if runtime behavior is needed.
    }
}

