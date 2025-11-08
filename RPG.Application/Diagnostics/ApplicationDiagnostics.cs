using System.Diagnostics;

namespace RPG.Application.Diagnostics;

internal static class ApplicationDiagnostics
{
    public static readonly ActivitySource ActivitySource = new("RPG.Application");
}
