using System.Windows.Forms;

namespace Simple_Customized_Crosshair_SCC.Services
{
    /// <summary>
    /// Сервис для работы с мониторами
    /// </summary>
    public class MonitorService
    {
        public System.Collections.Generic.List<ScreenInfo> GetScreens()
        {
            var screens = new System.Collections.Generic.List<ScreenInfo>();

            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                var screen = Screen.AllScreens[i];
                screens.Add(new ScreenInfo
                {
                    Index = i,
                    Name = $"Display {i + 1}",
                    Bounds = screen.Bounds,
                    IsPrimary = screen.Primary
                });
            }

            return screens;
        }

        public ScreenInfo? GetScreen(int index)
        {
            var screens = GetScreens();
            return screens.Count > index ? screens[index] : null;
        }
    }
}