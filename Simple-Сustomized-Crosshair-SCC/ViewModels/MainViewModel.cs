using Simple_Customized_Crosshair_SCC.Models;
using Simple_Customized_Crosshair_SCC.Services;
using Simple_Customized_Crosshair_SCC.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Simple_Customized_Crosshair_SCC.ViewModels
{
    /// <summary>
    /// Основная ViewModel приложения
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly CrosshairManager _crosshairManager;
        private readonly MonitorService _monitorService;
        private Crosshair _currentCrosshair;
        private CrosshairWindow? _crosshairWindow;

        #region Properties

        public ObservableCollection<Crosshair> Crosshairs { get; } = new();
        public System.Collections.Generic.List<ScreenInfo> Screens { get; } = new();
        public DisplaySettings DisplaySettings { get; } = new();

        public Crosshair CurrentCrosshair
        {
            get => _currentCrosshair;
            set
            {
                if (_currentCrosshair != null)
                {
                    _currentCrosshair.PropertyChanged -= CurrentCrosshair_PropertyChanged;
                }

                _currentCrosshair = value;

                if (_currentCrosshair != null)
                {
                    _currentCrosshair.PropertyChanged += CurrentCrosshair_PropertyChanged;
                }

                OnPropertyChanged();
                UpdateCrosshair();
            }
        }

        private void CurrentCrosshair_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateCrosshair();
            OnPropertyChanged(nameof(CurrentCrosshair));
        }

        private bool _isCrosshairVisible;
        public bool IsCrosshairVisible
        {
            get => _isCrosshairVisible;
            set { _isCrosshairVisible = value; OnPropertyChanged(); UpdateCrosshairVisibility(); }
        }

        #endregion

        #region Commands

        // Инициализация команд в конструкторе с помощью инициализаторов свойств
        public ICommand ShowCrosshairCommand { get; private set; } = null!;
        public ICommand HideCrosshairCommand { get; private set; } = null!;
        public ICommand SaveCrosshairCommand { get; private set; } = null!;
        public ICommand NewCrosshairCommand { get; private set; } = null!;
        public ICommand DeleteCrosshairCommand { get; private set; } = null!;
        public ICommand ExportCrosshairCommand { get; private set; } = null!;
        public ICommand ImportCrosshairCommand { get; private set; } = null!;
        public ICommand DuplicateCrosshairCommand { get; private set; } = null!;
        public ICommand ChangeColorCommand { get; private set; } = null!;
        public ICommand ChangeDotColorCommand { get; private set; } = null!;
        public ICommand ChangeLineColorCommand { get; private set; } = null!;
        public ICommand ChangeOutlineColorCommand { get; private set; } = null!;
        public ICommand RenameCrosshairCommand { get; private set; } = null!;

        #endregion

        public MainViewModel()
        {
            _crosshairManager = new CrosshairManager();
            _monitorService = new MonitorService();

            LoadDefaultCrosshairs();
            LoadSavedCrosshairs();
            LoadScreens();

            _currentCrosshair = Crosshairs.FirstOrDefault() ?? CreateDefaultCrosshair();
            _currentCrosshair.PropertyChanged += CurrentCrosshair_PropertyChanged;

            InitializeCommands();
            DisplaySettings.PropertyChanged += (s, e) => UpdateDisplaySettings();
        }

        private void InitializeCommands()
        {
            ShowCrosshairCommand = new RelayCommand(_ => ShowCrosshair());
            HideCrosshairCommand = new RelayCommand(_ => HideCrosshair());
            SaveCrosshairCommand = new RelayCommand(_ => SaveCrosshair());
            NewCrosshairCommand = new RelayCommand(_ => CreateNewCrosshair());
            DeleteCrosshairCommand = new RelayCommand(_ => DeleteCurrentCrosshair(), _ => CurrentCrosshair != null);
            ExportCrosshairCommand = new RelayCommand(_ => ExportCrosshair(), _ => CurrentCrosshair != null);
            ImportCrosshairCommand = new RelayCommand(_ => ImportCrosshair());
            DuplicateCrosshairCommand = new RelayCommand(_ => DuplicateCurrentCrosshair(), _ => CurrentCrosshair != null);
            ChangeColorCommand = new RelayCommand(_ => ChangeColor());
            ChangeDotColorCommand = new RelayCommand(_ => ChangeDotColor());
            ChangeLineColorCommand = new RelayCommand(_ => ChangeLineColor());
            ChangeOutlineColorCommand = new RelayCommand(_ => ChangeOutlineColor());
            RenameCrosshairCommand = new RelayCommand(_ => RenameCurrentCrosshair(), _ => CurrentCrosshair != null);
        }

        #region Private Methods

        private Crosshair CreateDefaultCrosshair()
        {
            return new Crosshair
            {
                Name = "Default Crosshair",
                Type = CrosshairType.Cross,
                Color = Colors.Red,
                Size = 20,
                Thickness = 2,
                Opacity = 1.0,

                // Центральная точка
                ShowCenterDot = true,
                DotColor = Colors.Red,
                DotSize = 4,

                // Линии перекрестия
                ShowCrosshairLines = true,
                LineColor = Colors.Red,
                LineLength = 8,
                LineThickness = 2,
                Gap = 2,

                // Все линии включены по умолчанию
                ShowTopLine = true,
                ShowBottomLine = true,
                ShowLeftLine = true,
                ShowRightLine = true,

                // Обводка
                ShowOutline = true,
                OutlineColor = Colors.Black,
                OutlineThickness = 1.0
            };
        }

        private void LoadDefaultCrosshairs()
        {
            var defaultCrosshairs = new[]
            {
                new Crosshair {
                    Name = "Red Cross",
                    Type = CrosshairType.Cross,
                    Color = Colors.Red,
                    ShowCenterDot = false,
                    ShowCrosshairLines = false,
                    ShowOutline = false
                },
                new Crosshair {
                    Name = "Green Dot",
                    Type = CrosshairType.Dot,
                    Color = Colors.Lime,
                    Size = 10,
                    ShowCenterDot = false,
                    ShowCrosshairLines = false,
                    ShowOutline = false
                },
                new Crosshair {
                    Name = "Blue Circle",
                    Type = CrosshairType.Circle,
                    Color = Colors.Blue,
                    ShowCenterDot = false,
                    ShowCrosshairLines = false,
                    ShowOutline = false
                },
                new Crosshair {
                    Name = "Yellow Square",
                    Type = CrosshairType.Square,
                    Color = Colors.Yellow,
                    ShowCenterDot = false,
                    ShowCrosshairLines = false,
                    ShowOutline = false
                },
                new Crosshair {
                    Name = "Cyan Triangle",
                    Type = CrosshairType.Triangle,
                    Color = Colors.Cyan,
                    ShowCenterDot = false,
                    ShowCrosshairLines = false,
                    ShowOutline = false
                },
                new Crosshair {
                    Name = "Advanced Custom",
                    Type = CrosshairType.Custom,
                    Color = Colors.White,
                    ShowCenterDot = true,
                    DotColor = Colors.Red,
                    DotSize = 4,
                    ShowCrosshairLines = true,
                    LineColor = Colors.White,
                    LineLength = 10,
                    LineThickness = 2,
                    Gap = 3,
                    ShowTopLine = true,
                    ShowBottomLine = true,
                    ShowLeftLine = true,
                    ShowRightLine = true,
                    ShowOutline = true,
                    OutlineColor = Colors.Black,
                    OutlineThickness = 1.0
                }
            };

            foreach (var crosshair in defaultCrosshairs)
            {
                Crosshairs.Add(crosshair);
            }
        }

        private void LoadSavedCrosshairs()
        {
            var savedCrosshairs = _crosshairManager.LoadCrosshairs();
            foreach (var crosshair in savedCrosshairs)
            {
                if (!Crosshairs.Any(c => c.Name == crosshair.Name))
                {
                    Crosshairs.Add(crosshair);
                }
            }
        }

        private void LoadScreens()
        {
            Screens.Clear();
            var screens = _monitorService.GetScreens();
            foreach (var screen in screens)
            {
                Screens.Add(screen);
            }
            OnPropertyChanged(nameof(Screens));
        }

        private void ShowCrosshair()
        {
            if (_crosshairWindow == null)
            {
                _crosshairWindow = new CrosshairWindow(CurrentCrosshair, DisplaySettings);
                _crosshairWindow.Closed += (s, e) =>
                {
                    _crosshairWindow = null;
                    IsCrosshairVisible = false;
                };
            }

            _crosshairWindow.Show();
            IsCrosshairVisible = true;
            UpdateCrosshair();
        }

        private void HideCrosshair()
        {
            _crosshairWindow?.Close();
            _crosshairWindow = null;
            IsCrosshairVisible = false;
        }

        private void UpdateCrosshairVisibility()
        {
            if (_crosshairWindow != null)
            {
                if (IsCrosshairVisible)
                {
                    _crosshairWindow.Show();
                }
                else
                {
                    _crosshairWindow.Hide();
                }
            }
        }

        private void UpdateCrosshair()
        {
            _crosshairWindow?.UpdateCrosshair(CurrentCrosshair);
        }

        private void UpdateDisplaySettings()
        {
            if (_crosshairWindow != null)
            {
                var wasVisible = IsCrosshairVisible;
                _crosshairWindow.Close();
                _crosshairWindow = null;

                if (wasVisible)
                {
                    System.Threading.Thread.Sleep(100);
                    ShowCrosshair();
                }
            }
        }

        private void SaveCrosshair()
        {
            _crosshairManager.SaveCrosshair(CurrentCrosshair);
            System.Windows.MessageBox.Show("Crosshair saved successfully!", "Success",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void CreateNewCrosshair()
        {
            var newCrosshair = new Crosshair
            {
                Name = $"New Crosshair {Crosshairs.Count + 1}",
                Type = CrosshairType.Custom, // Все новые прицелы создаются как Custom
                Color = Colors.Red,
                Size = 20,
                Thickness = 2,
                Opacity = 1.0,

                // Центральная точка
                ShowCenterDot = true,
                DotColor = Colors.Red,
                DotSize = 4,

                // Линии перекрестия
                ShowCrosshairLines = true,
                LineColor = Colors.Red,
                LineLength = 8,
                LineThickness = 2,
                Gap = 2,

                // Все линии включены по умолчанию
                ShowTopLine = true,
                ShowBottomLine = true,
                ShowLeftLine = true,
                ShowRightLine = true,

                // Обводка
                ShowOutline = true,
                OutlineColor = Colors.Black,
                OutlineThickness = 1.0
            };

            Crosshairs.Add(newCrosshair);
            CurrentCrosshair = newCrosshair;
        }

        private void DeleteCurrentCrosshair()
        {
            if (System.Windows.MessageBox.Show($"Are you sure you want to delete '{CurrentCrosshair.Name}'?",
                "Confirm Delete", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
            {
                _crosshairManager.DeleteCrosshair(CurrentCrosshair);
                Crosshairs.Remove(CurrentCrosshair);
                CurrentCrosshair = Crosshairs.FirstOrDefault() ?? CreateDefaultCrosshair();
            }
        }

        private void ExportCrosshair()
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Crosshair files (*.json)|*.json",
                FileName = $"{CurrentCrosshair.Name}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                _crosshairManager.ExportCrosshair(CurrentCrosshair, saveDialog.FileName);
                System.Windows.MessageBox.Show("Crosshair exported successfully!", "Success",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void ImportCrosshair()
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Crosshair files (*.json)|*.json"
            };

            if (openDialog.ShowDialog() == true)
            {
                var crosshair = _crosshairManager.ImportCrosshair(openDialog.FileName);
                if (crosshair != null)
                {
                    Crosshairs.Add(crosshair);
                    CurrentCrosshair = crosshair;
                    System.Windows.MessageBox.Show("Crosshair imported successfully!", "Success",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to import crosshair!", "Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void DuplicateCurrentCrosshair()
        {
            var duplicate = CurrentCrosshair.Clone();
            Crosshairs.Add(duplicate);
            CurrentCrosshair = duplicate;
        }

        private void RenameCurrentCrosshair()
        {
            var dialog = new RenameDialog(CurrentCrosshair.Name);
            if (dialog.ShowDialog() == true)
            {
                var newName = dialog.NewName?.Trim();
                if (!string.IsNullOrEmpty(newName) && !Crosshairs.Any(c => c.Name == newName && c != CurrentCrosshair))
                {
                    CurrentCrosshair.Name = newName;
                    OnPropertyChanged(nameof(Crosshairs));
                }
                else
                {
                    System.Windows.MessageBox.Show("Crosshair name already exists or is invalid!", "Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
        }

        private void ChangeColor()
        {
            ChangeColorProperty("Color", CurrentCrosshair.Color);
        }

        private void ChangeDotColor()
        {
            ChangeColorProperty("DotColor", CurrentCrosshair.DotColor);
        }

        private void ChangeLineColor()
        {
            ChangeColorProperty("LineColor", CurrentCrosshair.LineColor);
        }

        private void ChangeOutlineColor()
        {
            ChangeColorProperty("OutlineColor", CurrentCrosshair.OutlineColor);
        }

        private void ChangeColorProperty(string propertyName, System.Windows.Media.Color currentColor)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(
                    currentColor.A,
                    currentColor.R,
                    currentColor.G,
                    currentColor.B),
                FullOpen = true
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var winFormsColor = colorDialog.Color;
                var newColor = System.Windows.Media.Color.FromArgb(winFormsColor.A, winFormsColor.R, winFormsColor.G, winFormsColor.B);

                switch (propertyName)
                {
                    case "Color":
                        CurrentCrosshair.Color = newColor;
                        break;
                    case "DotColor":
                        CurrentCrosshair.DotColor = newColor;
                        break;
                    case "LineColor":
                        CurrentCrosshair.LineColor = newColor;
                        break;
                    case "OutlineColor":
                        CurrentCrosshair.OutlineColor = newColor;
                        break;
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}