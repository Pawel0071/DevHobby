using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RPG.Client.Stride.Rendering;

internal readonly struct RenderEntity
{
    public RenderEntity(Vector3 position, float size, Color color, float rotationDegrees = 0f, Texture2D? texture = null)
    {
        Position = position;
        Size = size;
        Color = color;
        RotationDegrees = rotationDegrees;
        Texture = texture;
    }

    public Vector3 Position { get; }
    public float Size { get; }
    public Color Color { get; }
    public float RotationDegrees { get; }
    public Texture2D? Texture { get; }
}
