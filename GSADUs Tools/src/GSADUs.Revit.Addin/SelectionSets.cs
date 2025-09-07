using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace GSADUs.Revit.Addin
{
  public static class SelectionSets
  {
    public static IReadOnlyList<(string Name, ICollection<ElementId> Ids)> Get(Document doc) =>
      new FilteredElementCollector(doc)
        .OfClass(typeof(SelectionFilterElement))
        .Cast<SelectionFilterElement>()
        .Select(s => (s.Name, s.GetElementIds()))
        .OrderBy(t => t.Name)
        .ToList();
  }
}
