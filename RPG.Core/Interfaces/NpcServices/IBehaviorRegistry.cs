// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Core/Interfaces/NpcServices/IBehaviorRegistry.cs
using RPG.AI.Utility;
using RPG.Domain.Models.Npcs;

namespace RPG.Core.Interfaces.NpcServices;

/// <summary>
/// Rejestr zachowań AI – dobiera i buforuje UtilityAgent dla NPC na podstawie jego komponentów.
/// </summary>
public interface IBehaviorRegistry
{
    UtilityAgent GetOrCreateAgent(Npc npc);
    void Invalidate(Npc npc);
}
