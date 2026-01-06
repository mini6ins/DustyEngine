using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using DustyProjectHub.UI.Windows;

namespace DustyProjectHub;

public sealed record CreateProjectResult(string Name, string Path);

public class CreateNewProject
{
    public static async Task<CreateProjectResult?> Show()
    {
        var projectNameBox = new TextBox
        {
            Watermark = "Project name",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var projectPathBox = new TextBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Watermark = "Select project folder..."
        };

        var browseBtn = new Button
        {
            Content = "Browse…",
            MinWidth = 90
        };

        browseBtn.Click += async (_, _) =>
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Project Folder"
            };

            var folder = await dialog.ShowAsync(MainWindow.Instance!);
            if (!string.IsNullOrWhiteSpace(folder))
                projectPathBox.Text = folder;
        };

        var okBtn = new Button
        {
            Content = "OK",
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
            Title = "Create Project",
            Width = 520,
            Height = 260,
            CanResize = false,
            SystemDecorations = SystemDecorations.Full,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        CreateProjectResult? result = null;

        okBtn.Click += (_, _) =>
        {
            var name = projectNameBox.Text?.Trim();
            var path = projectPathBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
                return;

            result = new CreateProjectResult(name, path);
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
                projectPathBox,
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
                new TextBlock { Text = "Project name", FontSize = 14 },
                projectNameBox,

                new TextBlock { Text = "Project folder path", FontSize = 14 },
                pathRow,

                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        cancelBtn,
                        okBtn
                    }
                }
            }
        };

        await window.ShowDialog(MainWindow.Instance!);
        return result;
    }
}
