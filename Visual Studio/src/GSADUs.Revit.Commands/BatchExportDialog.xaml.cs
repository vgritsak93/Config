using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using GSADUs.Core;
using GSADUs.Revit.Commands; // If BatchExportDialog is in this namespace

namespace GSADUs.Revit.Commands
{
    public partial class BatchExportDialog : Window
    {
        private List<string> _selectionSetNames;
        private BatchExportSettings _settings;

        public BatchExportDialog(List<string> selectionSetNames, BatchExportSettings settings)
        {
            InitializeComponent();
            _selectionSetNames = selectionSetNames ?? new List<string>();
            _settings = settings ?? new BatchExportSettings();
            PopulateSelectionSets();
            BindSettingsToUI();
        }

        private void PopulateSelectionSets()
        {
            SelectionSetsList.Items.Clear();
            foreach (var name in _selectionSetNames)
                SelectionSetsList.Items.Add(name);
        }

        private void BindSettingsToUI()
        {
            OutputFolderText.Text = _settings.ExportRoot;
            LogFolderText.Text = _settings.LogFolder;
            SaveBeforeExportCheck.IsChecked = _settings.SaveBeforeExport;
            DetachIfCentralCheck.IsChecked = false; // Default, can be set from settings if needed
            DryRunCheck.IsChecked = false;
            LimitCountText.Text = _settings.LimitCount.ToString();
            PurgePassesText.Text = _settings.PurgePasses.ToString();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectionSetsList.SelectAll();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            SelectionSetsList.UnselectAll();
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var helper = new WindowInteropHelper(this);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFolderText.Text = dialog.SelectedPath;
            }
        }

        private void BrowseLog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var helper = new WindowInteropHelper(this);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LogFolderText.Text = dialog.SelectedPath;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (SelectionSetsList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one selection set.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(OutputFolderText.Text))
            {
                MessageBox.Show("Please specify an output folder.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public List<string> GetSelectedSetNames()
        {
            var selected = new List<string>();
            foreach (var item in SelectionSetsList.SelectedItems)
                selected.Add(item.ToString());
            return selected;
        }

        public BatchExportDialogOptions Options => new BatchExportDialogOptions
        {
            OutputFolder = OutputFolderText.Text,
            LogFolder = LogFolderText.Text,
            SaveBeforeExport = SaveBeforeExportCheck.IsChecked == true,
            DetachIfCentral = DetachIfCentralCheck.IsChecked == true,
            DryRun = DryRunCheck.IsChecked == true,
            LimitCount = int.TryParse(LimitCountText.Text, out var limit) ? limit : 0,
            PurgePasses = int.TryParse(PurgePassesText.Text, out var purge) ? purge : 3
        };
    }

    public class BatchExportDialogOptions
    {
        public string OutputFolder { get; set; }
        public string LogFolder { get; set; }
        public bool SaveBeforeExport { get; set; }
        public bool DetachIfCentral { get; set; }
        public bool DryRun { get; set; }
        public int LimitCount { get; set; }
        public int PurgePasses { get; set; }
    }
}

// Prompt user for output folder and log folder using Windows Forms dialogs
string outputFolder = "";
string logFolder = "";
using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
{
    folderDialog.Description = "Select Output Folder";
    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        outputFolder = folderDialog.SelectedPath;
    else
        return Result.Cancelled;

    folderDialog.Description = "Select Log Folder";
    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        logFolder = folderDialog.SelectedPath;
    else
        return Result.Cancelled;
}

// Use all selection sets and default options for now
var selectedSets = new List<string>(selectionSetNames);
var options = new
{
    OutputFolder = outputFolder,
    LogFolder = logFolder,
    SaveBeforeExport = settings.SaveBeforeExport,
    LimitCount = settings.LimitCount,
    PurgePasses = settings.PurgePasses,
    DetachIfCentral = false,
    DryRun = false
};
