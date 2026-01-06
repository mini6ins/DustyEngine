using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using DustyProjectHub.UI.Windows;

namespace DustyProjectHub;

public static class MessageDialog
{
    public static async Task Show(string title, string message)
    {
        var okBtn = new Button
        {
            Content = "OK",
            MinWidth = 90,
            IsDefault = true
        };

        var window = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            CanResize = false,
            SystemDecorations = SystemDecorations.Full,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        okBtn.Click += (_, _) => window.Close();

        window.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    FontSize = 14
                },

                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children =
                    {
                        okBtn
                    }
                }
            }
        };

        await window.ShowDialog(MainWindow.Instance!);
    }
}