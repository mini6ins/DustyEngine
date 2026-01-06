using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace DustyProjectHub;

public class MainWindow : Window
{
    private readonly ObservableCollection<ProjectInfo> _projects = [];

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

        var topPanel = CreateTopPanel();
        Grid.SetRow(topPanel, 0);
        root.Children.Add(topPanel);

        var scroll = CreateProjectListView();
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        Content = root;
    }

    private StackPanel CreateTopPanel()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(8),
        };

        var createButton = new Button { Content = "Create" };
        var addButton = new Button { Content = "Add project" };
        var settingButton = new Button { Content = "Settings" };

        createButton.Click += (_, _) => CreateProject();
        addButton.Click += (_, _) => AddProject();
        settingButton.Click += (_, _) => SettingsMenu.Show(this);

        panel.Children.Add(createButton);
        panel.Children.Add(addButton);
        panel.Children.Add(settingButton);

        return panel;
    }

    private ScrollViewer CreateProjectListView()
    {
        var items = new ItemsControl
        {
            ItemsSource = _projects,
            ItemsPanel = new FuncTemplate<Panel>(() => new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            })!,
            ItemTemplate = new FuncDataTemplate<ProjectInfo>((p, _) =>
                ProjectUIFactory.CreateProjectButton(p, OnProjectClicked))
        };

        return new ScrollViewer
        {
            Content = items,
            Margin = new Thickness(8),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private async Task AddProject()
    {
        var projectPath = await AddProjectDialog.Show(this);

        if (string.IsNullOrWhiteSpace(projectPath))
            return;

        if (!ProjectService.ValidateProjectPath(projectPath, out var errorMessage))
        {
            await MessageDialog.Show(this, "Error", errorMessage!);
            return;
        }

        await AddProjectToList(projectPath);
    }

    private async Task CreateProject()
    {
        var projectPath = await CreateNewProject.Show(this);

        if (string.IsNullOrWhiteSpace(projectPath))
            return;

        await AddProjectToList(projectPath);
    }

    private async Task AddProjectToList(string projectPath)
    {
        try
        {
            var projectInfo = ProjectService.GetProjectInfo(projectPath);

            if (_projects.Any(p => p.Path == projectPath))
            {
                await MessageDialog.Show(this, "Info", "This project is already in the list.");
                return;
            }

            _projects.Add(projectInfo);
        }
        catch (Exception ex)
        {
            await MessageDialog.Show(this, "Error", $"Failed to load project:\n{ex.Message}");
        }
    }

    private async void OnProjectClicked(ProjectInfo projectInfo)
    {
        var settings = HubSettingsLoader.Load();
        var enginePath = settings.EnginePath;

        if (!ProjectLauncher.ValidateEnginePath(enginePath, out var errorMessage))
        {
            await MessageDialog.Show(this, "Error", errorMessage!);
            return;
        }

        var engineVersion = ProjectService.LoadEngineVersionFromFile(enginePath);
        
        if (!ProjectLauncher.IsVersionCompatible(engineVersion, projectInfo.EngineVersion))
        {
            var ok = await ConfirmDialog.Show(
                this,
                "Confirm",
                $"Project '{projectInfo.Name}' was created with another engine version.\nContinue anyway?"
            );

            if (!ok) return;
        }

        ProjectLauncher.LaunchProject(projectInfo, enginePath);
    }
}