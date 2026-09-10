using System.Windows;
using TimeMaker.Models;
using TimeMaker.ViewModels;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for RaceResultApiSelectWindow.xaml
    ///
    /// Asks which of Time Maker's Simple API entries to create on the chosen RaceResult event.
    /// Mandatory ones are ticked and locked, optional ones start ticked and can be turned off;
    /// nothing is deleted either way, so unticking one simply means it is not created.
    /// </summary>
    public partial class RaceResultApiSelectWindow
    {
        private readonly List<ApiSelectionItemViewModel> _items;

        public RaceResultApiSelectWindow(string eventName)
        {
            InitializeComponent();

            EventNameText.Text = eventName;
            _items = RaceResultApiCatalog.Entries
                .Select(e => new ApiSelectionItemViewModel(e))
                .ToList();
            ApiList.ItemsSource = _items;
        }

        /// <summary>
        /// The optional rows left ticked. Mandatory ones are not listed - setup always includes
        /// them - so this is only what the user chose to add on top.
        /// </summary>
        public IReadOnlyCollection<RaceResultApiDefinition> SelectedOptional =>
            _items.Where(i => !i.IsMandatory && i.IsSelected).Select(i => i.Definition).ToList();

        private void Create(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
