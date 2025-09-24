using RPG.Core.Domain.Entities.Common;

namespace RPG.Core.Interfaces;

public interface IMovable
{
    Location Position { get; set; }
    void Move(double deltaX, double deltaY, double deltaZ);
}