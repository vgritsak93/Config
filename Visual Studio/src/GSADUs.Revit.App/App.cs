using Autodesk.Revit.UI;
using System.Linq;

namespace GSADUs.BatchExport
{
  public class App : IExternalApplication
  {
    public Result OnStartup(UIControlledApplication app)
    {
      const string tab = "GSADUs Tools"; // existing pyRevit tab
      var panel = app.GetRibbonPanels(tab).FirstOrDefault(p => p.Name == "Batch Tools")
                 ?? app.CreateRibbonPanel(tab, "Batch Tools");

      string asm = typeof(App).Assembly.Location;
      var btn = new PushButtonData(
        "BatchExportBtn",
        "Revit Batch Export",
        asm,
        "GSADUs.Revit.Commands.BatchExportCommand"
      );

      var push = panel.AddItem(btn) as PushButton;
      push.ToolTip = "Export selection sets as separate RVT files.";
      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
  }
}