namespace RPG.Domain.Common.Interfaces;

public interface ILevel
{
    int Level { get; set; }
    int Experience { get; set; }
    int ExperienceToNextLevel { get; set; }
}