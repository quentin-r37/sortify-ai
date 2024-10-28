using System.Threading.Tasks;
using Avalonia.Controls;
using FileOrganizer.ViewModels;

namespace FileOrganizer.UIService;

public interface IDialogService
{
    Task ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase;
    void CloseDialog<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase;
    void RegisterDialog<TViewModel, TWindow>()
        where TViewModel : ViewModelBase
        where TWindow : Window;
}