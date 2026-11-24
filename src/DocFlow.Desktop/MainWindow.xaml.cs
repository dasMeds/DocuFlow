using System.Windows;
using DocFlow.Desktop.ViewModels;
using Microsoft.Win32;

namespace DocFlow.Desktop;

/// <summary>
/// Thin UI code-behind forwarding file picker and drag-and-drop events to MainViewModel.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
    }

    private async void SelectFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Multiselect = true };
        if (dialog.ShowDialog() == true)
        {
            await _vm.EnqueueFilesAsync(dialog.FileNames);
        }
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            await _vm.EnqueueFilesAsync(files);
        }
    }
}
