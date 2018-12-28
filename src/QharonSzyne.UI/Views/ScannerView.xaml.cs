using System;
using System.Windows;

using QharonSzyne.Core.ViewModels;
using QharonSzyne.UI.Utilities;

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

        private ScannerViewModel ViewModel => (ScannerViewModel)this.DataContext;

        private void SelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowserDialog.ShowNewFolderButton = false;

                if (!String.IsNullOrWhiteSpace(this.ViewModel.SourceDirectory.Value)
                    && System.IO.Directory.Exists(this.ViewModel.SourceDirectory.Value))
                {
                    folderBrowserDialog.SelectedPath = this.ViewModel.SourceDirectory.Value;
                }

                var win = new Win32Window((new System.Windows.Interop.WindowInteropHelper(this).Handle));

                var result = folderBrowserDialog.ShowDialog(win);

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    this.ViewModel.SourceDirectory.Value = folderBrowserDialog.SelectedPath;
                }
            }
        }
    }
}
