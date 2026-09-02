using System.Windows;
using ClickWrap;
using TimeMaker.Windows;

namespace TimeMaker.Services
{
    /// <summary>
    /// Startup update check against the ClickWrap server, plus the hand-off to
    /// this app's updater when the user accepts. The library itself has no UI -
    /// the dialog and the shutdown are ours.
    /// </summary>
    public class UpdateService : IDisposable
    {
        /// <summary>
        /// App id as published on the ClickWrap server. Permanent - the installer
        /// (ClickWrap/src/ClickWrap.Installer/apps/time-maker.yaml) is built against it.
        /// </summary>
        private const string AppId = "time-maker";

        /// <summary>Must match the serverUrl in that same installer config.</summary>
        private const string ServerUrl = "https://install.susky.net";

        private readonly UpdateClient _client = new(ServerUrl);

        /// <summary>
        /// Asks the server for a newer version. Returns <c>null</c> when the app is
        /// up to date, when the server has no versions for this app id, or when the
        /// check could not be made - an update check must never disrupt a start.
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var update = await _client.CheckForUpdateAsync(AppId, cancellationToken);
                App.Logger.Log(update is null
                    ? "Update check: application is up to date"
                    : $"Update check: version {update.LatestVersion} is available");
                return update;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                // Offline, server down, or a bad response. Log it and stay quiet -
                // the user came here to time a race, not to hear about our server.
                App.Logger.LogWarning($"Update check failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Runs the check and, if a newer version exists, offers it to the user.
        /// Call once the main window is up so the dialog has an owner.
        /// </summary>
        public async Task PromptIfUpdateAvailableAsync(CancellationToken cancellationToken = default)
        {
            var update = await CheckForUpdateAsync(cancellationToken);
            if (update is null)
            {
                return;
            }

            var message = $"Je dostupná nová verzia {update.LatestVersion}.\n" +
                          $"Používate verziu {InstalledApp.GetCurrentVersion(AppId)}.";

            if (!string.IsNullOrWhiteSpace(update.ReleaseNotes))
            {
                message += $"\n\nZmeny:\n{update.ReleaseNotes.Trim()}";
            }

            message += "\n\nChcete ju nainštalovať teraz? Aplikácia sa zatvorí.";

            if (!ThemedDialog.Show("Nová verzia", message, ThemedDialogIcon.Info, ThemedDialogButtons.YesNo))
            {
                App.Logger.Log($"Update to {update.LatestVersion} postponed by the user");
                return;
            }

            StartUpdate(update.LatestVersion);
        }

        /// <summary>
        /// Launches the updater and closes the app. Both halves matter: setup.exe
        /// starts the app again once it has updated it, so an app that stays open
        /// ends up running beside a newer copy of itself.
        /// </summary>
        private static void StartUpdate(string version)
        {
            // Shutdown() rather than InstalledApp.UpdateAndExit() - that kills the
            // process outright, and this app has serial ports and an API client to
            // close first (App.OnExit).
            if (InstalledApp.StartUpdater(AppId))
            {
                App.Logger.Log($"Updater started for version {version}, shutting down");
                Application.Current.Shutdown();
                return;
            }

            App.Logger.LogWarning("Update requested but no updater is registered for this install");
            ThemedDialog.Show("Aktualizácia",
                "Aktualizátor sa nenašiel. Táto kópia aplikácie zrejme nebola nainštalovaná " +
                "inštalátorom Time Maker. Nainštalujte novú verziu ručne.",
                ThemedDialogIcon.Warning);
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
