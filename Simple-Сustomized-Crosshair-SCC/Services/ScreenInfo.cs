using System.Drawing;

namespace Simple_Customized_Crosshair_SCC.Services
{
    public class ScreenInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public Rectangle Bounds { get; set; }
        public bool IsPrimary { get; set; }

        public override string ToString()
        {
            return $"{Name} {(IsPrimary ? "(Primary)" : "")} - {Bounds.Width}x{Bounds.Height}";
        }
    }
}