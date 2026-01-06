using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace DustyProjectHub.UI.Windows;

public class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }
    private readonly ProjectService _projectService;

    public MainWindow()
    {
        Instance = this;
        HubSettingsLoader.Load();
        _projectService = new ProjectService();
        
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

        createButton.Click += (_, _) => _ = _projectService.CreateProject();
        addButton.Click += (_, _) => _ = _projectService.AddProject();
        settingButton.Click += (_, _) => _ = SettingsMenu.Show();

        panel.Children.Add(createButton);
        panel.Children.Add(addButton);
        panel.Children.Add(settingButton);

        return panel;
    }

    private ScrollViewer CreateProjectListView()
    {
        var items = new ItemsControl
        {
            ItemsSource = _projectService.Projects,
            ItemsPanel = new FuncTemplate<Panel>(() => new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            })!,
            ItemTemplate = new FuncDataTemplate<ProjectInfo>((p, _) => ProjectUIFactory.CreateProjectButton(p, ProjectService.OnProjectClicked))
        };

        return new ScrollViewer
        {
            Content = items,
            Margin = new Thickness(8),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }
    
    public static Task ShowErrorDialog(string title, string message) => MessageDialog.Show(title, message);
    public static Task ShowInfoDialog(string title, string message) => MessageDialog.Show(title, message);
}