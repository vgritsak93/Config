using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GSADUs.Revit.Addin.UI
{
    public partial class BatchExportWindow : Window
    {
        public BatchExportSettings? Result { get; private set; }

        public BatchExportWindow(IEnumerable<string> setNames)
        {
            InitializeComponent();
            ApplyDefaultOutputPath();
            SetsList.ItemsSource = setNames.ToList();

            const string DefaultExportDir = @"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Output";
            OutputDirBox.Text = System.IO.Directory.Exists(DefaultExportDir)
              ? DefaultExportDir
              : System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                CheckFileExists = false,
                FileName = "Select Folder"
            };
            if (dlg.ShowDialog() == true)
            {
                var path = System.IO.Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(path))
                    OutputDirBox.Text = path;
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            var selected = SetsList.SelectedItems.Cast<string>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Select at least one set.");
                return;
            }
            if (string.IsNullOrWhiteSpace(OutputDirBox.Text) || !System.IO.Directory.Exists(OutputDirBox.Text))
            {
                MessageBox.Show(this, "Choose a valid output folder.");
                return;
            }

            Result = new BatchExportSettings(
              selected,
              OutputDirBox.Text,
              SaveBeforeBox.IsChecked == true,
              DetachBox.IsChecked == true,
              RecenterBox.IsChecked == true,
              OverwriteBox.IsChecked == true
            );

            DialogResult = true;
        }

        private void ApplyDefaultOutputPath()
        {
            var tb = this.FindName("OutputPath") as System.Windows.Controls.TextBox
                  ?? this.FindName("OutputDir") as System.Windows.Controls.TextBox
                  ?? this.FindName("Output") as System.Windows.Controls.TextBox;

            if (tb != null && string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = @"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Output";
        }
    }
}
