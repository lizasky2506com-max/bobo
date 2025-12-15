using System.Windows;
using BANKOMAT.Wpf.ViewModels;

namespace BANKOMAT.Wpf;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void PinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.Pin = PinBox.Password;
    }

    private void OldPinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeOldPin = OldPinBox.Password;
    }

    private void NewPinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeNewPin = NewPinBox.Password;
    }
}
