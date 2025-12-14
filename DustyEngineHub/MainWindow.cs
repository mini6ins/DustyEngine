using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace DustyEngineHub;

public class MainWindow : Window
{
    private const string DefaultProjectPath =
        "/home/maksym/DustyEngine/TestProject";

    private const string DefaultRunnerPath =
        "/home/maksym/DustyEngine/Runner/bin/Debug/net9.0/Runner";

    private const string DefaultEditorExe =
        "/home/maksym/DustyEngine/DustyEngineEditor/bin/Debug/net9.0/DustyEngineEditor";

    private readonly TextBox _projectPathBox;
    private readonly TextBox _runnerPathBox;
    private readonly TextBox _editorPathBox;
    private readonly CheckBox _useDefaultCheck;

    public MainWindow()
    {
        Title = "DustyEngine Hub";
        Width = 620;
        Height = 300;

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

        _editorPathBox = new TextBox
        {
            Text = DefaultEditorExe,
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

                new TextBlock { Text = "Editor path:" },
                _editorPathBox,

                runButton
            }
        };
    }

    private void SetDefault(bool useDefault)
    {
        _projectPathBox.IsEnabled = !useDefault;
        _runnerPathBox.IsEnabled = !useDefault;
        _editorPathBox.IsEnabled = !useDefault;

        if (!useDefault) return;

        _projectPathBox.Text = DefaultProjectPath;
        _runnerPathBox.Text = DefaultRunnerPath;
        _editorPathBox.Text = DefaultEditorExe;
    }


    private void RunEditor()
    {
        var projectPath = _projectPathBox.Text ?? "";
        var runnerPath = _runnerPathBox.Text ?? "";

        var editorExe =  _editorPathBox.Text  ?? "";
        var editorDll =  _editorPathBox.Text + ".dll";

        string cmd;

        if (File.Exists(editorExe))
            cmd = $"setsid \"{editorExe}\" \"{projectPath}\" \"{runnerPath}\" >/tmp/dusty_editor.log 2>&1 </dev/null &";

        else if (File.Exists(editorDll))
            cmd = $"setsid dotnet \"{editorDll}\" \"{projectPath}\" \"{runnerPath}\" >/tmp/dusty_editor.log 2>&1 </dev/null &";

        else return;

        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add(cmd);

        Process.Start(psi);

        Environment.Exit(0);
    }
}
