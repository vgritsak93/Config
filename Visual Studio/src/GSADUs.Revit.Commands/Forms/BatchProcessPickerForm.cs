// Forms/BatchProcessPickerForm.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GSADUsRevitAddin.Batch;

namespace GSADUsRevitAddin.Forms
{
    public sealed class BatchProcessPickerForm : Form
    {
        private readonly List<IBatchProcess> _available;
        private TextBox _txtFolder = null!;
        private Button _btnBrowse = null!;
        private CheckedListBox _clb = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;

        public string SelectedFolder => _txtFolder.Text;
        public List<IBatchProcess> SelectedProcesses => _clb.CheckedItems.Cast<IBatchProcess>().ToList();

        public BatchProcessPickerForm(List<IBatchProcess> available)
        {
            _available = available ?? new List<IBatchProcess>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Batch Process Setup";
            Width = 640;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;

            _txtFolder = new TextBox { Left = 16, Top = 16, Width = 480, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _btnBrowse = new Button { Left = 504, Top = 14, Width = 100, Text = "Browse...", Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _btnBrowse.Click += (_, __) =>
            {
                using var dlg = new FolderBrowserDialog { ShowNewFolderButton = false, Description = "Select folder containing .rvt files" };
                if (dlg.ShowDialog(this) == DialogResult.OK) _txtFolder.Text = dlg.SelectedPath;
            };

            _clb = new CheckedListBox
            {
                Left = 16,
                Top = 56,
                Width = 588,
                Height = 320,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                CheckOnClick = true
            };
            _clb.DisplayMember = "Name";

            _btnOk = new Button { Text = "OK", Left = 404, Width = 100, Top = 392, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, DialogResult = DialogResult.OK };
            _btnCancel = new Button { Text = "Cancel", Left = 504, Width = 100, Top = 392, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { _txtFolder, _btnBrowse, _clb, _btnOk, _btnCancel });

            Load += (_, __) =>
            {
                _clb.Items.Clear();
                foreach (var p in _available) _clb.Items.Add(p, false);
            };

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }
    }
}
