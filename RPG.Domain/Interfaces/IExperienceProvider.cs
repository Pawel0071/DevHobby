namespace RPG.Domain.Interfaces;

public interface IExperienceProvider
{
    Dictionary<int, int> ExperienceTable { get; }
    int GetRequiredExperience(int level);
    bool IsMaxLevel(int level);
}
