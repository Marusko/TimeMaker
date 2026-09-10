using System.Windows;
using TimeMaker.Models;

namespace TimeMaker.ViewModels
{
    /// <summary>
    /// One checkbox row in the API selection dialog. Mandatory entries are ticked and locked -
    /// the app cannot work without them - so only optional rows can be toggled. A row can stand
    /// for more than one Simple API entry (impulse invalidation needs two), and then the whole
    /// row is switched on or off together.
    /// </summary>
    public class ApiSelectionItemViewModel
    {
        public ApiSelectionItemViewModel(RaceResultApiDefinition definition)
        {
            Definition = definition;
            // Everything starts selected; the user unticks what they do not want.
            IsSelected = true;
        }

        /// <summary>The catalog row this represents; what setup is handed back for the ticked ones.</summary>
        public RaceResultApiDefinition Definition { get; }

        public string Title => Definition.Title;

        /// <summary>The Simple API labels this row covers, as RaceResult shows them.</summary>
        public string LabelText => Definition.LabelText;

        public string? Hint => Definition.Hint;

        public Visibility HintVisibility =>
            string.IsNullOrWhiteSpace(Hint) ? Visibility.Collapsed : Visibility.Visible;

        public bool IsMandatory => Definition.Mandatory;

        /// <summary>Mandatory rows keep their checkbox ticked but greyed out.</summary>
        public bool IsToggleable => !IsMandatory;

        /// <summary>The "Povinné" badge, shown on the rows that cannot be turned off.</summary>
        public Visibility MandatoryVisibility =>
            IsMandatory ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Bound two-way from the checkbox. Mandatory rows are disabled in the UI, so this only
        /// ever changes on an optional one.
        /// </summary>
        public bool IsSelected { get; set; }
    }
}
