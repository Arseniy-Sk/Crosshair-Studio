using Simple_Customized_Crosshair_SCC.Models;
using Simple_Customized_Crosshair_SCC.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Simple_Customized_Crosshair_SCC.Views
{
    /// <summary>
    /// Окно для отображения прицела
    /// </summary>
    public partial class CrosshairWindow : Window
    {
        private Crosshair _currentCrosshair;
        private DisplaySettings _displaySettings;
        private MonitorService _monitorService;

        public CrosshairWindow(Crosshair crosshair, DisplaySettings displaySettings)
        {
            InitializeComponent();
            _currentCrosshair = crosshair;
            _displaySettings = displaySettings;
            _monitorService = new MonitorService();

            ConfigureWindow();
            UpdateCrosshair(crosshair);
        }

        private void ConfigureWindow()
        {
            // Настройка позиции окна на выбранном мониторе
            var screens = _monitorService.GetScreens();
            if (_displaySettings.SelectedMonitorIndex < screens.Count && screens.Count > 0)
            {
                var screen = screens[_displaySettings.SelectedMonitorIndex];
                Left = screen.Bounds.Left;
                Top = screen.Bounds.Top;
                Width = screen.Bounds.Width;
                Height = screen.Bounds.Height;
            }
            else
            {
                // По умолчанию используем основной монитор
                var primaryScreen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.First();
                if (primaryScreen != null)
                {
                    Left = primaryScreen.Bounds.Left;
                    Top = primaryScreen.Bounds.Top;
                    Width = primaryScreen.Bounds.Width;
                    Height = primaryScreen.Bounds.Height;
                }
            }

            // Настройка свойств окна
            Topmost = _displaySettings.AlwaysOnTop;

            // Настройка клик-сквозь режима
            UpdateClickThrough();
        }

        private void UpdateClickThrough()
        {
            if (_displaySettings.ClickThrough)
            {
                SetClickThrough();
            }
            else
            {
                SetNormalMode();
            }
        }

        private void SetClickThrough()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            // Получаем текущий стиль окна
            var extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

            // Устанавливаем флаги для прозрачного и клик-сквозь окна
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                extendedStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);

            // Дополнительные настройки для полного игнорирования кликов
            this.IsHitTestVisible = false;
            CrosshairCanvas.IsHitTestVisible = false;
        }

        private void SetNormalMode()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            // Получаем текущий стиль окна
            var extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

            // Убираем флаги прозрачности и клик-сквозь
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT & ~NativeMethods.WS_EX_LAYERED);

            // Восстанавливаем обработку кликов
            this.IsHitTestVisible = true;
            CrosshairCanvas.IsHitTestVisible = true;
        }

        public void UpdateCrosshair(Crosshair crosshair)
        {
            _currentCrosshair = crosshair;
            Dispatcher.Invoke(() =>
            {
                CrosshairCanvas.Children.Clear();
                DrawCrosshair();
            });
        }

        public void UpdateDisplaySettings(DisplaySettings displaySettings)
        {
            _displaySettings = displaySettings;
            Dispatcher.Invoke(() =>
            {
                ConfigureWindow();
                UpdateCrosshair(_currentCrosshair);
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            UpdateClickThrough();
        }

        private void DrawCrosshair()
        {
            var centerX = CrosshairCanvas.ActualWidth / 2;
            var centerY = CrosshairCanvas.ActualHeight / 2;

            if (double.IsNaN(centerX) || double.IsNaN(centerY) || centerX <= 0 || centerY <= 0)
                return;

            switch (_currentCrosshair.Type)
            {
                case CrosshairType.Cross:
                    DrawCross(centerX, centerY);
                    break;
                case CrosshairType.Circle:
                    DrawCircle(centerX, centerY);
                    break;
                case CrosshairType.Dot:
                    DrawDot(centerX, centerY);
                    break;
                case CrosshairType.Square:
                    DrawSquare(centerX, centerY);
                    break;
                case CrosshairType.Triangle:
                    DrawTriangle(centerX, centerY);
                    break;
                case CrosshairType.Custom:
                    DrawCustomCrosshair(centerX, centerY);
                    break;
            }
        }

        private void DrawCross(double centerX, double centerY)
        {
            var brush = new SolidColorBrush(_currentCrosshair.Color);
            brush.Opacity = _currentCrosshair.Opacity;

            // Обводка для Cross типа
            if (_currentCrosshair.ShowOutline && _currentCrosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(_currentCrosshair.OutlineColor);
                outlineBrush.Opacity = _currentCrosshair.Opacity;

                // Горизонтальная обводка
                var horizontalOutline = new Line
                {
                    X1 = centerX - _currentCrosshair.Size / 2,
                    Y1 = centerY,
                    X2 = centerX + _currentCrosshair.Size / 2,
                    Y2 = centerY,
                    Stroke = outlineBrush,
                    StrokeThickness = _currentCrosshair.Thickness + _currentCrosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };

                // Вертикальная обводка
                var verticalOutline = new Line
                {
                    X1 = centerX,
                    Y1 = centerY - _currentCrosshair.Size / 2,
                    X2 = centerX,
                    Y2 = centerY + _currentCrosshair.Size / 2,
                    Stroke = outlineBrush,
                    StrokeThickness = _currentCrosshair.Thickness + _currentCrosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };

                CrosshairCanvas.Children.Add(horizontalOutline);
                CrosshairCanvas.Children.Add(verticalOutline);
            }

            // Горизонтальная линия
            var horizontalLine = new Line
            {
                X1 = centerX - _currentCrosshair.Size / 2,
                Y1 = centerY,
                X2 = centerX + _currentCrosshair.Size / 2,
                Y2 = centerY,
                Stroke = brush,
                StrokeThickness = _currentCrosshair.Thickness,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            // Вертикальная линия
            var verticalLine = new Line
            {
                X1 = centerX,
                Y1 = centerY - _currentCrosshair.Size / 2,
                X2 = centerX,
                Y2 = centerY + _currentCrosshair.Size / 2,
                Stroke = brush,
                StrokeThickness = _currentCrosshair.Thickness,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            CrosshairCanvas.Children.Add(horizontalLine);
            CrosshairCanvas.Children.Add(verticalLine);
        }

        private void DrawCircle(double centerX, double centerY)
        {
            var brush = new SolidColorBrush(_currentCrosshair.Color);
            brush.Opacity = _currentCrosshair.Opacity;

            // Обводка для Circle типа
            if (_currentCrosshair.ShowOutline && _currentCrosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(_currentCrosshair.OutlineColor);
                outlineBrush.Opacity = _currentCrosshair.Opacity;

                var outlineEllipse = new Ellipse
                {
                    Width = _currentCrosshair.Size + _currentCrosshair.OutlineThickness * 2,
                    Height = _currentCrosshair.Size + _currentCrosshair.OutlineThickness * 2,
                    Stroke = outlineBrush,
                    StrokeThickness = _currentCrosshair.Thickness + _currentCrosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(outlineEllipse, centerX - (_currentCrosshair.Size + _currentCrosshair.OutlineThickness * 2) / 2);
                Canvas.SetTop(outlineEllipse, centerY - (_currentCrosshair.Size + _currentCrosshair.OutlineThickness * 2) / 2);
                CrosshairCanvas.Children.Add(outlineEllipse);
            }

            var ellipse = new Ellipse
            {
                Width = _currentCrosshair.Size,
                Height = _currentCrosshair.Size,
                Stroke = brush,
                StrokeThickness = _currentCrosshair.Thickness,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(ellipse, centerX - _currentCrosshair.Size / 2);
            Canvas.SetTop(ellipse, centerY - _currentCrosshair.Size / 2);
            CrosshairCanvas.Children.Add(ellipse);
        }

        private void DrawDot(double centerX, double centerY)
        {
            var brush = new SolidColorBrush(_currentCrosshair.Color);
            brush.Opacity = _currentCrosshair.Opacity;

            var dotSize = Math.Max(2, _currentCrosshair.Size / 4);

            // Обводка для Dot типа
            if (_currentCrosshair.ShowOutline && _currentCrosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(_currentCrosshair.OutlineColor);
                outlineBrush.Opacity = _currentCrosshair.Opacity;

                var outlineEllipse = new Ellipse
                {
                    Width = dotSize + _currentCrosshair.OutlineThickness * 2,
                    Height = dotSize + _currentCrosshair.OutlineThickness * 2,
                    Stroke = outlineBrush,
                    StrokeThickness = _currentCrosshair.OutlineThickness,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(outlineEllipse, centerX - (dotSize + _currentCrosshair.OutlineThickness * 2) / 2);
                Canvas.SetTop(outlineEllipse, centerY - (dotSize + _currentCrosshair.OutlineThickness * 2) / 2);
                CrosshairCanvas.Children.Add(outlineEllipse);
            }

            var ellipse = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = brush,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(ellipse, centerX - dotSize / 2);
            Canvas.SetTop(ellipse, centerY - dotSize / 2);
            CrosshairCanvas.Children.Add(ellipse);
        }

        private void DrawSquare(double centerX, double centerY)
        {
            var brush = new SolidColorBrush(_currentCrosshair.Color);
            brush.Opacity = _currentCrosshair.Opacity;

            // Обводка для Square типа
            if (_currentCrosshair.ShowOutline && _currentCrosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(_currentCrosshair.OutlineColor);
                outlineBrush.Opacity = _currentCrosshair.Opacity;

                var outlineRectangle = new System.Windows.Shapes.Rectangle
                {
                    Width = _currentCrosshair.Size + _currentCrosshair.OutlineThickness * 2,
                    Height = _currentCrosshair.Size + _currentCrosshair.OutlineThickness * 2,
                    Stroke = outlineBrush,
                    StrokeThickness = _currentCrosshair.Thickness + _currentCrosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(outlineRectangle, centerX - (_currentCrosshair.Size + _currentCrosshair.OutlineThickness * 2) / 2);
                Canvas.SetTop(outlineRectangle, centerY - (_currentCrosshair.Size + _currentCrosshair.OutlineThickness * 2) / 2);
                CrosshairCanvas.Children.Add(outlineRectangle);
            }

            var rectangle = new System.Windows.Shapes.Rectangle
            {
                Width = _currentCrosshair.Size,
                Height = _currentCrosshair.Size,
                Stroke = brush,
                StrokeThickness = _currentCrosshair.Thickness,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rectangle, centerX - _currentCrosshair.Size / 2);
            Canvas.SetTop(rectangle, centerY - _currentCrosshair.Size / 2);
            CrosshairCanvas.Children.Add(rectangle);
        }

        private void DrawTriangle(double centerX, double centerY)
        {
            var brush = new SolidColorBrush(_currentCrosshair.Color);
            brush.Opacity = _currentCrosshair.Opacity;

            // Обводка для Triangle типа
            if (_currentCrosshair.ShowOutline && _currentCrosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(_currentCrosshair.OutlineColor);
                outlineBrush.Opacity = _currentCrosshair.Opacity;

                var outlinePolygon = new Polygon
                {
                    Stroke = outlineBrush,
                    StrokeThickness = _currentCrosshair.Thickness + _currentCrosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false,
                    Points = new PointCollection
                    {
                        new System.Windows.Point(centerX, centerY - _currentCrosshair.Size / 2),
                        new System.Windows.Point(centerX - _currentCrosshair.Size / 2, centerY + _currentCrosshair.Size / 2),
                        new System.Windows.Point(centerX + _currentCrosshair.Size / 2, centerY + _currentCrosshair.Size / 2)
                    }
                };

                CrosshairCanvas.Children.Add(outlinePolygon);
            }

            var polygon = new Polygon
            {
                Stroke = brush,
                StrokeThickness = _currentCrosshair.Thickness,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false,
                Points = new PointCollection
                {
                    new System.Windows.Point(centerX, centerY - _currentCrosshair.Size / 2),
                    new System.Windows.Point(centerX - _currentCrosshair.Size / 2, centerY + _currentCrosshair.Size / 2),
                    new System.Windows.Point(centerX + _currentCrosshair.Size / 2, centerY + _currentCrosshair.Size / 2)
                }
            };

            CrosshairCanvas.Children.Add(polygon);
        }

        private void DrawCustomCrosshair(double centerX, double centerY)
        {
            // Центральная точка
            if (_currentCrosshair.ShowCenterDot)
            {
                DrawCenterDot(centerX, centerY);
            }

            // Линии перекрестия
            if (_currentCrosshair.ShowCrosshairLines)
            {
                DrawCrosshairLines(centerX, centerY);
            }

            // Обводка
            if (_currentCrosshair.ShowOutline && _currentCrosshair.OutlineThickness > 0)
            {
                DrawCrosshairOutline(centerX, centerY);
            }
        }

        private void DrawCenterDot(double centerX, double centerY)
        {
            var dotBrush = new SolidColorBrush(_currentCrosshair.DotColor);
            dotBrush.Opacity = _currentCrosshair.Opacity;

            var dotSize = Math.Max(1, _currentCrosshair.DotSize);

            // Обводка для центральной точки
            if (_currentCrosshair.ShowOutline && _currentCrosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(_currentCrosshair.OutlineColor);
                outlineBrush.Opacity = _currentCrosshair.Opacity;

                var outlineEllipse = new Ellipse
                {
                    Width = dotSize + _currentCrosshair.OutlineThickness * 2,
                    Height = dotSize + _currentCrosshair.OutlineThickness * 2,
                    Stroke = outlineBrush,
                    StrokeThickness = _currentCrosshair.OutlineThickness,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(outlineEllipse, centerX - (dotSize + _currentCrosshair.OutlineThickness * 2) / 2);
                Canvas.SetTop(outlineEllipse, centerY - (dotSize + _currentCrosshair.OutlineThickness * 2) / 2);
                CrosshairCanvas.Children.Add(outlineEllipse);
            }

            var ellipse = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = dotBrush,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(ellipse, centerX - dotSize / 2);
            Canvas.SetTop(ellipse, centerY - dotSize / 2);
            CrosshairCanvas.Children.Add(ellipse);
        }

        private void DrawCrosshairLines(double centerX, double centerY)
        {
            var lineBrush = new SolidColorBrush(_currentCrosshair.LineColor);
            lineBrush.Opacity = _currentCrosshair.Opacity;

            // Верхняя линия
            if (_currentCrosshair.ShowTopLine)
            {
                var topLine = new Line
                {
                    X1 = centerX,
                    Y1 = centerY - _currentCrosshair.Gap - _currentCrosshair.LineLength,
                    X2 = centerX,
                    Y2 = centerY - _currentCrosshair.Gap,
                    Stroke = lineBrush,
                    StrokeThickness = _currentCrosshair.LineThickness,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };
                CrosshairCanvas.Children.Add(topLine);
            }

            // Нижняя линия
            if (_currentCrosshair.ShowBottomLine)
            {
                var bottomLine = new Line
                {
                    X1 = centerX,
                    Y1 = centerY + _currentCrosshair.Gap,
                    X2 = centerX,
                    Y2 = centerY + _currentCrosshair.Gap + _currentCrosshair.LineLength,
                    Stroke = lineBrush,
                    StrokeThickness = _currentCrosshair.LineThickness,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };
                CrosshairCanvas.Children.Add(bottomLine);
            }

            // Левая линия
            if (_currentCrosshair.ShowLeftLine)
            {
                var leftLine = new Line
                {
                    X1 = centerX - _currentCrosshair.Gap - _currentCrosshair.LineLength,
                    Y1 = centerY,
                    X2 = centerX - _currentCrosshair.Gap,
                    Y2 = centerY,
                    Stroke = lineBrush,
                    StrokeThickness = _currentCrosshair.LineThickness,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };
                CrosshairCanvas.Children.Add(leftLine);
            }

            // Правая линия
            if (_currentCrosshair.ShowRightLine)
            {
                var rightLine = new Line
                {
                    X1 = centerX + _currentCrosshair.Gap,
                    Y1 = centerY,
                    X2 = centerX + _currentCrosshair.Gap + _currentCrosshair.LineLength,
                    Y2 = centerY,
                    Stroke = lineBrush,
                    StrokeThickness = _currentCrosshair.LineThickness,
                    SnapsToDevicePixels = true,
                    IsHitTestVisible = false
                };
                CrosshairCanvas.Children.Add(rightLine);
            }
        }

        private void DrawCrosshairOutline(double centerX, double centerY)
        {
            var outlineBrush = new SolidColorBrush(_currentCrosshair.OutlineColor);
            outlineBrush.Opacity = _currentCrosshair.Opacity;

            // Обводка для линий
            if (_currentCrosshair.ShowTopLine)
            {
                DrawOutlinedLine(centerX, centerY - _currentCrosshair.Gap - _currentCrosshair.LineLength,
                               centerX, centerY - _currentCrosshair.Gap, outlineBrush);
            }

            if (_currentCrosshair.ShowBottomLine)
            {
                DrawOutlinedLine(centerX, centerY + _currentCrosshair.Gap,
                               centerX, centerY + _currentCrosshair.Gap + _currentCrosshair.LineLength, outlineBrush);
            }

            if (_currentCrosshair.ShowLeftLine)
            {
                DrawOutlinedLine(centerX - _currentCrosshair.Gap - _currentCrosshair.LineLength, centerY,
                               centerX - _currentCrosshair.Gap, centerY, outlineBrush);
            }

            if (_currentCrosshair.ShowRightLine)
            {
                DrawOutlinedLine(centerX + _currentCrosshair.Gap, centerY,
                               centerX + _currentCrosshair.Gap + _currentCrosshair.LineLength, centerY, outlineBrush);
            }
        }

        private void DrawOutlinedLine(double x1, double y1, double x2, double y2, SolidColorBrush outlineBrush)
        {
            var outlineLine = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = outlineBrush,
                StrokeThickness = _currentCrosshair.LineThickness + _currentCrosshair.OutlineThickness * 2,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };
            CrosshairCanvas.Children.Insert(0, outlineLine);
        }
    }

    /// <summary>
    /// Нативные методы для работы с окнами
    /// </summary>
    internal static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}