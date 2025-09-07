using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace GSADUs.Revit.Addin
{
    public class Startup : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            const string panelName = "GSADUs";

            // Create/find panel on the built-in Add-Ins tab
            var panel = app.GetRibbonPanels(Tab.AddIns)
                           .FirstOrDefault(p => p.Name.Equals(panelName, StringComparison.OrdinalIgnoreCase))
                        ?? app.CreateRibbonPanel(Tab.AddIns, panelName);
            // (Alternative: app.CreateRibbonPanel(panelName); // defaults to Add-Ins)

            var pbd = new PushButtonData(
              "BatchExportBtn", "Batch Export",
              Assembly.GetExecutingAssembly().Location,
              "GSADUs.Revit.Addin.BatchExportCommand");

            var btn = (PushButton)panel.AddItem(pbd);

            // icons
            string asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string icons = Path.Combine(asmDir, "icons");
            btn.LargeImage = new BitmapImage(new Uri(Path.Combine(icons, "batch_export_32.png")));
            btn.Image = new BitmapImage(new Uri(Path.Combine(icons, "batch_export_16.png")));

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
    }
}

