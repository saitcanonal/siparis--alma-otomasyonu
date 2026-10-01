using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace RestaurantManager
{
    // Avalonia'da WPF'teki gibi hazır bir "MessageBox.Show(...)" yoktur.
    // Bu sınıf, orijinal projedeki bilgi ve onay (Evet/Hayır) pencerelerinin
    // aynısını üretmek için küçük, bağımsız pencereler oluşturur.
    public static class SimpleDialogs
    {
        public static async Task ShowInfoAsync(Window owner, string message, string title = "Bilgi")
        {
            var okButton = new Button
            {
                Content = "Tamam",
                Padding = new Thickness(16, 6),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var dialog = new Window
            {
                Title = title,
                Width = 380,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                        okButton
                    }
                }
            };

            okButton.Click += (s, e) => dialog.Close();
            await dialog.ShowDialog(owner);
        }

        public static async Task<bool> ShowConfirmAsync(Window owner, string message, string title = "Onay")
        {
            bool sonuc = false;

            var evetButton = new Button { Content = "Evet", Padding = new Thickness(16, 6) };
            var hayirButton = new Button { Content = "Hayır", Padding = new Thickness(16, 6) };

            var dialog = new Window
            {
                Title = title,
                Width = 380,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 10,
                            Children = { evetButton, hayirButton }
                        }
                    }
                }
            };

            evetButton.Click += (s, e) => { sonuc = true; dialog.Close(); };
            hayirButton.Click += (s, e) => { sonuc = false; dialog.Close(); };

            await dialog.ShowDialog(owner);
            return sonuc;
        }
    }
}
