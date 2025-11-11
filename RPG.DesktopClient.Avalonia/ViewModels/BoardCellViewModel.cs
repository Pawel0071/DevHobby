using Avalonia.Media;

namespace RPG.DesktopClient.Avalonia.ViewModels;

internal sealed class BoardCellViewModel : ViewModelBase
{
    private string _glyph = string.Empty;
    private IBrush _foreground = Brushes.LightGray;
    private IBrush _background = Brushes.Black;
    private string? _tooltip;

    public string Glyph
    {
        get => _glyph;
        set => SetProperty(ref _glyph, value);
    }

    public IBrush Foreground
    {
        get => _foreground;
        set => SetProperty(ref _foreground, value);
    }

    public IBrush Background
    {
        get => _background;
        set => SetProperty(ref _background, value);
    }

    public string? Tooltip
    {
        get => _tooltip;
        set => SetProperty(ref _tooltip, value);
    }
}
