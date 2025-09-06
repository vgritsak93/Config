using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

namespace GSADUs.Revit.Commands
{
    public static class ExportHelper
    {
        // Stub: Duplicate document or isolate elements
        public static Document PrepareExportDocument(UIDocument uiDoc, ICollection<ElementId> elementIds)
        {
            var doc = uiDoc.Document;
            var view = doc.ActiveView;

            using (var tx = new Transaction(doc, "Isolate Elements"))
            {
                tx.Start();
                view.IsolateElementsTemporary(elementIds);
                tx.Commit();
            }

            return doc;
        }

        // Stub: Move bounding box center to (0,0,0)
        public static void MoveBoundingBoxToOrigin(Document doc, ICollection<ElementId> elementIds)
        {
            // TODO: Implement bounding box logic
        }

        // Stub: Purge document
        public static void Purge(Document doc, int passes)
        {
            // TODO: Implement purge logic
        }

        // Stub: Save document as RVT
        public static void SaveAs(Document doc, string outputPath, bool compact, bool preview)
        {
            var options = new SaveAsOptions
            {
                Compact = compact,
                PreviewViewId = ElementId.InvalidElementId // disables preview
            };

            doc.SaveAs(outputPath, options);
        }
    }
}