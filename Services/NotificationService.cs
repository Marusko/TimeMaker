using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows;
using TimeMaker.Models;
using Windows.UI.Notifications;

namespace TimeMaker.Services
{
    public class NotificationService
    {
        public static void ShowInfoNotification(string title, string message)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .SetToastDuration(ToastDuration.Short)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTimeOffset.Now.AddSeconds(10);
                });
        }

        public static void ShowRetryNotification(string title, string message, string sourceId, string dataId)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddInputTextBox("bibInput", "Nové číslo", "Nové číslo pre tento impulz")
                .AddButton(new ToastButton()
                    .SetContent("Ignorovať")
                    .AddArgument("action", "ignore"))
                .AddButton(new ToastButton()
                    .SetContent("Znova nahrať")
                    .AddArgument("action", "retry")
                    .AddArgument("dataId", dataId)
                    .AddArgument("sourceId", sourceId))
                .AddButton(new ToastButton()
                    .SetContent("Zmeniť a odoslať")
                    .AddArgument("action", "change")
                    .AddArgument("dataId", dataId)
                    .AddArgument("sourceId", sourceId))
                .SetToastScenario(ToastScenario.Reminder)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(1);
                    toast.Activated += Toast_Activated;
                });
        }

        private static void Toast_Activated(ToastNotification sender, object args)
        {
            if (args is ToastActivatedEventArgs activated)
            {
                var arguments = ToastArguments.Parse(activated.Arguments);
                var userInputs = activated.UserInput;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dataId = arguments.TryGetValue("dataId", out var dataIdValue) ? dataIdValue : string.Empty;
                    var sourceId = arguments.TryGetValue("sourceId", out var sourceIdValue) ? sourceIdValue : string.Empty;
                    var action = arguments.TryGetValue("action", out var actionValue) ? actionValue : string.Empty;

                    if (!string.IsNullOrEmpty(action) && action.Equals("retry") && !string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(dataId))
                    {
                        var source = App.SourceManager.GetSource(sourceId);
                        if (source != null)
                        {
                            if (source.LogDataLookup.TryGetValue(dataId, out var item))
                            {
                                var data = new DataModel()
                                {
                                    Id = item.Id,
                                    SourceId = sourceId,
                                    Bib = item.Bib,
                                    Time = item.Time,
                                    TimingPoint = item.TimingPoint,
                                    RawData = item.Raw,
                                    IsClear = item.IsClear
                                };
                                item.Status = UploadStatus.Pending;
                                App.RaceResult.AddManual(data);
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(action) && action.Equals("change") && !string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(dataId))
                    {
                        if (userInputs.TryGetValue("bibInput", out var input))
                        {
                            var inputText = input?.ToString();
                            var source = App.SourceManager.GetSource(sourceId);
                            if (source != null && !string.IsNullOrEmpty(inputText))
                            {
                                if (source.LogDataLookup.TryGetValue(dataId, out var itemVm))
                                {
                                    itemVm.BibChanges.Add($"{itemVm.Bib} -> {inputText}");
                                    itemVm.Bib = inputText;

                                    var data = new DataModel()
                                    {
                                        Id = itemVm.Id,
                                        SourceId = sourceId,
                                        Bib = itemVm.Bib,
                                        Time = itemVm.Time,
                                        TimingPoint = itemVm.TimingPoint,
                                        RawData = itemVm.Raw,
                                        IsClear = itemVm.IsClear
                                    };
                                    itemVm.Status = UploadStatus.Pending;
                                    App.RaceResult.AddManual(data);
                                }
                            }
                        }
                    }
                });
            }
            sender.Activated -= Toast_Activated;
        }
    }
}