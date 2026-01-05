using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace DustyProjectHub;

public static class SettingsMenu
{
    public static async Task Show(Window owner)
    {
        var settings = HubSettingsLoader.Load();

        var enginePathBox = new TextBox
        {
            Text = settings.EnginePath,
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var browseBtn = new Button
        {
            Content = "Browse…",
            MinWidth = 90
        };

        browseBtn.Click += async (_, _) =>
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Engine Executable (linux executable or exe)",
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = "All Files",
                        Extensions = new List<string> { "*" }
                    }
                }
            };

            var files = await dialog.ShowAsync(owner);
            if (files != null && files.Length > 0)
                enginePathBox.Text = files[0];
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
            Width = 520,
            Height = 220,
            CanResize = false,
            SystemDecorations = SystemDecorations.Full,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        saveBtn.Click += (_, _) =>
        {
            settings.EnginePath = enginePathBox.Text?.Trim() ?? "";
            HubSettingsLoader.Save(settings);
            window.Close();
        };

        cancelBtn.Click += (_, _) => window.Close();

        var pathRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8,
            Children =
            {
                enginePathBox,
                browseBtn
            }
        };
        Grid.SetColumn(browseBtn, 1);

        window.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Engine executable path",
                    FontSize = 14
                },

                pathRow,

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

        await window.ShowDialog(owner);
    }
}