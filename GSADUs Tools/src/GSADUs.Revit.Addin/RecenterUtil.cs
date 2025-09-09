using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace GSADUs.Revit.Addin
{
    internal static class RecenterUtil
    {
        private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        /// <summary>
        /// Center the given selection set to Internal Origin in XY, using union BB of Walls.
        /// Matches the prototype pyRevit logic.
        /// </summary>
        public static void CenterSelectionXY(Document doc, IList<ElementId> ids)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (ids == null || ids.Count == 0) return;

            double minx = double.PositiveInfinity, miny = double.PositiveInfinity;
            double maxx = double.NegativeInfinity, maxy = double.NegativeInfinity;
            bool found = false;

            foreach (var id in ids)
            {
                var e = doc.GetElement(id);
                if (e == null) continue;
                if (e.Category == null || e.Category.Id != new ElementId(BuiltInCategory.OST_Walls)) continue;

                BoundingBoxXYZ bb = null;
                try { bb = e.get_BoundingBox(null); } catch { }
                if (bb == null || bb.Min == null || bb.Max == null) continue;

                var xs = new[] { bb.Min.X, bb.Max.X };
                var ys = new[] { bb.Min.Y, bb.Max.Y };

                if (Finite(xs[0]) && Finite(xs[1]) && Finite(ys[0]) && Finite(ys[1]))
                {
                    minx = Math.Min(minx, Math.Min(xs[0], xs[1]));
                    miny = Math.Min(miny, Math.Min(ys[0], ys[1]));
                    maxx = Math.Max(maxx, Math.Max(xs[0], xs[1]));
                    maxy = Math.Max(maxy, Math.Max(ys[0], ys[1]));
                    found = true;
                }
            }

            if (!found) return; // no valid Walls bounding boxes

            if (!Finite(minx) || !Finite(miny) || !Finite(maxx) || !Finite(maxy) ||
                minx > maxx || miny > maxy) return;

            var cx = 0.5 * (minx + maxx);
            var cy = 0.5 * (miny + maxy);
            if (!Finite(cx) || !Finite(cy)) return;

            var delta = new XYZ(-cx, -cy, 0.0);
            if (!Finite(delta.X) || !Finite(delta.Y)) return;

            // Remember pinned state, unpin before move
            var pinned = new Dictionary<ElementId, bool>();
            foreach (var id in ids)
            {
                var e = doc.GetElement(id);
                if (e != null) pinned[id] = e.Pinned;
            }

            using (var tg = new TransactionGroup(doc, "GSADUs: Center To Origin"))
            {
                tg.Start();
                using (var t = new Transaction(doc, "Move ADU model"))
                {
                    t.Start();
                    try
                    {
                        foreach (var kv in pinned)
                        {
                            var e = doc.GetElement(kv.Key);
                            if (e != null && e.Pinned) e.Pinned = false;
                        }

                        ElementTransformUtils.MoveElements(doc, (ICollection<ElementId>)ids, delta);

                        foreach (var kv in pinned)
                        {
                            var e = doc.GetElement(kv.Key);
                            if (e != null) e.Pinned = kv.Value;
                        }

                        t.Commit();
                    }
                    catch
                    {
                        t.RollBack();
                        tg.RollBack();
                        throw;
                    }
                }
                tg.Assimilate();
            }
        }
    }
}
