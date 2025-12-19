using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GraphicsEngineOpenGL;

namespace DustyEngineHub;

public class MainWindow : Window
{
    private const string DefaultProjectPath =
        "/home/maksym/DustyEngine/TestProject";

    private const string DefaultRunnerPath =
        "/home/maksym/DustyEngine/Runner/bin/Debug/net9.0/Runner";

    private readonly TextBox _projectPathBox;
    private readonly TextBox _runnerPathBox;
    private readonly ComboBox _renderModeBox;
    private readonly CheckBox _useDefaultCheck;

    public MainWindow()
    {
        Title = "DustyEngine Hub";
        Width = 620;
        Height = 280;

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

        _renderModeBox = new ComboBox
        {
            ItemsSource = new[] { RenderMode.Standalone.ToString(), RenderMode.Editor.ToString() },
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 200
        };

        _useDefaultCheck.Checked += (_, _) => SetDefault(true);
        _useDefaultCheck.Unchecked += (_, _) => SetDefault(false);

        var runButton = new Button
        {
            Content = "Run",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        runButton.Click += (_, _) => RunRunner();

        // (опционально) две кнопки быстрее
        var runStandalone = new Button
        {
            Content = "Run Standalone",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        runStandalone.Click += (_, _) =>
        {
            _renderModeBox.SelectedItem = RenderMode.Standalone.ToString();
            RunRunner();
        };

        var runEditor = new Button
        {
            Content = "Run Editor",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        runEditor.Click += (_, _) =>
        {
            _renderModeBox.SelectedItem = RenderMode.Editor.ToString();
            RunRunner();
        };

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

                new TextBlock { Text = "Render mode:" },
                _renderModeBox,

                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        runButton,
                        runStandalone,
                        runEditor
                    }
                }
            }
        };
    }

    private void SetDefault(bool useDefault)
    {
        _projectPathBox.IsEnabled = !useDefault;
        _runnerPathBox.IsEnabled = !useDefault;

        if (!useDefault) return;

        _projectPathBox.Text = DefaultProjectPath;
        _runnerPathBox.Text = DefaultRunnerPath;
    }

    private void RunRunner()
    {
        var projectPath = (_projectPathBox.Text ?? "").Trim();
        var runnerExe = (_runnerPathBox.Text ?? "").Trim();

        var selectedMode = (_renderModeBox.SelectedItem as string) ?? RenderMode.Standalone.ToString();
        if (!Enum.TryParse(selectedMode, true, out RenderMode mode))
            mode = RenderMode.Standalone;

        if (string.IsNullOrWhiteSpace(projectPath) || string.IsNullOrWhiteSpace(runnerExe))
            return;

        var runnerDll = runnerExe + ".dll";

        string cmd;
        if (File.Exists(runnerExe))
        {
            // Runner <ProjectPath> <RenderMode>
            cmd = $"setsid \"{runnerExe}\" \"{projectPath}\" \"{mode}\" >/tmp/dusty_runner.log 2>&1 </dev/null &";
        }
        else if (File.Exists(runnerDll))
        {
            cmd = $"setsid dotnet \"{runnerDll}\" \"{projectPath}\" \"{mode}\" >/tmp/dusty_runner.log 2>&1 </dev/null &";
        }
        else
        {
            // можно сделать MessageBox, но пока просто выходим
            return;
        }

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
