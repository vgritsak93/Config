import os
import shutil
import csv
from datetime import datetime

def ensure_folder(path):
    if not os.path.exists(path):
        os.makedirs(path)


def ensure_template_structure(model_group_folder):
    template_folder = r"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Config\Templates\A000-M0 Template"
    if not os.path.exists(model_group_folder):
        shutil.copytree(template_folder, model_group_folder)

# --- CONFIGURABLE INPUTS ---
source_folder = r"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Output"
target_root = r"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\3 - Models"
log_file = r"G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Config\CSV Logs\batch_move_log.csv"

HEADER = ["Key", "Filename", "Source", "Destination", "Timestamp"]

def ensure_folder(path):
def ensure_template_structure(model_group_folder):
    # Template subfolders to enforce
    subfolders = ["Archive", "csv", "pdf", "png", "rvt"]
    for sub in subfolders:
        ensure_folder(os.path.join(model_group_folder, sub))
    if not os.path.exists(path):
        os.makedirs(path)

def truncate_timestamp(ts):
    return ts.split('.')[0] if '.' in ts else ts

def read_log():
    log_dict = {}
    if os.path.exists(log_file):
        with open(log_file, 'r', newline='') as csvfile:
            reader = csv.DictReader(csvfile)
            for row in reader:
                log_dict[row['Key']] = row
    return log_dict

def write_log(log_dict):
    with open(log_file, 'w', newline='') as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=HEADER)
        writer.writeheader()
        # Sort keys alphabetically for easier reference
        for key in sorted(log_dict.keys()):
            writer.writerow(log_dict[key])

def move_and_upsert(filename, log_dict):
    key = os.path.splitext(filename)[0]
    model_group_folder = os.path.join(target_root, key)
    ensure_template_structure(model_group_folder)
    target_folder = os.path.join(model_group_folder, "rvt")
    ensure_folder(target_folder)
    src_path = os.path.join(source_folder, filename)
    dst_path = os.path.join(target_folder, filename)
    # Move and overwrite if exists
    if os.path.exists(dst_path):
        os.remove(dst_path)
    shutil.move(src_path, dst_path)
    timestamp = truncate_timestamp(datetime.now().isoformat())
    # Upsert log entry
    log_dict[key] = {
        "Key": key,
        "Filename": filename,
        "Source": src_path,
        "Destination": dst_path,
        "Timestamp": timestamp
    }
    print("Moved {} to {}".format(filename, target_folder))

def main():
    # Read existing log
    log_dict = read_log()
    # Process all RVT files
    for filename in os.listdir(source_folder):
        if filename.lower().endswith(".rvt"):
            move_and_upsert(filename, log_dict)
    # Write updated log
    write_log(log_dict)
    # Open the CSV log for review
    os.startfile(log_file)

if __name__ == "__main__":
    main()
