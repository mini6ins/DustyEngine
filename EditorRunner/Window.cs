using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GraphicsEngine;

public class ProjectSelectorApp : Application
{
    public static ProjectSelectorApp? Instance { get; private set; }
    public (string path, RenderMode mode)? Result { get; set; }

    public override void Initialize()
    {
        Instance = this;

        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ProjectSelectorWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

public class ProjectSelectorWindow : Window
{
    private TextBox _pathTextBox = null!;

    public ProjectSelectorWindow()
    {
        Title = "DustyEngine - Select Project";
        Width = 500;
        Height = 150;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        BuildContent();
    }

    private void BuildContent()
    {
        var mainPanel = new StackPanel
        {
            Margin = new Thickness(15)
        };

        var pathLabel = new TextBlock
        {
            Text = "Project Path:",
            Margin = new Thickness(0, 0, 0, 5)
        };

        var pathPanel = new DockPanel
        {
            Margin = new Thickness(0, 0, 0, 20)
        };

        var browseButton = new Button
        {
            Content = "Browse...",
            Width = 80,
            Margin = new Thickness(10, 0, 0, 0)
        };
        browseButton.Click += async (s, e) => await BrowseButton_Click();
        DockPanel.SetDock(browseButton, Dock.Right);

        _pathTextBox = new TextBox
        {
            Watermark = "Enter project path..."
        };

        pathPanel.Children.Add(browseButton);
        pathPanel.Children.Add(_pathTextBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 80,
            IsDefault = true
        };
        okButton.Click += OkButton_Click;

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80
        };
        cancelButton.Click += CancelButton_Click;

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        mainPanel.Children.Add(pathLabel);
        mainPanel.Children.Add(pathPanel);
        mainPanel.Children.Add(buttonPanel);

        Content = mainPanel;
    }

    private async Task BrowseButton_Click()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        var result = await dialog.ShowAsync(this);
        if (!string.IsNullOrEmpty(result))
            _pathTextBox.Text = result;
    }

    private void OkButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_pathTextBox.Text))
        {
            ShowErrorMessage("Please select a project path.");
            return;
        }

        if (!Directory.Exists(_pathTextBox.Text))
        {
            ShowErrorMessage("Selected path does not exist.");
            return;
        }

        if (ProjectSelectorApp.Instance != null)
            ProjectSelectorApp.Instance.Result = (_pathTextBox.Text, RenderMode.EditorStop);

        Close();
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private async void ShowErrorMessage(string message)
    {
        var messageBox = new Window
        {
            Title = "Error",
            Width = 300,
            Height = 150,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(15),
            Spacing = 15
        };

        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        var okBtn = new Button
        {
            Content = "OK",
            Width = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        okBtn.Click += (s, e) => messageBox.Close();

        panel.Children.Add(okBtn);
        messageBox.Content = panel;

        await messageBox.ShowDialog(this);
    }
}
