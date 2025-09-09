using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GSADUs.Revit.Addin;

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

    private void ApplyDefaultOutputPath()
    {
        var tb = this.FindName("OutputPath") as System.Windows.Controls.TextBox
            ?? this.FindName("OutputDir") as System.Windows.Controls.TextBox
            ?? this.FindName("Output") as System.Windows.Controls.TextBox;
        if (tb != null && string.IsNullOrWhiteSpace(tb.Text))
            tb.Text = @"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Output";
    }

    private void BrowseBtn_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        { SelectedPath = OutputDirBox.Text, ShowNewFolderButton = true };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            OutputDirBox.Text = dlg.SelectedPath;
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
            selected, OutputDirBox.Text,
            SaveBeforeBox.IsChecked == true,
            DetachBox.IsChecked == true,
            RecenterBox.IsChecked == true,
            OverwriteBox.IsChecked == true);

        DialogResult = true;
    }
  }
}

