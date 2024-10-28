using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FileOrganizer.Database;
using FileOrganizer.DataExtractor;
using FileOrganizer.Services;
using FileOrganizer.ViewModels;
using FileOrganizer.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddTransient<SettingsWindow>();
            services.AddSingleton<SettingsWindowViewModel>();
            services.AddSingleton<IAIConfigurationService, AIConfigurationService>();
            services.AddSingleton<ITextExtractorService, TextExtractorService>();
            services.AddSingleton<ITextExtractionService, TextExtractionService>();
            services.AddDbContext<AppDbContext>(options =>
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
                options.UseSqlite($"Data Source={dbPath}");
            }, ServiceLifetime.Scoped);
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddTextExtraction();


            var serviceProvider = services.BuildServiceProvider();
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
        }

        base.OnFrameworkInitializationCompleted();
    }
}