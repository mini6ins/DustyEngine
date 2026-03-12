using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using DustyProjectHub.Services;
using DustyProjectHub.UI.Windows;

namespace DustyProjectHub;

public enum ProjectTemplates
{
    Empty3D,
    Empty2D,
}

public static class CreateNewProject
{
    private static ProjectTemplates _selectedTemplate = ProjectTemplates.Empty3D;

    public static async Task<(ProjectInfo?, ProjectTemplates)> Show()
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

        var projectTemplate = new ComboBox
        {
            ItemsSource = Enum.GetValues<ProjectTemplates>(),
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var enginePaths = HubSettingsLoader.HubSettings.EnginePaths;

        var engineVersions = enginePaths
            .Select(HubSettingsLoader.LoadEngineVersionFromFile)
            .ToList();

        var engineVersion = new ComboBox
        {
            ItemsSource = engineVersions,
            SelectedIndex = engineVersions.Count > 0 ? 0 : -1,
            HorizontalAlignment = HorizontalAlignment.Stretch
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
            Width = 560,
            Height = 320,
            CanResize = false,
            SystemDecorations = SystemDecorations.Full,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        ProjectInfo? result = null;

        okBtn.Click += (_, _) =>
        {
            var name = projectNameBox.Text?.Trim();
            var path = projectPathBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
                return;

            if (engineVersion.SelectedItem is not double version)
                return;

            if (projectTemplate.SelectedItem is ProjectTemplates template)
                _selectedTemplate = template;

            result = new ProjectInfo(name, version, path, DateTime.Now);
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
            ColumnSpacing = 8
        };

        pathRow.Children.Add(projectPathBox);
        pathRow.Children.Add(browseBtn);
        Grid.SetColumn(browseBtn, 1);

        var templateRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Template:",
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 100
                },
                projectTemplate
            }
        };

        var engineRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Engine version:",
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 100
                },
                engineVersion
            }
        };

        var settingsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(16)),
                new ColumnDefinition(GridLength.Star)
            }
        };

        settingsGrid.Children.Add(templateRow);
        settingsGrid.Children.Add(engineRow);
        Grid.SetColumn(engineRow, 2);

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

                new TextBlock { Text = "Project settings", FontSize = 14 },

                settingsGrid,

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
        return (result, _selectedTemplate);
    }
}