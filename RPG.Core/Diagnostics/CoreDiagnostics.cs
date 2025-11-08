using System.Diagnostics;

namespace RPG.Core.Diagnostics;

internal static class CoreDiagnostics
{
    public static readonly ActivitySource ActivitySource = new("RPG.Core");
}
