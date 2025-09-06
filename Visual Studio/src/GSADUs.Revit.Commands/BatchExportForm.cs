using System;
using System.IO;
using System.Windows.Forms;

namespace GSADUs.Revit.Commands
{
    public sealed class BatchExportForm : Form
    {
        public string InputFolder { get; private set; }
        public string OutputFolder { get; private set; }

        TextBox tbIn = new TextBox { ReadOnly = true, Dock = DockStyle.Top };
        Button btnIn = new Button { Text = "Choose input folder", Dock = DockStyle.Top };
        TextBox tbOut = new TextBox { ReadOnly = true, Dock = DockStyle.Top };
        Button btnOut = new Button { Text = "Choose output folder", Dock = DockStyle.Top };
        Button ok = new Button { Text = "OK", Dock = DockStyle.Right };
        Button cancel = new Button { Text = "Cancel", Dock = DockStyle.Right };

        public BatchExportForm()
        {
            Text = "Batch Export";
            Width = 520; Height = 160; StartPosition = FormStartPosition.CenterParent;

            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8), AutoSize = true };
            panel.Controls.Add(btnIn);
            panel.Controls.Add(tbIn);
            panel.Controls.Add(btnOut);
            panel.Controls.Add(tbOut);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8) };
            bottom.Controls.Add(ok);
            bottom.Controls.Add(cancel);
            ok.Left = Width - 200; ok.Top = 8;
            cancel.Left = Width - 120; cancel.Top = 8;

            Controls.Add(panel);
            Controls.Add(bottom);

            btnIn.Click += (_, __) => { var p = PickFolder("Select folder with RVT files"); if (p != null) { tbIn.Text = p; InputFolder = p; } };
            btnOut.Click += (_, __) => { var p = PickFolder("Select output folder"); if (p != null) { tbOut.Text = p; OutputFolder = p; } };
            ok.Click += (_, __) => { if (Directory.Exists(InputFolder) && Directory.Exists(OutputFolder)) DialogResult = DialogResult.OK; };
            cancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; };
        }

        static string PickFolder(string description)
        {
            using var dlg = new FolderBrowserDialog { Description = description, ShowNewFolderButton = true };
            return dlg.ShowDialog() == DialogResult.OK ? dlg.SelectedPath : null;
        }
    }
}
