using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FileOrganizer.Database;
using FileOrganizer.DataExtractor;
using FileOrganizer.Services;
using FileOrganizer.UIService;
using FileOrganizer.ViewModels;
using FileOrganizer.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace FileOrganizer;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            BindingPlugins.DataValidators.RemoveAt(0);

            var services = new ServiceCollection();


            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<SettingsWindow>();
            services.AddSingleton<SettingsWindowViewModel>();
            services.AddSingleton<IAIConfigurationService, AIConfigurationService>();
            services.AddSingleton<ITextExtractorService, TextExtractorService>();
            services.AddSingleton<ITextExtractionService, TextExtractionService>();
            services.AddDbContext<AppDbContext>(options =>
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
                options.UseSqlite($"Data Source={dbPath}");
            });
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<IFolderPickerService, FolderPickerService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<INotificationService>(provider =>
            {
                var window = provider.GetRequiredService<MainWindow>();
                var notificationManager = new WindowNotificationManager(window)
                {
                    MaxItems = 3,
                    Position = NotificationPosition.TopCenter
                };
                return new NotificationService(notificationManager);
            });


            services.AddTextExtraction();


            var serviceProvider = services.BuildServiceProvider();
            var dialogService = serviceProvider.GetRequiredService<IDialogService>();
            dialogService.RegisterDialog<SettingsWindowViewModel, SettingsWindow>();

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            }

            var factory = serviceProvider.GetService<TextExtractorFactory>();
            var documentExtractor = serviceProvider.GetService<IDocumentTextExtractor>();

            if (factory == null)
                throw new InvalidOperationException("TextExtractorFactory is not registered properly");
            if (documentExtractor == null)
                throw new InvalidOperationException("IDocumentTextExtractor is not registered properly");


            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = viewModel;
            desktop.MainWindow = mainWindow;


            mainWindow.Activated += (s, e) => viewModel.UpdateWindowState(true);
            mainWindow.Deactivated += (s, e) => viewModel.UpdateWindowState(false);

            var settingWindow = serviceProvider.GetRequiredService<SettingsWindow>();
            var settingWindowViewModel = serviceProvider.GetRequiredService<SettingsWindowViewModel>();

            settingWindow.Activated += (s, e) => settingWindowViewModel.UpdateWindowState(true);
            settingWindow.Deactivated += (s, e) => settingWindowViewModel.UpdateWindowState(false);
        }

        base.OnFrameworkInitializationCompleted();
    }
}