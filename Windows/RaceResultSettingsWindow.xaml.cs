using System.Windows;
using System.Windows.Media;
using RaceResultClient;
using TimeMaker.Models;
using TimeMaker.Services;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for RaceResultSettingsWindow.xaml
    ///
    /// The API link can be filled in two ways, chosen by the radio buttons at the top:
    /// pasted in by hand, exactly as before, or fetched by logging into RaceResult, picking an
    /// event and letting the app create the Simple API entries on it. Either way what follows is
    /// the same - the link is loaded with "Načítať" and saved with "Uložiť".
    /// </summary>
    public partial class RaceResultSettingsWindow
    {
        /// <summary>
        /// The RaceResult session behind the "prihlásiť sa" mode. Created on the first login and
        /// logged out again when this window closes; nothing else in the app uses it.
        /// </summary>
        private RaceResultSetupService? _setup;

        /// <summary>
        /// The link setup produced and the event it belongs to, kept so the main window's title
        /// only names an event the link in the box actually points at - the user is free to
        /// switch back to manual entry and paste a link for something else.
        /// </summary>
        private string? _createdLink;

        private string? _createdEventName;

        public RaceResultSettingsWindow()
        {
            InitializeComponent();
            App.RaceResult.RaceResultApiLoaded += OnRaceResultApiLoaded;
        }

        private void OnRaceResultApiLoaded(object? sender, Models.RaceResultApiLoadedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                PointsApiLabel.Content = e.PointsApiStatus;
                PointsApiLabel.FontWeight = e.PointsApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                PointsApiLabel.Background = e.PointsApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                RawDataApiLabel.Content = e.ManualApiStatus;
                RawDataApiLabel.FontWeight = e.ManualApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                RawDataApiLabel.Background = e.ManualApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                BibListApiLabel.Content = e.BibsApiStatus;
                BibListApiLabel.FontWeight = e.BibsApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                BibListApiLabel.Background = e.BibsApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                InvalidApiLabel.Content = e.InvalidApiStatus;
                InvalidApiLabel.FontWeight = e.InvalidApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                InvalidApiLabel.Background = e.InvalidApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                RawSearchApiLabel.Content = e.RawSearchApiStatus;
                RawSearchApiLabel.FontWeight = e.RawSearchApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                RawSearchApiLabel.Background = e.RawSearchApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                LoadButton.IsEnabled = true;
                SaveButton.IsEnabled = e is { PointsApiLoaded: true, ManualApiLoaded: true };
            });
        }

        private async void Load(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadButton.IsEnabled = false;
                if (ApiLinkText.Text.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    ThemedDialog.Show("Nezabezpečené pripojenie",
                        "API link používa HTTP bez šifrovania, prístupový kľúč môže byť odchytený. Odporúčame použiť HTTPS.",
                        ThemedDialogIcon.Warning);
                }
                await App.RaceResult.LoadApi(ApiLinkText.Text);
            }
            catch (Exception ex)
            {
                ThemedDialog.Show("Chyba", $"Nastala chyba pri načítavaní API: {ex.Message}", ThemedDialogIcon.Error);
                // Let the user correct the link and retry.
                LoadButton.IsEnabled = true;
            }
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.RaceResult.Start();
                App.RaceResult.RaceResultApiLoaded -= OnRaceResultApiLoaded;
                Close();
            }
            catch (Exception ex)
            {
                ThemedDialog.Show("Chyba", $"Nastala chyba pri spustení API služby: {ex.Message}", ThemedDialogIcon.Error);
            }
        }

        // ── Filling the link by logging into RaceResult ──

        /// <summary>
        /// Shows or hides the login panel. In automatic mode the link field is not the user's to
        /// type in - it is written by "Vytvoriť API" - so it is locked until manual mode is
        /// picked again. Read-only rather than disabled: the theme greys a read-only box, and
        /// the link stays selectable, so it can still be copied out.
        /// </summary>
        private void ApiModeChanged(object sender, RoutedEventArgs e)
        {
            // The radios raise Checked while the XAML is still being parsed, before the rest of
            // the tree exists.
            if (!IsInitialized)
            {
                return;
            }

            var automatic = AutoModeRadio.IsChecked == true;
            AutoPanel.Visibility = automatic ? Visibility.Visible : Visibility.Collapsed;
            ApiLinkText.IsReadOnly = automatic;
        }

        /// <summary>Swaps the user/password inputs for the API key box and back.</summary>
        private void LoginMethodChanged(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }

            var apiKey = ApiKeyLoginRadio.IsChecked == true;
            UserLoginPanel.Visibility = apiKey ? Visibility.Collapsed : Visibility.Visible;
            ApiKeyLoginPanel.Visibility = apiKey ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Login(object sender, RoutedEventArgs e)
        {
            var byApiKey = ApiKeyLoginRadio.IsChecked == true;
            if (byApiKey && string.IsNullOrWhiteSpace(ApiKeyText.Text))
            {
                ThemedDialog.Show("Prihlásenie", "Zadajte API kľúč.", ThemedDialogIcon.Warning);
                return;
            }

            var user = UserText.Text.Trim();
            if (!byApiKey && (string.IsNullOrWhiteSpace(user) || PasswordText.Password.Length == 0))
            {
                ThemedDialog.Show("Prihlásenie", "Zadajte používateľa a heslo.", ThemedDialogIcon.Warning);
                return;
            }

            var server = string.IsNullOrWhiteSpace(ServerText.Text)
                ? RaceResultApiCatalog.DefaultServer
                : ServerText.Text.Trim();

            var credentials = new RaceResultCredentials
            {
                Server = server,
                UseHttps = HttpsCheck.IsChecked == true,
                User = byApiKey ? null : user,
                Password = byApiKey ? null : PasswordText.Password,
                ApiKey = byApiKey ? ApiKeyText.Text.Trim() : null,
            };

            SetAutoBusy(true, "Prihlasovanie…");
            try
            {
                _setup ??= new RaceResultSetupService();
                var events = await _setup.LoginAndListEventsAsync(credentials);
                PasswordText.Clear();

                EventsCombo.Items.Clear();
                foreach (var ev in events)
                {
                    EventsCombo.Items.Add(new ComboBoxItem
                    {
                        Content = string.IsNullOrWhiteSpace(ev.EventDate)
                            ? ev.EventName
                            : $"{ev.EventName} - {ev.EventDate}",
                        Tag = ev,
                    });
                }

                AccountText.Text = byApiKey
                    ? $"Prihlásený na {server} cez API kľúč"
                    : $"Prihlásený na {server} ako {user}";
                ShowLoggedIn(true);

                if (EventsCombo.Items.Count == 0)
                {
                    ShowAutoStatus("Účet nemá žiadne nadchádzajúce podujatia.");
                }
                else
                {
                    EventsCombo.SelectedIndex = 0;
                    ShowAutoStatus($"Načítaných podujatí: {EventsCombo.Items.Count}");
                }
            }
            catch (Exception ex)
            {
                ShowAutoStatus(null);
                ThemedDialog.Show("Chyba", ex.Message, ThemedDialogIcon.Error);
            }
            finally
            {
                SetAutoBusy(false);
            }
        }

        /// <summary>
        /// Asks which APIs to create, creates what the event is missing, corrects what points
        /// somewhere else, leaves everything else alone - and puts the resulting link into the
        /// API link field, from where the user carries on exactly as with a pasted one.
        /// </summary>
        private async void CreateApi(object sender, RoutedEventArgs e)
        {
            if (EventsCombo.SelectedItem is not ComboBoxItem { Tag: EventListItem selected })
            {
                ThemedDialog.Show("Vytvorenie API", "Najprv vyberte podujatie.", ThemedDialogIcon.Warning);
                return;
            }

            if (_setup is not { IsLoggedIn: true })
            {
                ThemedDialog.Show("Vytvorenie API", "Nie ste prihlásený do RaceResult.", ThemedDialogIcon.Warning);
                return;
            }

            var selection = new RaceResultApiSelectWindow(selected.EventName) { Owner = this };
            if (selection.ShowDialog() != true)
            {
                return;
            }

            SetAutoBusy(true, "Vytváranie API…");
            try
            {
                var result = await _setup.ProvisionAsync(selected.Id, selection.SelectedOptional);

                ApiLinkText.Text = result.ApiLink;
                ApiLinkText.IsReadOnly = true;
                _createdLink = result.ApiLink;
                _createdEventName = selected.EventName;
                ShowAutoStatus(result.Describe(selected.EventName));

                ThemedDialog.Show("API pripravené",
                    $"{result.Describe(selected.EventName)}\n\n"
                    + "Odkaz bol vložený do poľa API link, pokračujte tlačidlom Načítať.",
                    ThemedDialogIcon.Success);
            }
            catch (Exception ex)
            {
                ShowAutoStatus(null);
                ThemedDialog.Show("Chyba", $"Nastala chyba pri vytváraní API: {ex.Message}", ThemedDialogIcon.Error);
            }
            finally
            {
                SetAutoBusy(false);
            }
        }

        private async void Logout(object sender, RoutedEventArgs e)
        {
            SetAutoBusy(true, "Odhlasovanie…");
            try
            {
                if (_setup is not null)
                {
                    await _setup.LogoutAsync();
                }
            }
            finally
            {
                EventsCombo.Items.Clear();
                ShowLoggedIn(false);
                SetAutoBusy(false);
                ShowAutoStatus(null);
            }
        }

        /// <summary>
        /// Swaps the login fields for the event picker, and the login button for the pair of
        /// buttons that goes with it. The fields and their button live in different halves of
        /// the panel - one at the top, one pinned to the bottom - so both have to be told.
        /// </summary>
        private void ShowLoggedIn(bool loggedIn)
        {
            LoginPanel.Visibility = loggedIn ? Visibility.Collapsed : Visibility.Visible;
            LoginButton.Visibility = LoginPanel.Visibility;
            EventPanel.Visibility = loggedIn ? Visibility.Visible : Visibility.Collapsed;
            EventActions.Visibility = EventPanel.Visibility;
        }

        /// <summary>
        /// Disables the login panel while a call is in flight, so a second click cannot start a
        /// second one. The "Načítať" / "Uložiť" buttons are left alone - they follow the loaded
        /// API state, not this.
        /// </summary>
        private void SetAutoBusy(bool busy, string? status = null)
        {
            // Hidden, not collapsed - see the XAML: the bar keeps its space either way.
            AutoProgress.Visibility = busy ? Visibility.Visible : Visibility.Hidden;
            LoginPanel.IsEnabled = !busy;
            LoginButton.IsEnabled = !busy;
            EventPanel.IsEnabled = !busy;
            EventActions.IsEnabled = !busy;
            ManualModeRadio.IsEnabled = !busy;
            AutoModeRadio.IsEnabled = !busy;

            if (status is not null)
            {
                ShowAutoStatus(status);
            }
        }

        private void ShowAutoStatus(string? message)
        {
            AutoStatusText.Text = message ?? string.Empty;
            AutoStatusText.Visibility = string.IsNullOrEmpty(message)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        /// <summary>
        /// Ends the RaceResult session the login mode opened, and stops listening for API loads -
        /// the window may be closed without saving, and the handler would otherwise outlive it.
        /// </summary>
        private void WindowClosed(object? sender, EventArgs e)
        {
            App.RaceResult.RaceResultApiLoaded -= OnRaceResultApiLoaded;
            _setup?.Dispose();
            _setup = null;
        }
    }
}
