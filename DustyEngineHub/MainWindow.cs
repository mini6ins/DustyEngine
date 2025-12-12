using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using DustyEngineEditor;

namespace DustyEngineHub;

public class MainWindow : Window
{
    private const string DefaultProjectPath =
        "/home/maksym/DustyEngine/TestProject";

    private const string DefaultRunnerPath =
        "/home/maksym/DustyEngine/Runner/bin/Debug/net9.0/Runner";

    private TextBox _projectPathBox;
    private TextBox _runnerPathBox;
    private CheckBox _useDefaultCheck;

    public MainWindow()
    {
        Title = "DustyEngine Hub";
        Width = 500;
        Height = 260;

        _useDefaultCheck = new CheckBox
        {
            Content = "Use standard path",
            IsChecked = true
        };

        _projectPathBox = new TextBox
        {
            Text = DefaultProjectPath,
            IsEnabled = false
        };

        _runnerPathBox = new TextBox
        {
            Text = DefaultRunnerPath,
            IsEnabled = false
        };

        _useDefaultCheck.Checked += (_, _) => SetDefault(true);
        _useDefaultCheck.Unchecked += (_, _) => SetDefault(false);

        var runButton = new Button
        {
            Content = "Run Editor",
            HorizontalAlignment = HorizontalAlignment.Left
        };

        runButton.Click += (_, _) => RunEditor();

        Content = new StackPanel
        {
            Margin = new Thickness(10),
            Spacing = 8,
            Children =
            {
                _useDefaultCheck,

                new TextBlock { Text = "Project path:" },
                _projectPathBox,

                new TextBlock { Text = "Runner path:" },
                _runnerPathBox,

                runButton
            }
        };
    }

    private void SetDefault(bool useDefault)
    {
        _projectPathBox.IsEnabled = !useDefault;
        _runnerPathBox.IsEnabled = !useDefault;

        if (useDefault)
        {
            _projectPathBox.Text = DefaultProjectPath;
            _runnerPathBox.Text = DefaultRunnerPath;
        }
    }

    private void RunEditor()
    {
        string projectPath = _projectPathBox.Text ?? "";
        string runnerPath = _runnerPathBox.Text ?? "";

        new Editor(projectPath, runnerPath);
    }
}
