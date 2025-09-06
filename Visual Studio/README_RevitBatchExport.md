# Revit Batch Export Add-in

Export each saved **Selection Set** (Revit Selection Filter) from a model into its own `.rvt`, with optional origin centering and deep cleanup. Target: **Revit 2026.2**, **.NET Framework 4.8**.

> **Status:** Ribbon + command stub are wired. UI dialog and core batch logic (set isolation, recenter, purge, save, logging) are the next implementations. This README is focused for **GitHub Copilot** to complete missing parts safely.

---

## Architecture
- **GSADUs.Revit.App** — `IExternalApplication` entry; ribbon wiring (creates *GSADUs Tools → Batch Tools → Revit Batch Export* button).
- **GSADUs.Revit.Commands** — `IExternalCommand` implementations (primary: `BatchExportCommand`).
- **GSADUs.Core** — shared logic (settings, logging, IO/helpers).

```
GSADUsRevitAddin/
  src/
    GSADUs.Revit.App/
    GSADUs.Revit.Commands/
    GSADUs.Core/
  icons/    # optional
  config/   # settings.json (optional deploy)
  logs/     # optional default log folder
  exports/  # optional default output
  addin/    # .addin manifest for deployment
```

---

## Requirements
- Autodesk Revit **2026.2**
- .NET Framework **4.8**
- Visual Studio 2022
- References: `RevitAPI.dll`, `RevitAPIUI.dll` (2026)

---

## Install / Deploy
1. Build **Release** (Any CPU or x64) with Revit 2026 API references.
2. Create `addin/GSADUs.BatchExport.addin` and copy to `%AppData%/Autodesk/Revit/Addins/2026/`.
3. Point `<Assembly>` to built `GSADUs.Revit.App.dll`; set a unique `<AddInId>`; `FullClassName = GSADUs.BatchExport.App`.

---

## Settings (JSON)
Locations: next to DLLs or `%ProgramData%/GSADUs/BatchExport/settings.json`
```json
{
  "saveBeforeExport": true,
  "exportRoot": "",
  "logFolder": "",
  "limitCount": 0,
  "purgePasses": 3,
  "saveCompact": true,
  "savePreview": false
}
```
Empty `exportRoot` / `logFolder` ⇒ prompt user in UI.

---

## Intended UX (to implement)
- Launch: **GSADUs Tools → Batch Tools → Revit Batch Export**.
- **Modal WPF dialog** collects:
  - Selection Sets checklist (Select All / None)
  - Output folder (Browse)
  - Log file/folder (optional; defaults near output)
  - Options: Save/Sync before export, Detach if central, Limit N sets, Dry Run (no save), Purge passes (default 3)
- Run export → end **Summary dialog** with counts, failures, log path.

---

## Implementation Plan (for Copilot)
> Keep UI thin; move logic into **Core** functions. All Revit API mutations happen inside **Transactions**.

1. **Load/Merge Settings**
   - Read JSON defaults; overlay dialog selections.
   - If `saveBeforeExport` ⇒ save / sync active doc.
2. **Collect Selection Sets**
   - `FilteredElementCollector(doc).OfClass(SelectionFilterElement)` → `{ Name, ElementIds }`.
   - Exclude links/imports; handle zero-set case with friendly message.
   - Apply UI filter and `limitCount`.
3. **Per-Set Export Loop**
   - **Work doc**: if central, open **detached** temp copy (preserve worksets). Otherwise SaveAs to temp and reopen or use in-memory copy strategy.
   - **Isolation**: compute `allIds - setIds`; delete remainder in **batched** transactions.
   - **Recenter**: union `BoundingBoxXYZ` over remaining elements; `MoveElements` by `(-center.X, -center.Y, 0)`; keep Z.
   - **Purge**: up to `purgePasses`; prefer API purge if available; else manual unused-type deletion.
   - **SaveAs**: `<exportRoot>/<SetName>/<SetName>.rvt`; `Compact = saveCompact`; disable preview if possible; ensure directories.
   - **Log row** (CSV): `SetName, ElementCount, BBoxMin, BBoxMax, TransformXY, OutputPath, ElapsedMs, Warnings, Errors` (merge-by-SetName).
   - Close work doc.
4. **Summary**: aggregate successes/failures; show path to outputs & CSV.

---

## Current Gaps (to implement)
- [ ] **WPF dialog** for inputs (modal; launched from `BatchExportCommand`).
- [ ] **SettingsManager** (load JSON, merge with UI, persist last-used paths).
- [ ] **Selection set collector** (SelectionFilterElement → ids; exclude links/imports; empty-state UX).
- [ ] **Workshared handling** (detect central; open detached copy; no writes to central).
- [ ] **Isolation** (delete non-set elements; chunked deletes for large models).
- [ ] **Recenter** (bbox union + `ElementTransformUtils.MoveElements`).
- [ ] **Purge** (multi-pass; API or manual unused-type deletion).
- [ ] **SaveAs** (compact/no preview; directory creation; overwrite policy).
- [ ] **CSV logger** (UTF‑8, header, idempotent merge-by-SetName).
- [ ] **Summary dialog** (counts, failures, open-folder action).
- [ ] **Unit tests** for Core utilities where feasible.

---

## Implementation Notes
- Prefer **modal** dialog (no `ExternalEvent` required). If a dockable panel is introduced, marshal operations with `IExternalEventHandler`.
- All Revit API calls inside explicit **Transactions**; avoid long-running single transactions—batch where possible.
- For centrals: use `OpenOptions.DetachFromCentralOption = DetachAndPreserveWorksets` when opening copies.
- Large deletes: process in chunks; catch/continue per-set on errors.
- Paths: sanitize `SetName` for valid folder/file names.
- Logging should be robust against reruns (merge/update by `SetName`).

---

## Roadmap
- Progress UI + cancel; icons/tooltips.
- Option to open exports after save; per-set subfolder toggle.
- Future: batch views/sheets export; post-export webhook; “Lite export” via template.

---

## License / Scope
Internal GSADUs tooling for project workflows.
