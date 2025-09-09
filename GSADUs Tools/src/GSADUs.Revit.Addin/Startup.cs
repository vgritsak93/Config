using System;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using System.Linq;
using System.Reflection;

namespace GSADUs.Revit.Addin
{
  public class Startup : IExternalApplication
  {
    static BitmapImage Pack(string packUri)
    {
      var img = new BitmapImage();
      img.BeginInit();
      img.UriSource = new Uri(packUri, UriKind.Absolute);
      img.CacheOption = BitmapCacheOption.OnLoad;
      img.EndInit();
      return img;
    }

    public Result OnStartup(UIControlledApplication app)
    {
      var panel = app.GetRibbonPanels(Tab.AddIns).FirstOrDefault(p => p.Name == "GSADUs")
               ?? app.CreateRibbonPanel(Tab.AddIns, "GSADUs");

      var pbd = new PushButtonData(
        "BatchExportBtn", "Batch Export",
        Assembly.GetExecutingAssembly().Location,
        "GSADUs.Revit.Addin.BatchExportCommand");

      var btn = (PushButton)panel.AddItem(pbd);

      var small = Pack("pack://application:,,,/GSADUs.Revit.Addin;component/icons/batch_export_16.png");
      var large = Pack("pack://application:,,,/GSADUs.Revit.Addin;component/icons/batch_export_32.png");
      btn.Image = small;
      btn.LargeImage = large;

      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
  }
}

