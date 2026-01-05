using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace DustyProjectHub;

public sealed record ProjectInfo(
    string Name,
    double EngineVersion,
    string Path,
    DateTime LastOpened
);

public class MainWindow : Window
{
    private readonly ObservableCollection<ProjectInfo> _projects = [];

    private static ProjectInfo GetProjectInfo(string projectPath)
    {
        var json = File.ReadAllText(Path.Combine(projectPath, "Settings/project_settings.json"));

        using var doc = JsonDocument.Parse(json);

        var engineVersion = doc.RootElement.GetProperty("DustyEngineVersion").GetDouble();
        var name = doc.RootElement.GetProperty("ProjectName").GetString();

        Console.WriteLine(name + engineVersion);
        return new ProjectInfo(name, engineVersion, projectPath, DateTime.Now);
    }

    private static double LoadEngineVersionFromFile(string enginePath)
    {
        if (string.IsNullOrWhiteSpace(enginePath))
            return 0.0;

        var dir = Path.GetDirectoryName(enginePath);
        if (string.IsNullOrWhiteSpace(dir))
            return 0.0;

        var path = Path.Combine(dir, "engine_version.txt");

        if (!File.Exists(path)) return 0.0;

        var text = File.ReadAllText(path).Trim();
        return double.Parse(text, CultureInfo.InvariantCulture);
    }


    public MainWindow()
    {
        Title = "DustyProjectHub";
        Width = 1024;
        Height = 600;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        BuildContent();
    }


    private void BuildContent()
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        var topPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(8),
        };


        var createButton = new Button
        {
            Content = "Create",
        };

        var addButton = new Button
        {
            Content = "Add project",
        };

        var settingButton = new Button
        {
            Content = "Settings",
        };

        createButton.Click += (_, _) => CreateProject();
        addButton.Click += (_, _) => AddProject();
        settingButton.Click += (_, _) => OpenSetting();
        topPanel.Children.Add(createButton);
        topPanel.Children.Add(addButton);
        topPanel.Children.Add(settingButton);
        Grid.SetRow(topPanel, 0);
        root.Children.Add(topPanel);

        var items = new ItemsControl
        {
            ItemsSource = _projects,

            ItemsPanel = new FuncTemplate<Panel>(() => new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            })!,
            ItemTemplate = new FuncDataTemplate<ProjectInfo>((p, _) =>
            {
                var btn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(0),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    Content = new Border
                    {
                        Padding = new Thickness(12),
                        CornerRadius = new CornerRadius(8),
                        Background = Brushes.DimGray,
                        Child = new StackPanel
                        {
                            Spacing = 2,
                            Children =
                            {
                                new Grid
                                {
                                    ColumnDefinitions =
                                    {
                                        new ColumnDefinition(GridLength.Star),
                                        new ColumnDefinition(GridLength.Auto)
                                    },
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = p.Name,
                                            FontSize = 16
                                        },
                                        new TextBlock
                                        {
                                            Text = $"Engine version: {p.EngineVersion}",
                                            FontSize = 14,
                                            Opacity = 0.8,
                                            HorizontalAlignment = HorizontalAlignment.Right
                                        }
                                    }
                                },

                                new TextBlock
                                {
                                    Text = p.Path,
                                    Opacity = 0.7,
                                    FontSize = 12
                                },
                                new TextBlock
                                {
                                    Text = p.LastOpened == default
                                        ? "Never opened"
                                        : $"Last: {p.LastOpened:g}",
                                    Opacity = 0.7,
                                    FontSize = 12
                                }
                            }
                        }
                    }
                };

                btn.Click += (_, _) => OnProjectClicked(p);

                return btn;
            })
        };

        var scroll = new ScrollViewer
        {
            Content = items,
            Margin = new Thickness(8),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        Content = root;
    }

    private async Task AddProject()
    {
        var projectPath = await AddProjectDialog.Show(this);

        if (string.IsNullOrWhiteSpace(projectPath))
            return;

        var settingsFile = Path.Combine(projectPath, "Settings/project_settings.json");

        if (!File.Exists(settingsFile))
        {
            await MessageDialog.Show(
                this,
                "Error",
                $"Project settings file not found at:\n{settingsFile}\n\nPlease select a valid project folder."
            );
            return;
        }

        try
        {
            var projectInfo = GetProjectInfo(projectPath);

            if (_projects.Any(p => p.Path == projectPath))
            {
                await MessageDialog.Show(this, "Info", "This project is already in the list."
                );
                return;
            }

            _projects.Add(projectInfo);
        }
        catch (Exception ex)
        {
            await MessageDialog.Show(
                this,
                "Error",
                $"Failed to load project:\n{ex.Message}"
            );
        }
    }

    private void CreateProject()
    {
        
    }

    private async Task OpenSetting()
    {
        await SettingsMenu.Show(this);
    }

    private async Task OnProjectClicked(ProjectInfo projectInfo)
    {
        var settings = HubSettingsLoader.Load();
        var enginePath = settings.EnginePath;

        if (string.IsNullOrWhiteSpace(enginePath))
        {
            await MessageDialog.Show(
                this,
                "Error",
                "Engine path is not configured. Please set it in Settings."
            );
            return;
        }

        if (!File.Exists(enginePath))
        {
            await MessageDialog.Show(
                this,
                "Error",
                $"Engine not found at:\n{enginePath}\n\nPlease update the path in Settings."
            );
            return;
        }

        if (System.Math.Abs(LoadEngineVersionFromFile(enginePath) - projectInfo.EngineVersion) > 0.0001)
        {
            var ok = await ConfirmDialog.Show(
                this,
                "Confirm",
                $"Project '{projectInfo.Name}' was created with another engine version.\nContinue anyway?"
            );

            if (!ok) return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = enginePath,
            Arguments = $"\"{projectInfo.Path}\"",
            UseShellExecute = false
        });
    }
}