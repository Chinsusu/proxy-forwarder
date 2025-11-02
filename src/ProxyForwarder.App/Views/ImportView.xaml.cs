// <copyright file="ImportView.xaml.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.App.ViewModels;

namespace ProxyForwarder.App.Views;

public partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();
        DataContext = App.HostInstance!.Services.GetRequiredService<ImportViewModel>();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ImportViewModel vm && sender is PasswordBox pb)
        {
            vm.Token = pb.Password;
        }
    }
}
