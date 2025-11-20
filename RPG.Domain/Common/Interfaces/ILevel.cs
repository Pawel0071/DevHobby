namespace RPG.Domain.Common.Interfaces;

public interface ILevel
{
    Guid Id { get; }
    string Name { get; }
    int Level { get; set; }
    long Experience { get; set; }
    long ExperienceToNextLevel { get; }
}
