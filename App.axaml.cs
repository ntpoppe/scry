using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Scry.ViewModels;
using Scry.Views;
using System;
using System.Linq;

namespace Scry
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
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                var window = new ScryWindow
                {
                    DataContext = new ScryWindowViewModel(),
                    ShowInTaskbar = false
                };

                desktop.MainWindow = window;

                // Ugly way to hide window on first open, EventHandler needs to reference itself
                // Could replace this with a placeholder "Scry is running!" thing
                EventHandler? onFirstOpen = null;
                onFirstOpen = (s, e) =>
                {
                    window.Hide();
                    window.Opened -= onFirstOpen!;
                };
                window.Opened += onFirstOpen;

                // Hide window on unfocus
                window.Deactivated += (s, e) =>
                {
                    window.Hide();
                };

                GlobalHotkey.Register(() => Toggle(window, desktop));

                base.OnFrameworkInitializationCompleted();
            }
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

        private void Toggle(ScryWindow w, IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (w.IsVisible)
            {
                w.Hide();
            }
            else
            {
                w.Show();
                w.Activate();
            }
        }
    }
}