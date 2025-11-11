namespace RPG.DesktopClient.Avalonia.ViewModels;

internal sealed class EntityInfoViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _position = string.Empty;
    private string _details = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    public string Details
    {
        get => _details;
        set => SetProperty(ref _details, value);
    }
}
