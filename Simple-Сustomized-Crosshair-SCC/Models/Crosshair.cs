using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Simple_Customized_Crosshair_SCC.Models
{
    /// <summary>
    /// Модель прицела
    /// </summary>
    public class Crosshair : INotifyPropertyChanged
    {
        private string _name = "New Crosshair";
        private System.Windows.Media.Color _color = Colors.Red;
        private double _size = 20;
        private double _thickness = 2;
        private double _opacity = 1.0;
        private CrosshairType _type = CrosshairType.Cross;
        private string _customData = string.Empty;

        // Свойства центральной точки
        private bool _showCenterDot = true;
        private System.Windows.Media.Color _dotColor = Colors.Red;
        private double _dotSize = 4.0;

        // Свойства линий перекрестия
        private bool _showCrosshairLines = true;
        private System.Windows.Media.Color _lineColor = Colors.Red;
        private double _lineLength = 8.0;
        private double _lineThickness = 2.0;
        private double _gap = 2.0;

        // Индивидуальное управление линиями
        private bool _showTopLine = true;
        private bool _showBottomLine = true;
        private bool _showLeftLine = true;
        private bool _showRightLine = true;

        // Свойства обводки
        private bool _showOutline = true;
        private System.Windows.Media.Color _outlineColor = Colors.Black;
        private double _outlineThickness = 1.0;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public System.Windows.Media.Color Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public double Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        public double Thickness
        {
            get => _thickness;
            set { _thickness = value; OnPropertyChanged(); }
        }

        public double Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(); }
        }

        public CrosshairType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public string CustomData
        {
            get => _customData;
            set { _customData = value; OnPropertyChanged(); }
        }

        // Центральная точка
        public bool ShowCenterDot
        {
            get => _showCenterDot;
            set { _showCenterDot = value; OnPropertyChanged(); }
        }

        public System.Windows.Media.Color DotColor
        {
            get => _dotColor;
            set { _dotColor = value; OnPropertyChanged(); }
        }

        public double DotSize
        {
            get => _dotSize;
            set { _dotSize = value; OnPropertyChanged(); }
        }

        // Линии перекрестия
        public bool ShowCrosshairLines
        {
            get => _showCrosshairLines;
            set { _showCrosshairLines = value; OnPropertyChanged(); }
        }

        public System.Windows.Media.Color LineColor
        {
            get => _lineColor;
            set { _lineColor = value; OnPropertyChanged(); }
        }

        public double LineLength
        {
            get => _lineLength;
            set { _lineLength = value; OnPropertyChanged(); }
        }

        public double LineThickness
        {
            get => _lineThickness;
            set { _lineThickness = value; OnPropertyChanged(); }
        }

        public double Gap
        {
            get => _gap;
            set { _gap = value; OnPropertyChanged(); }
        }

        // Индивидуальное управление линиями
        public bool ShowTopLine
        {
            get => _showTopLine;
            set { _showTopLine = value; OnPropertyChanged(); }
        }

        public bool ShowBottomLine
        {
            get => _showBottomLine;
            set { _showBottomLine = value; OnPropertyChanged(); }
        }

        public bool ShowLeftLine
        {
            get => _showLeftLine;
            set { _showLeftLine = value; OnPropertyChanged(); }
        }

        public bool ShowRightLine
        {
            get => _showRightLine;
            set { _showRightLine = value; OnPropertyChanged(); }
        }

        // Обводка
        public bool ShowOutline
        {
            get => _showOutline;
            set { _showOutline = value; OnPropertyChanged(); }
        }

        public System.Windows.Media.Color OutlineColor
        {
            get => _outlineColor;
            set { _outlineColor = value; OnPropertyChanged(); }
        }

        public double OutlineThickness
        {
            get => _outlineThickness;
            set { _outlineThickness = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Crosshair Clone()
        {
            return new Crosshair
            {
                Name = this.Name + " Copy",
                Color = this.Color,
                Size = this.Size,
                Thickness = this.Thickness,
                Opacity = this.Opacity,
                Type = this.Type,
                CustomData = this.CustomData,

                // Центральная точка
                ShowCenterDot = this.ShowCenterDot,
                DotColor = this.DotColor,
                DotSize = this.DotSize,

                // Линии перекрестия
                ShowCrosshairLines = this.ShowCrosshairLines,
                LineColor = this.LineColor,
                LineLength = this.LineLength,
                LineThickness = this.LineThickness,
                Gap = this.Gap,

                // Индивидуальное управление линиями
                ShowTopLine = this.ShowTopLine,
                ShowBottomLine = this.ShowBottomLine,
                ShowLeftLine = this.ShowLeftLine,
                ShowRightLine = this.ShowRightLine,

                // Обводка
                ShowOutline = this.ShowOutline,
                OutlineColor = this.OutlineColor,
                OutlineThickness = this.OutlineThickness
            };
        }
    }

    public enum CrosshairType
    {
        Cross,
        Circle,
        Dot,
        Square,
        Triangle,
        Custom
    }
}