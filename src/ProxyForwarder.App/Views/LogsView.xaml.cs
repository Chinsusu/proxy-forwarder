// <copyright file="LogsView.xaml.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.App.ViewModels;

namespace ProxyForwarder.App.Views;

public partial class LogsView : UserControl
{
    public LogsView()
    {
        InitializeComponent();
        DataContext = App.HostInstance!.Services.GetRequiredService<LogsViewModel>();
    }
}
