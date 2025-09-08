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
  }
}

