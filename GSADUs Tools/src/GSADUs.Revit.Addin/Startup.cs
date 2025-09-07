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
      var panel = app.GetRibbonPanels(Tab.AddIns).FirstOrDefault(p => p.Name == "GSADUs")
               ?? app.CreateRibbonPanel(Tab.AddIns, "GSADUs");

      var pbd = new PushButtonData(
        "BatchExportBtn", "Batch Export",
        Assembly.GetExecutingAssembly().Location,
        "GSADUs.Revit.Addin.BatchExportCommand");

      var btn = (PushButton)panel.AddItem(pbd);

      string asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
      string icons  = Path.Combine(asmDir, "icons");
      var p32 = Path.Combine(icons, "batch_export_32.png");
      var p16 = Path.Combine(icons, "batch_export_16.png");
      if (File.Exists(p32)) btn.LargeImage = new BitmapImage(new Uri(p32));
      if (File.Exists(p16)) btn.Image      = new BitmapImage(new Uri(p16));

      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
  }
}
