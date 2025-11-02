using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace ProxyForwarder.App.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    [ObservableProperty] private string text = "";

    public IRelayCommand OpenFolderCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public LogsViewModel()
    {
        OpenFolderCommand = new RelayCommand(OpenFolder);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        _ = RefreshAsync();
    }

    private void OpenFolder()
    {
        var dir = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo{ FileName = dir, UseShellExecute = true });
    }

    private Task RefreshAsync()
    {
        var dir = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(dir);
        var last = Directory.GetFiles(dir, "*.log").OrderByDescending(f => f).FirstOrDefault();
        if (last is null) { Text = "(no logs yet)"; return Task.CompletedTask; }
        var lines = File.ReadAllLines(last);
        var take = string.Join(Environment.NewLine, lines.TakeLast(400));
        Text = take;
        return Task.CompletedTask;
    }
}
