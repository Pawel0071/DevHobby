namespace RPG.DesktopClient.Avalonia.ViewModels;

internal sealed class MessageViewModel : ViewModelBase
{
    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }
}
