using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

public static class ConfirmDialog
{
    public static async Task<bool> Show(Window owner, string title, string message)
    {
        var result = false;

        var yes = new Button { Content = "Yes", MinWidth = 90 };
        var no = new Button { Content = "No", MinWidth = 90 };

        var dialog = new Window
        {
            Title = title,
            Width = 360,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        yes.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        no.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },

                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { no, yes }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}