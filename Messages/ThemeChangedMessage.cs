using CommunityToolkit.Mvvm.Messaging.Messages;



public class ThemeChangedMessage : ValueChangedMessage<AppTheme>
{
    public ThemeChangedMessage(AppTheme value) : base(value) { }
}
