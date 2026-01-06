using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace DustyProjectHub;

public class CreateNewProject
{
    public static async Task<string?> Show(Window owner)
    {
        var projectPathBox = new TextBox
        {
            MinWidth = 0,
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

            var folder = await dialog.ShowAsync(owner);
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
            Title = "Add Project",
            Width = 520,
            Height = 200,
            CanResize = false,
            SystemDecorations = SystemDecorations.Full,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        string? result = null;

        okBtn.Click += (_, _) =>
        {
            var path = projectPathBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(path))
            {
                result = path;
                window.Close();
            }
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
                new TextBlock
                {
                    Text = "Project folder path",
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
                        okBtn
                    }
                }
            }
        };

        await window.ShowDialog(owner);
        return result;
    }
}