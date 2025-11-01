using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Simple_Customized_Crosshair_SCC.Models
{
    /// <summary>
    /// Настройки отображения
    /// </summary>
    public class DisplaySettings : INotifyPropertyChanged
    {
        private int _selectedMonitorIndex;
        private bool _alwaysOnTop = true;
        private bool _clickThrough = true;

        public int SelectedMonitorIndex
        {
            get => _selectedMonitorIndex;
            set { _selectedMonitorIndex = value; OnPropertyChanged(); }
        }

        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set { _alwaysOnTop = value; OnPropertyChanged(); }
        }

        public bool ClickThrough
        {
            get => _clickThrough;
            set
            {
                _clickThrough = value;
                OnPropertyChanged();
                // При изменении этого свойства может потребоваться пересоздание окна
                OnPropertyChanged(nameof(RequiresWindowRecreation));
            }
        }

        /// <summary>
        /// Определяет, требуется ли пересоздание окна при изменении настроек
        /// </summary>
        public bool RequiresWindowRecreation => ClickThrough;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}