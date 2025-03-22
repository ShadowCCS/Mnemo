using Avalonia.Controls;
using Avalonia.Input;
using MnemoProject.ViewModels;

namespace MnemoProject.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Start dragging the window
            this.BeginMoveDrag(e);
        }
    }


}