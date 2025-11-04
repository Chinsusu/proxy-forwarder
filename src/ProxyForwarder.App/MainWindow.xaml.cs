// <copyright file="MainWindow.xaml.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Reflection;
using System.Windows;

namespace ProxyForwarder.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DisplayVersion();
    }

    private void DisplayVersion()
    {
        var version = Assembly.GetExecutingAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        
        if (!string.IsNullOrWhiteSpace(version) && !version.EndsWith("unknown"))
        {
            this.Title = $"{this.Title}  v{version}";
        }
    }
}
