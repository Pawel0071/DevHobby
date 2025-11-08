namespace RPG.Domain.Common.Interfaces;

public interface ILevel
{
    int Level { get; set; }
    long Experience { get; set; }
    long ExperienceToNextLevel { get; }
}
