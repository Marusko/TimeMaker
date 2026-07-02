using System.Media;
using System.Windows;
using System.Windows.Media;

namespace TimeMaker.Windows
{
    public enum ThemedDialogButtons
    {
        Ok,
        YesNo,
    }

    public enum ThemedDialogIcon
    {
        None,
        Info,
        Warning,
        Error,
        Success,
    }

    /// <summary>
    /// A modal dialog styled with the application theme, used in place of
    /// <see cref="MessageBox"/>.
    /// </summary>
    public partial class ThemedDialog : Window
    {
        private readonly SystemSound? _sound;

        public ThemedDialog(string title, string message, ThemedDialogButtons buttons, ThemedDialogIcon icon)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;

            SetIcon(icon);

            // Mirror the sounds MessageBox plays for each MessageBoxImage.
            _sound = icon switch
            {
                ThemedDialogIcon.Error => SystemSounds.Hand,
                ThemedDialogIcon.Warning => SystemSounds.Exclamation,
                ThemedDialogIcon.Info => SystemSounds.Asterisk,
                ThemedDialogIcon.Success => SystemSounds.Asterisk,
                _ => null,
            };
            Loaded += (_, _) => _sound?.Play();

            if (buttons == ThemedDialogButtons.YesNo)
            {
                PrimaryButton.Content = "Áno";
                SecondaryButton.Content = "Nie";
                SecondaryButton.Visibility = Visibility.Visible;
                SecondaryButton.IsCancel = true;
            }
            else
            {
                PrimaryButton.Content = "OK";
                SecondaryButton.Visibility = Visibility.Collapsed;
                PrimaryButton.IsCancel = true;
            }

            PrimaryButton.IsDefault = true;
        }

        private void SetIcon(ThemedDialogIcon icon)
        {
            if (icon == ThemedDialogIcon.None)
            {
                IconBadge.Visibility = Visibility.Collapsed;
                return;
            }

            var (background, foreground, glyph) = icon switch
            {
                ThemedDialogIcon.Error => ("ErrorSoftBrush", "ErrorBrush", "✕"),
                ThemedDialogIcon.Warning => ("WarningSoftBrush", "WarningBrush", "!"),
                ThemedDialogIcon.Success => ("SuccessSoftBrush", "SuccessBrush", "✓"),
                _ => ("AccentSoftBrush", "AccentBrush", "i"),
            };

            IconBadge.Background = (Brush)FindResource(background);
            IconGlyph.Foreground = (Brush)FindResource(foreground);
            IconGlyph.Text = glyph;
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void SecondaryButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        /// <summary>
        /// Shows the dialog modally, centered over the currently active window.
        /// Safe to call from any thread. Returns true for OK / Áno, false for
        /// Nie or dismissal.
        /// </summary>
        public static bool Show(string title, string message,
            ThemedDialogIcon icon = ThemedDialogIcon.None,
            ThemedDialogButtons buttons = ThemedDialogButtons.Ok)
        {
            var app = Application.Current;
            if (app is null)
            {
                // No WPF application (e.g. during a hard crash) - fall back.
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.None);
                return true;
            }

            // Sources raise errors from background threads (serial port, API).
            if (!app.Dispatcher.CheckAccess())
            {
                return app.Dispatcher.Invoke(() => Show(title, message, icon, buttons));
            }

            var dialog = new ThemedDialog(title, message, buttons, icon);

            // Prefer the active window so the dialog stacks above topmost
            // dialogs (source windows are Topmost), not under them.
            var owner = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible)
                        ?? (app.MainWindow is { IsVisible: true } main ? main : null);
            if (owner is not null && owner != dialog)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.Topmost = owner.Topmost;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.Topmost = true;
            }

            return dialog.ShowDialog() == true;
        }
    }
}
