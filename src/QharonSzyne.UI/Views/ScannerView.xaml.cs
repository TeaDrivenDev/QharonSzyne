using System.Windows;

namespace QharonSzyne.UI.Views
{
    public partial class ScannerView : Window
    {
        private static readonly MaterialDesignThemes.Wpf.Icon materialDesignDummyReference =
            new MaterialDesignThemes.Wpf.Icon();

        private static readonly MaterialDesignColors.SwatchesProvider materialDesignColorsDummyReference =
            new MaterialDesignColors.SwatchesProvider();

        public ScannerView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Cliquez le petit bouton");
        }
    }
}
