using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FileOrganizer.ViewModels;
using FileOrganizer.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;
using System;
using FileOrganizer.Database;
using Microsoft.EntityFrameworkCore;

namespace FileOrganizer
{
    public partial class App : Application
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
                services.AddTransient<Step1ViewModel>();
                services.AddTransient<Step2ViewModel>();
                services.AddTransient<Step3ViewModel>();
                services.AddTransient<Step4ViewModel>();
                services.AddTransient<AnalysisViewModel>();

                services.AddDbContext<AppDbContext>(options =>
                {
                    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
                    options.UseSqlite($"Data Source={dbPath}");
                });

                var serviceProvider = services.BuildServiceProvider();

                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

                mainWindow.DataContext = viewModel;
                desktop.MainWindow = mainWindow;

            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}