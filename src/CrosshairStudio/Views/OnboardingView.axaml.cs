using Avalonia.Controls;
using Avalonia.Threading;
using CrosshairStudio.ViewModels;

namespace CrosshairStudio.Views;

public partial class OnboardingView : UserControl
{
    private readonly DispatcherTimer _timer;

    public OnboardingView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(32) };
        _timer.Tick += (_, _) =>
        {
            if (DataContext is OnboardingViewModel vm)
            {
                vm.Demo.Rotation = (vm.Demo.Rotation + 0.35) % 360;
                if (vm.PageIndex == 2)
                    vm.Demo.Gap = 3 + Math.Sin(vm.Demo.Rotation * Math.PI / 180) * 3;
            }
        };
        AttachedToVisualTree += (_, _) => _timer.Start();
        DetachedFromVisualTree += (_, _) => _timer.Stop();
    }
}
