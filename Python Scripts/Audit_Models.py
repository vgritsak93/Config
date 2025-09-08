import os
import csv
from datetime import datetime

target_root = r"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\3 - Models"
template_folder = r"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Config\Templates\A000-M0 Template"
report_file = r"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Config\CSV Logs\audit_models_log.csv"

HEADER = ["Key", "MissingFolders", "ExtraFolders", "Timestamp"]

def get_subfolders(folder):
    return set([name for name in os.listdir(folder)
                if os.path.isdir(os.path.join(folder, name))])

def ensure_folder(path):
    if not os.path.exists(path):
        os.makedirs(path)

def audit_model_group(model_group_folder, template_subfolders):
    subfolders = get_subfolders(model_group_folder)
    missing = template_subfolders - subfolders
    extra = subfolders - template_subfolders
    # Add missing folders
    for mf in missing:
        ensure_folder(os.path.join(model_group_folder, mf))
    # Delete extra folders if empty
    for ef in extra:
        ef_path = os.path.join(model_group_folder, ef)
        if os.path.isdir(ef_path) and not os.listdir(ef_path):
            os.rmdir(ef_path)
    # Case consistency: Rename 'RVT' to 'rvt' via 'rvt_' if needed
    rvt_path = os.path.join(model_group_folder, 'RVT')
    rvt_lower_path = os.path.join(model_group_folder, 'rvt')
    rvt_temp_path = os.path.join(model_group_folder, 'rvt_')
    if os.path.exists(rvt_path):
        # If 'rvt' already exists, remove it to avoid conflict
        if os.path.exists(rvt_lower_path):
            try:
                if os.path.isdir(rvt_lower_path):
                    os.rmdir(rvt_lower_path)
                else:
                    os.remove(rvt_lower_path)
            except Exception as e:
                print(f"Error removing existing 'rvt': {e}")
        # Rename 'RVT' to 'rvt_'
        try:
            os.rename(rvt_path, rvt_temp_path)
        except Exception as e:
            print(f"Error renaming 'RVT' to 'rvt_': {e}")
        # Rename 'rvt_' to 'rvt'
        try:
            os.rename(rvt_temp_path, rvt_lower_path)
        except Exception as e:
            print(f"Error renaming 'rvt_' to 'rvt': {e}")
    return list(missing), list(extra)

def main():
    template_subfolders = get_subfolders(template_folder)
    log_dict = {}
    for mg_name in os.listdir(target_root):
        mg_path = os.path.join(target_root, mg_name)
        if not os.path.isdir(mg_path):
            continue
        if mg_name == "A000-M0 Template":
            continue
        missing, extra = audit_model_group(mg_path, template_subfolders)
        timestamp = datetime.now().isoformat().split('.')[0]
        log_dict[mg_name] = {
            "Key": mg_name,
            "MissingFolders": ", ".join(missing) if missing else "",
            "ExtraFolders": ", ".join(extra) if extra else "",
            "Timestamp": timestamp
        }
    # Write CSV report, sorted by Key
    with open(report_file, 'w', newline='') as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=HEADER)
        writer.writeheader()
        for key in sorted(log_dict.keys()):
            writer.writerow(log_dict[key])
    print("Audit complete. Report saved to:", report_file)
    # Open the CSV log for review
    os.startfile(report_file)

if __name__ == "__main__":
    main()
