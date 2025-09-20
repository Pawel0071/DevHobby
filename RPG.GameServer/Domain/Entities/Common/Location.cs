namespace RPG.GameServer.Domain.Entities.Common;

public class Location
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Location() { }

    public Location(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double DistanceTo(Location other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public void MoveBy(double dx, double dy, double dz)
    {
        X += dx;
        Y += dy;
        Z += dz;
    }
}