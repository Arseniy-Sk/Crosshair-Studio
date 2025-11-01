using Simple_Customized_Crosshair_SCC.ViewModels;
using Simple_Customized_Crosshair_SCC.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Simple_Customized_Crosshair_SCC.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private bool _isMaximized = false;
        private System.Windows.Point _windowPosition;
        private System.Windows.Size _windowSize;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Подписка на изменения текущего прицела для обновления превью
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentCrosshair))
                {
                    UpdatePreview();
                }
            };

            _viewModel.CurrentCrosshair.PropertyChanged += (s, e) => UpdatePreview();

            Loaded += (s, e) => UpdatePreview();
            MainPreviewCanvas.SizeChanged += (s, e) => UpdatePreview();

            // Добавляем обработчик для перетаскивания окна
            MouseDown += MainWindow_MouseDown;
        }

        #region Window Control Methods

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            e.Handled = true;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
            e.Handled = true;
        }

        private void ToggleMaximizeRestore()
        {
            if (WindowState == WindowState.Maximized)
            {
                RestoreWindow();
            }
            else
            {
                MaximizeWindow();
            }
        }

        private void MaximizeWindow()
        {
            _windowPosition = new System.Windows.Point(Left, Top);
            _windowSize = new System.Windows.Size(Width, Height);

            // Сохраняем текущие границы экрана
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workingArea = screen.WorkingArea;

            WindowState = WindowState.Maximized;
            MaximizeButton.Content = "❐";
            _isMaximized = true;
        }

        private void RestoreWindow()
        {
            WindowState = WindowState.Normal;

            // Восстанавливаем предыдущие размеры и позицию
            if (_windowSize.Width > 0 && _windowSize.Height > 0)
            {
                Left = _windowPosition.X;
                Top = _windowPosition.Y;
                Width = _windowSize.Width;
                Height = _windowSize.Height;
            }

            MaximizeButton.Content = "□";
            _isMaximized = false;
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    // Двойной клик по заголовку для максимизации/восстановления
                    ToggleMaximizeRestore();
                }
                else
                {
                    // Одинарный клик - начинаем перетаскивание только если клик не на кнопках
                    if (!IsMouseOverWindowButtons())
                    {
                        DragMove();
                    }
                }
            }
        }

        private bool IsMouseOverWindowButtons()
        {
            var mousePos = Mouse.GetPosition(this);

            // Проверяем, находится ли мышь над кнопками управления окном
            var minimizeButtonRect = new Rect(
                ActualWidth - 135,  // X позиция кнопок
                0,                  // Y позиция
                135,                // Ширина области кнопок
                35                  // Высота заголовка
            );

            return minimizeButtonRect.Contains(mousePos);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.Content = "❐";
                _isMaximized = true;

                // Скрываем границы при максимизации для лучшего вида
                BorderThickness = new Thickness(0);
            }
            else
            {
                MaximizeButton.Content = "□";
                _isMaximized = false;

                // Восстанавливаем границы
                BorderThickness = new Thickness(1);
            }
        }

        #endregion

        #region Preview Methods

        private void UpdatePreview()
        {
            if (MainPreviewCanvas == null) return;

            MainPreviewCanvas.Children.Clear();
            DrawPreviewCrosshair();
        }

        private void DrawPreviewCrosshair()
        {
            var crosshair = _viewModel.CurrentCrosshair;
            var centerX = MainPreviewCanvas.ActualWidth / 2;
            var centerY = MainPreviewCanvas.ActualHeight / 2;

            if (centerX <= 0 || centerY <= 0) return;

            var scale = Math.Min(MainPreviewCanvas.ActualWidth, MainPreviewCanvas.ActualHeight) / 200;
            scale = Math.Max(0.5, Math.Min(2.0, scale));

            switch (crosshair.Type)
            {
                case CrosshairType.Cross:
                    DrawPreviewCross(centerX, centerY, crosshair, scale);
                    break;
                case CrosshairType.Circle:
                    DrawPreviewCircle(centerX, centerY, crosshair, scale);
                    break;
                case CrosshairType.Dot:
                    DrawPreviewDot(centerX, centerY, crosshair, scale);
                    break;
                case CrosshairType.Square:
                    DrawPreviewSquare(centerX, centerY, crosshair, scale);
                    break;
                case CrosshairType.Triangle:
                    DrawPreviewTriangle(centerX, centerY, crosshair, scale);
                    break;
                case CrosshairType.Custom:
                    DrawPreviewCustom(centerX, centerY, crosshair, scale);
                    break;
            }
        }

        private void DrawPreviewCross(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            var brush = new SolidColorBrush(crosshair.Color);
            brush.Opacity = crosshair.Opacity;

            var scaledSize = crosshair.Size * scale;

            if (crosshair.ShowOutline && crosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(crosshair.OutlineColor);
                outlineBrush.Opacity = crosshair.Opacity;

                var horizontalOutline = new Line
                {
                    X1 = centerX - scaledSize / 2,
                    Y1 = centerY,
                    X2 = centerX + scaledSize / 2,
                    Y2 = centerY,
                    Stroke = outlineBrush,
                    StrokeThickness = crosshair.Thickness + crosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true
                };

                var verticalOutline = new Line
                {
                    X1 = centerX,
                    Y1 = centerY - scaledSize / 2,
                    X2 = centerX,
                    Y2 = centerY + scaledSize / 2,
                    Stroke = outlineBrush,
                    StrokeThickness = crosshair.Thickness + crosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true
                };

                MainPreviewCanvas.Children.Add(horizontalOutline);
                MainPreviewCanvas.Children.Add(verticalOutline);
            }

            var horizontalLine = new Line
            {
                X1 = centerX - scaledSize / 2,
                Y1 = centerY,
                X2 = centerX + scaledSize / 2,
                Y2 = centerY,
                Stroke = brush,
                StrokeThickness = crosshair.Thickness,
                SnapsToDevicePixels = true
            };

            var verticalLine = new Line
            {
                X1 = centerX,
                Y1 = centerY - scaledSize / 2,
                X2 = centerX,
                Y2 = centerY + scaledSize / 2,
                Stroke = brush,
                StrokeThickness = crosshair.Thickness,
                SnapsToDevicePixels = true
            };

            MainPreviewCanvas.Children.Add(horizontalLine);
            MainPreviewCanvas.Children.Add(verticalLine);
        }

        private void DrawPreviewCircle(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            var brush = new SolidColorBrush(crosshair.Color);
            brush.Opacity = crosshair.Opacity;

            var scaledSize = crosshair.Size * scale;

            if (crosshair.ShowOutline && crosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(crosshair.OutlineColor);
                outlineBrush.Opacity = crosshair.Opacity;

                var outlineEllipse = new Ellipse
                {
                    Width = scaledSize + crosshair.OutlineThickness * 2,
                    Height = scaledSize + crosshair.OutlineThickness * 2,
                    Stroke = outlineBrush,
                    StrokeThickness = crosshair.Thickness + crosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true
                };

                Canvas.SetLeft(outlineEllipse, centerX - (scaledSize + crosshair.OutlineThickness * 2) / 2);
                Canvas.SetTop(outlineEllipse, centerY - (scaledSize + crosshair.OutlineThickness * 2) / 2);
                MainPreviewCanvas.Children.Add(outlineEllipse);
            }

            var ellipse = new Ellipse
            {
                Width = scaledSize,
                Height = scaledSize,
                Stroke = brush,
                StrokeThickness = crosshair.Thickness,
                SnapsToDevicePixels = true
            };

            Canvas.SetLeft(ellipse, centerX - scaledSize / 2);
            Canvas.SetTop(ellipse, centerY - scaledSize / 2);
            MainPreviewCanvas.Children.Add(ellipse);
        }

        private void DrawPreviewDot(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            var brush = new SolidColorBrush(crosshair.Color);
            brush.Opacity = crosshair.Opacity;

            var dotSize = Math.Max(2, crosshair.Size / 4) * scale;

            if (crosshair.ShowOutline && crosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(crosshair.OutlineColor);
                outlineBrush.Opacity = crosshair.Opacity;

                var outlineEllipse = new Ellipse
                {
                    Width = dotSize + crosshair.OutlineThickness * 2,
                    Height = dotSize + crosshair.OutlineThickness * 2,
                    Stroke = outlineBrush,
                    StrokeThickness = crosshair.OutlineThickness,
                    SnapsToDevicePixels = true
                };

                Canvas.SetLeft(outlineEllipse, centerX - (dotSize + crosshair.OutlineThickness * 2) / 2);
                Canvas.SetTop(outlineEllipse, centerY - (dotSize + crosshair.OutlineThickness * 2) / 2);
                MainPreviewCanvas.Children.Add(outlineEllipse);
            }

            var ellipse = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = brush,
                SnapsToDevicePixels = true
            };

            Canvas.SetLeft(ellipse, centerX - dotSize / 2);
            Canvas.SetTop(ellipse, centerY - dotSize / 2);
            MainPreviewCanvas.Children.Add(ellipse);
        }

        private void DrawPreviewSquare(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            var brush = new SolidColorBrush(crosshair.Color);
            brush.Opacity = crosshair.Opacity;

            var scaledSize = crosshair.Size * scale;

            if (crosshair.ShowOutline && crosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(crosshair.OutlineColor);
                outlineBrush.Opacity = crosshair.Opacity;

                var outlineRectangle = new System.Windows.Shapes.Rectangle
                {
                    Width = scaledSize + crosshair.OutlineThickness * 2,
                    Height = scaledSize + crosshair.OutlineThickness * 2,
                    Stroke = outlineBrush,
                    StrokeThickness = crosshair.Thickness + crosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true
                };

                Canvas.SetLeft(outlineRectangle, centerX - (scaledSize + crosshair.OutlineThickness * 2) / 2);
                Canvas.SetTop(outlineRectangle, centerY - (scaledSize + crosshair.OutlineThickness * 2) / 2);
                MainPreviewCanvas.Children.Add(outlineRectangle);
            }

            var rectangle = new System.Windows.Shapes.Rectangle
            {
                Width = scaledSize,
                Height = scaledSize,
                Stroke = brush,
                StrokeThickness = crosshair.Thickness,
                SnapsToDevicePixels = true
            };

            Canvas.SetLeft(rectangle, centerX - scaledSize / 2);
            Canvas.SetTop(rectangle, centerY - scaledSize / 2);
            MainPreviewCanvas.Children.Add(rectangle);
        }

        private void DrawPreviewTriangle(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            var brush = new SolidColorBrush(crosshair.Color);
            brush.Opacity = crosshair.Opacity;

            var scaledSize = crosshair.Size * scale;

            if (crosshair.ShowOutline && crosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(crosshair.OutlineColor);
                outlineBrush.Opacity = crosshair.Opacity;

                var outlinePolygon = new Polygon
                {
                    Stroke = outlineBrush,
                    StrokeThickness = crosshair.Thickness + crosshair.OutlineThickness * 2,
                    SnapsToDevicePixels = true,
                    Points = new PointCollection
                    {
                        new System.Windows.Point(centerX, centerY - scaledSize / 2),
                        new System.Windows.Point(centerX - scaledSize / 2, centerY + scaledSize / 2),
                        new System.Windows.Point(centerX + scaledSize / 2, centerY + scaledSize / 2)
                    }
                };

                MainPreviewCanvas.Children.Add(outlinePolygon);
            }

            var polygon = new Polygon
            {
                Stroke = brush,
                StrokeThickness = crosshair.Thickness,
                SnapsToDevicePixels = true,
                Points = new PointCollection
                {
                    new System.Windows.Point(centerX, centerY - scaledSize / 2),
                    new System.Windows.Point(centerX - scaledSize / 2, centerY + scaledSize / 2),
                    new System.Windows.Point(centerX + scaledSize / 2, centerY + scaledSize / 2)
                }
            };

            MainPreviewCanvas.Children.Add(polygon);
        }

        private void DrawPreviewCustom(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            if (crosshair.ShowCenterDot)
            {
                DrawPreviewCenterDot(centerX, centerY, crosshair, scale);
            }

            if (crosshair.ShowCrosshairLines)
            {
                DrawPreviewCrosshairLines(centerX, centerY, crosshair, scale);
            }

            if (crosshair.ShowOutline && crosshair.OutlineThickness > 0)
            {
                DrawPreviewCrosshairOutline(centerX, centerY, crosshair, scale);
            }
        }

        private void DrawPreviewCenterDot(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            var dotBrush = new SolidColorBrush(crosshair.DotColor);
            dotBrush.Opacity = crosshair.Opacity;

            var dotSize = Math.Max(1, crosshair.DotSize) * scale;

            if (crosshair.ShowOutline && crosshair.OutlineThickness > 0)
            {
                var outlineBrush = new SolidColorBrush(crosshair.OutlineColor);
                outlineBrush.Opacity = crosshair.Opacity;

                var outlineEllipse = new Ellipse
                {
                    Width = dotSize + crosshair.OutlineThickness * 2,
                    Height = dotSize + crosshair.OutlineThickness * 2,
                    Stroke = outlineBrush,
                    StrokeThickness = crosshair.OutlineThickness,
                    SnapsToDevicePixels = true
                };

                Canvas.SetLeft(outlineEllipse, centerX - (dotSize + crosshair.OutlineThickness * 2) / 2);
                Canvas.SetTop(outlineEllipse, centerY - (dotSize + crosshair.OutlineThickness * 2) / 2);
                MainPreviewCanvas.Children.Add(outlineEllipse);
            }

            var ellipse = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = dotBrush,
                SnapsToDevicePixels = true
            };

            Canvas.SetLeft(ellipse, centerX - dotSize / 2);
            Canvas.SetTop(ellipse, centerY - dotSize / 2);
            MainPreviewCanvas.Children.Add(ellipse);
        }

        private void DrawPreviewCrosshairLines(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            var lineBrush = new SolidColorBrush(crosshair.LineColor);
            lineBrush.Opacity = crosshair.Opacity;

            var scaledLineLength = crosshair.LineLength * scale;
            var scaledGap = crosshair.Gap * scale;
            var scaledThickness = crosshair.LineThickness * scale;

            if (crosshair.ShowTopLine)
            {
                var topLine = new Line
                {
                    X1 = centerX,
                    Y1 = centerY - scaledGap - scaledLineLength,
                    X2 = centerX,
                    Y2 = centerY - scaledGap,
                    Stroke = lineBrush,
                    StrokeThickness = scaledThickness,
                    SnapsToDevicePixels = true
                };
                MainPreviewCanvas.Children.Add(topLine);
            }

            if (crosshair.ShowBottomLine)
            {
                var bottomLine = new Line
                {
                    X1 = centerX,
                    Y1 = centerY + scaledGap,
                    X2 = centerX,
                    Y2 = centerY + scaledGap + scaledLineLength,
                    Stroke = lineBrush,
                    StrokeThickness = scaledThickness,
                    SnapsToDevicePixels = true
                };
                MainPreviewCanvas.Children.Add(bottomLine);
            }

            if (crosshair.ShowLeftLine)
            {
                var leftLine = new Line
                {
                    X1 = centerX - scaledGap - scaledLineLength,
                    Y1 = centerY,
                    X2 = centerX - scaledGap,
                    Y2 = centerY,
                    Stroke = lineBrush,
                    StrokeThickness = scaledThickness,
                    SnapsToDevicePixels = true
                };
                MainPreviewCanvas.Children.Add(leftLine);
            }

            if (crosshair.ShowRightLine)
            {
                var rightLine = new Line
                {
                    X1 = centerX + scaledGap,
                    Y1 = centerY,
                    X2 = centerX + scaledGap + scaledLineLength,
                    Y2 = centerY,
                    Stroke = lineBrush,
                    StrokeThickness = scaledThickness,
                    SnapsToDevicePixels = true
                };
                MainPreviewCanvas.Children.Add(rightLine);
            }
        }

        private void DrawPreviewCrosshairOutline(double centerX, double centerY, Crosshair crosshair, double scale)
        {
            var outlineBrush = new SolidColorBrush(crosshair.OutlineColor);
            outlineBrush.Opacity = crosshair.Opacity;

            var scaledLineLength = crosshair.LineLength * scale;
            var scaledGap = crosshair.Gap * scale;
            var scaledThickness = crosshair.LineThickness * scale;
            var scaledOutlineThickness = crosshair.OutlineThickness * scale;

            if (crosshair.ShowTopLine)
            {
                DrawPreviewOutlinedLine(centerX, centerY - scaledGap - scaledLineLength,
                                      centerX, centerY - scaledGap, outlineBrush, scaledThickness, scaledOutlineThickness);
            }

            if (crosshair.ShowBottomLine)
            {
                DrawPreviewOutlinedLine(centerX, centerY + scaledGap,
                                      centerX, centerY + scaledGap + scaledLineLength, outlineBrush, scaledThickness, scaledOutlineThickness);
            }

            if (crosshair.ShowLeftLine)
            {
                DrawPreviewOutlinedLine(centerX - scaledGap - scaledLineLength, centerY,
                                      centerX - scaledGap, centerY, outlineBrush, scaledThickness, scaledOutlineThickness);
            }

            if (crosshair.ShowRightLine)
            {
                DrawPreviewOutlinedLine(centerX + scaledGap, centerY,
                                      centerX + scaledGap + scaledLineLength, centerY, outlineBrush, scaledThickness, scaledOutlineThickness);
            }
        }

        private void DrawPreviewOutlinedLine(double x1, double y1, double x2, double y2, SolidColorBrush outlineBrush, double lineThickness, double outlineThickness)
        {
            var outlineLine = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = outlineBrush,
                StrokeThickness = lineThickness + outlineThickness * 2,
                SnapsToDevicePixels = true
            };
            MainPreviewCanvas.Children.Insert(0, outlineLine);
        }

        #endregion
    }
}