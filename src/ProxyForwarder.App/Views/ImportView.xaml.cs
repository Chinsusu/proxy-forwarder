using System.Windows;
using System.Windows.Controls;
using ProxyForwarder.App.ViewModels;

namespace ProxyForwarder.App.Views;

public partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ImportViewModel vm && sender is PasswordBox pb)
        {
            vm.Token = pb.Password;
        }
    }
}
