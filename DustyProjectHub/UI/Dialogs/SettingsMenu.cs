using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using DustyProjectHub.UI.Windows;

namespace DustyProjectHub;

public static class SettingsMenu
{
    public static async Task Show()
    {
        var engines = new ListBox
        {
            ItemsSource = HubSettingsLoader.HubSettings.EnginePaths,
            MinHeight = 120,
            ItemTemplate = new FuncDataTemplate<string>((path, _) =>
            {
                var version = HubSettingsLoader.LoadEngineVersionFromFile(path);

                return new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = path,
                            Width = 420,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },

                        new TextBlock
                        {
                            Text = $"v{version}",
                            FontWeight = FontWeight.Bold
                        }
                    }
                };
            })
        };


        var addBtn = new Button
        {
            Content = "Add engine",
            MinWidth = 120
        };

        var removeBtn = new Button
        {
            Content = "Remove",
            MinWidth = 120
        };

        addBtn.Click += async (_, _) =>
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Engine Executable",
                AllowMultiple = false
            };


            var files = await dialog.ShowAsync(MainWindow.Instance!);
            if (files == null || files.Length == 0) return;

            var path = files[0];

            if (!HubSettingsLoader.HubSettings.EnginePaths.Contains(path))
            {
                HubSettingsLoader.HubSettings.EnginePaths.Add(path);
                engines.ItemsSource = null;
                engines.ItemsSource = HubSettingsLoader.HubSettings.EnginePaths;
            }
        };

        removeBtn.Click += (_, _) =>
        {
            if (engines.SelectedItem is not string path) return;

            HubSettingsLoader.HubSettings.EnginePaths.Remove(path);
            engines.ItemsSource = null;
            engines.ItemsSource = HubSettingsLoader.HubSettings.EnginePaths;
        };

        var saveBtn = new Button
        {
            Content = "Save",
            MinWidth = 90,
            IsDefault = true
        };

        var cancelBtn = new Button
        {
            Content = "Cancel",
            MinWidth = 90,
            IsCancel = true
        };

        var window = new Window
        {
            Title = "Settings",
            Width = 640,
            Height = 360,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        saveBtn.Click += (_, _) =>
        {
            HubSettingsLoader.Save();
            window.Close();
        };

        cancelBtn.Click += (_, _) => window.Close();

        window.Content = new StackPanel
        {
            Margin = new(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Installed engines",
                    FontSize = 14
                },

                engines,

                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        addBtn,
                        removeBtn
                    }
                },

                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        cancelBtn,
                        saveBtn
                    }
                }
            }
        };

        await window.ShowDialog(MainWindow.Instance!);
    }
}