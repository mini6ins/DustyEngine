using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace DustyProjectHub;

public static class ProjectUIFactory
{
    public static Button CreateProjectButton(ProjectInfo project, Action<ProjectInfo> onClick)
    {
        var btn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Content = CreateProjectContent(project)
        };

        btn.Click += (_, _) => onClick(project);

        return btn;
    }

    private static Border CreateProjectContent(ProjectInfo project)
    {
        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = Brushes.DimGray,
            Child = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    CreateHeaderGrid(project),
                    CreatePathTextBlock(project),
                    CreateLastOpenedTextBlock(project)
                }
            }
        };
    }

    private static Grid CreateHeaderGrid(ProjectInfo project)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var nameText = new TextBlock
        {
            Text = project.Name,
            FontSize = 16
        };

        var versionText = new TextBlock
        {
            Text = $"Engine version: {project.EngineVersion}",
            FontSize = 14,
            Opacity = 0.8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        Grid.SetColumn(versionText, 1);

        grid.Children.Add(nameText);
        grid.Children.Add(versionText);

        return grid;
    }

    private static TextBlock CreatePathTextBlock(ProjectInfo project)
    {
        return new TextBlock
        {
            Text = project.Path,
            Opacity = 0.7,
            FontSize = 12
        };
    }

    private static TextBlock CreateLastOpenedTextBlock(ProjectInfo project)
    {
        return new TextBlock
        {
            Text = project.LastOpened == default
                ? "Never opened"
                : $"Last: {project.LastOpened:g}",
            Opacity = 0.7,
            FontSize = 12
        };
    }
}