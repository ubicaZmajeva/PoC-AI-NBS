
namespace AiComplaintAssistant.Layout;

public partial class MainLayout
{
    private MudTheme _theme = new MudTheme()
    {
        PaletteLight = new ()
        {
            Primary = "#00ADD8",
            Secondary = Colors.Blue.Lighten2,
            AppbarBackground = "#00ADD8",
        }
    };
    private bool _drawerOpen = true;
    
    private void OnMenuClicked() => _drawerOpen = !_drawerOpen;
}
