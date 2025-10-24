using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using FlashCards.UI.ViewModels;
using FlashCards.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FlashCards.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace FlashCards.UI;

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
            DisableAvaloniaDataAnnotationValidation();

                var configurationBuilder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                var configuration = configurationBuilder.Build();

                var services = new ServiceCollection();

                services.AddSingleton<IConfiguration>(configuration);

                services.AddDbContextFactory<ApplicationDataContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("FlashCards")));

                services.AddTransient<MainWindowViewModel>(sp =>
                {
                    var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDataContext>>();
                    return new MainWindowViewModel(factory, configuration);
                });

                services.AddTransient<MainWindow>(sp =>
                {
                    var vm = sp.GetRequiredService<MainWindowViewModel>();
                    return new MainWindow(vm);
                });

                var serviceProvider = services.BuildServiceProvider();

                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}