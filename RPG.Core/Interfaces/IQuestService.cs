using RPG.Core.Common;
using RPG.Domain.Models;
using RPG.Domain.Models.Quests;

namespace RPG.Core.Interfaces;

public interface IQuestService
{
    ServiceResult<bool> AcceptQuest(Character character, Quest quest);
    ServiceResult<bool> CompleteQuest(Character character, Guid questId);
    ServiceResult<bool> UpdateQuestProgress(Character character, Guid questId, string objectiveType, int progress);
    ServiceResult<bool> CanCompleteQuest(Character character, Guid questId);
}
