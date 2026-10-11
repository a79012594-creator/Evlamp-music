namespace EVLAMP.Music;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        MainPage = new NavigationPage(new MainPage())
        {
            BarBackgroundColor = Color.FromArgb("#08060D"),
            BarTextColor = Color.FromArgb("#FFFFFF")
        };
    }
}
