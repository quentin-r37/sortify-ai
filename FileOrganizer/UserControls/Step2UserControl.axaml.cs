using Avalonia.Controls;
using Avalonia.Input;
using LiveChartsCore.SkiaSharpView.Avalonia;

namespace FileOrganizer.UserControls;

public partial class Step2UserControl : UserControl
{
    public Step2UserControl()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
    {
        if (sender is CartesianChart chart)
        {
            e.Handled = true;
        }
    }
}