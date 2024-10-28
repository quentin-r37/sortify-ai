using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using FileOrganizer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileOrganizer.UIService;

public class DialogService(IServiceProvider serviceProvider) : IDialogService
{
    private readonly Dictionary<Type, Type> _mappings = new();

    public void RegisterDialog<TViewModel, TWindow>()
        where TViewModel : ViewModelBase
        where TWindow : Window
    {
        _mappings[typeof(TViewModel)] = typeof(TWindow);
    }

    public async Task ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase
    {
        var windowType = _mappings[typeof(TViewModel)];
        var window = (Window)ActivatorUtilities.CreateInstance(serviceProvider, windowType);
        window.DataContext = viewModel;

        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var activeWindow = lifetime?.MainWindow;

        if (activeWindow != null)
        {
            await window.ShowDialog(activeWindow);
        }
        else
        {
            throw new InvalidOperationException("No active window found to show dialog");
        }
    }

    public void CloseDialog<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase
    {
        if (viewModel.GetType() is { } viewModelType && _mappings.ContainsKey(viewModelType))
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
                    return;
                foreach (var window in lifetime.Windows)
                {
                    if (window.DataContext != viewModel) continue;
                    window.Close();
                    break;
                }
            });
        }
    }
}