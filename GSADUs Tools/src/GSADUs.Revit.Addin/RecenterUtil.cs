using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace GSADUs.Revit.Addin {
    internal static class RecenterUtil {
        static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        public static void CenterSelectionXY(Document doc, IList<ElementId> ids) {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (ids == null || ids.Count == 0) return;

            bool any = false;
            XYZ min = new XYZ(double.PositiveInfinity, double.PositiveInfinity, 0);
            XYZ max = new XYZ(double.NegativeInfinity, double.NegativeInfinity, 0);

            foreach (var id in ids) {
                var e = doc.GetElement(id);
                if (e == null) continue;
                BoundingBoxXYZ bb = null;
                try { bb = e.get_BoundingBox(null); } catch { bb = null; }
                if (bb == null || bb.Min == null || bb.Max == null) continue;
                if (!(Finite(bb.Min.X) && Finite(bb.Min.Y) && Finite(bb.Max.X) && Finite(bb.Max.Y))) continue;

                min = new XYZ(Math.Min(min.X, bb.Min.X), Math.Min(min.Y, bb.Min.Y), 0);
                max = new XYZ(Math.Max(max.X, bb.Max.X), Math.Max(max.Y, bb.Max.Y), 0);
                any = true;
            }
            if (!any) return;

            var cx = 0.5 * (min.X + max.X);
            var cy = 0.5 * (min.Y + max.Y);
            if (!(Finite(cx) && Finite(cy))) return;

            var delta = new XYZ(-cx, -cy, 0);
            using (var t = new Transaction(doc, "GSADUs Center To Origin")) {
                t.Start();
                try { ElementTransformUtils.MoveElements(doc, (ICollection<ElementId>)ids, delta); t.Commit(); }
                catch { t.RollBack(); throw; }
            }
        }
    }
}
