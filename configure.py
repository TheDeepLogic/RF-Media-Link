#!/usr/bin/env python3
"""RetroNFC Configuration Tool"""

import json
import os
import sys
import threading
import tkinter as tk
from pathlib import Path
from tkinter import messagebox, simpledialog, ttk

try:
    import serial
    import serial.tools.list_ports
    HAS_SERIAL = True
except ImportError:
    HAS_SERIAL = False

# Find config files in AppData or current directory
def find_config_dir():
    # Always prefer AppData
    appdata = Path(os.environ.get('LOCALAPPDATA', '')) / "RetroNFC"
    if appdata.exists() and (appdata / "config.json").exists():
        return appdata
    
    # Fall back to Program Files (legacy)
    prog_files = Path("C:\\Program Files\\RetroNFC")
    if (prog_files / "config.json").exists():
        return prog_files
    
    # Fall back to current directory
    if (Path.cwd() / "config.json").exists():
        return Path.cwd()
    
    return appdata  # Default to AppData

CONFIG_DIR = find_config_dir()
CONFIG_FILE = CONFIG_DIR / "config.json"
CATALOG_FILE = CONFIG_DIR / "catalog.json"
EMULATORS_FILE = CONFIG_DIR / "emulators.json"

class ConfigureTool:
    def __init__(self, root):
        self.root = root
        self.root.title("RetroNFC Configuration Tool")
        self.root.geometry("500x400")
        
        self.config = self.load_json(CONFIG_FILE)
        self.catalog = self.load_json(CATALOG_FILE)
        self.emulators = self.load_json(EMULATORS_FILE)
        
        self.setup_menu()
        
    def setup_menu(self):
        frame = ttk.Frame(self.root, padding="20")
        frame.pack(fill=tk.BOTH, expand=True)
        
        ttk.Label(frame, text="RetroNFC Configuration", font=("Arial", 14, "bold")).pack(pady=10)
        ttk.Label(frame, text=f"Config location: {CONFIG_DIR}", font=("Arial", 9)).pack(pady=5)
        ttk.Label(frame, text=f"Serial Port: {self.config.get('serial_port', 'Not set')}", font=("Arial", 9, "italic")).pack(pady=2)
        
        ttk.Button(frame, text="Add/View Tags", command=self.manage_tags).pack(pady=5, fill=tk.X)
        ttk.Button(frame, text="Manage Emulators", command=self.manage_emulators).pack(pady=5, fill=tk.X)
        ttk.Button(frame, text="Serial Settings", command=self.serial_settings).pack(pady=5, fill=tk.X)
        ttk.Button(frame, text="Exit", command=self.root.quit).pack(pady=5, fill=tk.X)
        
    def load_json(self, path):
        if path.exists():
            with open(path) as f:
                return json.load(f)
        return {} if path.name == "emulators.json" else ([] if path.name == "catalog.json" else {})
    
    def save_json(self, path, data):
        with open(path, 'w') as f:
            json.dump(data, f, indent=2)
        messagebox.showinfo("Saved", f"Saved to {path.name}")
    
    def manage_tags(self):
        window = tk.Toplevel(self.root)
        window.title("Manage RFID Tags")
        window.geometry("600x400")
        window.grab_set()
        
        frame = ttk.Frame(window, padding="10")
        frame.pack(fill=tk.BOTH, expand=True)
        
        ttk.Label(frame, text="Catalog Tags", font=("Arial", 12, "bold")).pack(pady=5)
        
        # List frame with scrollbar
        list_frame = ttk.Frame(frame)
        list_frame.pack(fill=tk.BOTH, expand=True, pady=5)
        
        scrollbar = ttk.Scrollbar(list_frame)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        
        self.tags_listbox = tk.Listbox(list_frame, yscrollcommand=scrollbar.set)
        self.tags_listbox.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.config(command=self.tags_listbox.yview)
        
        for entry in self.catalog:
            self.tags_listbox.insert(tk.END, f"{entry.get('uid')} - {entry.get('name')}")
        
        button_frame = ttk.Frame(frame)
        button_frame.pack(fill=tk.X, pady=5)
        
        ttk.Button(button_frame, text="Add Tag", command=self.add_tag).pack(side=tk.LEFT, padx=2)
        ttk.Button(button_frame, text="Delete Selected", command=self.delete_tag).pack(side=tk.LEFT, padx=2)
        
    def add_tag(self):
        dialog = tk.Toplevel(self.root)
        dialog.title("Add RFID Tag")
        dialog.geometry("400x300")
        dialog.grab_set()
        
        ttk.Label(dialog, text="Tag UID (or scan):").grid(row=0, column=0, sticky=tk.W, padx=10, pady=5)
        uid_var = tk.StringVar()
        uid_entry = ttk.Entry(dialog, textvariable=uid_var, width=30)
        uid_entry.grid(row=0, column=1, padx=10, pady=5)
        
        if HAS_SERIAL:
            status_label = ttk.Label(dialog, text="Click 'Scan Tag' to start", foreground="blue")
            status_label.grid(row=0, column=2, padx=5, columnspan=2)
            
            def scan_tag():
                import time
                try:
                    status_label.config(text="Waiting for tag scan...", foreground="orange")
                    dialog.update()
                    
                    # Monitor last_scan.txt for new scans from the service
                    scan_file = CONFIG_DIR / "last_scan.txt"
                    
                    # Get initial timestamp
                    initial_time = scan_file.stat().st_mtime if scan_file.exists() else 0
                    
                    # Wait for file to be updated
                    timeout = 30  # 30 second timeout
                    start_time = time.time()
                    
                    while (time.time() - start_time) < timeout:
                        if scan_file.exists():
                            current_time = scan_file.stat().st_mtime
                            if current_time > initial_time:
                                # File was updated, read the UID
                                content = scan_file.read_text().strip().split('\n')
                                if content:
                                    uid = content[0].strip()
                                    uid_var.set(uid)
                                    status_label.config(text=f"Scanned: {uid}", foreground="green")
                                    return
                        
                        time.sleep(0.1)
                        dialog.update()
                    
                    status_label.config(text="Timeout - no tag scanned", foreground="red")
                except Exception as e:
                    status_label.config(text=f"Error: {e}", foreground="red")
            
            ttk.Button(dialog, text="Scan Tag", command=scan_tag).grid(row=0, column=2, padx=5)
        
        ttk.Label(dialog, text="Name:").grid(row=1, column=0, sticky=tk.W, padx=10, pady=5)
        name_entry = ttk.Entry(dialog, width=30)
        name_entry.grid(row=1, column=1, padx=10, pady=5)
        
        ttk.Label(dialog, text="Action Type:").grid(row=2, column=0, sticky=tk.W, padx=10, pady=5)
        action_var = tk.StringVar(value="emulator")
        action_combo = ttk.Combobox(dialog, textvariable=action_var, values=["emulator", "file", "url", "command"], state="readonly")
        action_combo.grid(row=2, column=1, padx=10, pady=5)
        
        ttk.Label(dialog, text="Target:").grid(row=3, column=0, sticky=tk.W, padx=10, pady=5)
        target_entry = ttk.Entry(dialog, width=30)
        target_entry.grid(row=3, column=1, padx=10, pady=5)
        
        def save_tag():
            uid = uid_var.get().strip()
            name = name_entry.get().strip()
            if not uid or not name:
                messagebox.showerror("Error", "UID and Name required")
                return
            
            entry = {
                "uid": uid,
                "name": name,
                "action_type": action_var.get(),
                "action_target": target_entry.get(),
                "config": {}
            }
            
            # Remove if exists
            self.catalog = [e for e in self.catalog if e.get("uid") != uid]
            self.catalog.append(entry)
            self.save_json(CATALOG_FILE, self.catalog)
            dialog.destroy()
        
        ttk.Button(dialog, text="Save", command=save_tag).grid(row=4, column=0, columnspan=2, pady=10, sticky=tk.EW)
    
    def delete_tag(self):
        sel = self.tags_listbox.curselection()
        if sel:
            idx = sel[0]
            self.catalog.pop(idx)
            self.save_json(CATALOG_FILE, self.catalog)
            self.tags_listbox.delete(idx)
    
    def manage_emulators(self):
        messagebox.showinfo("Emulators", "Edit emulators.json directly:\n" + str(EMULATORS_FILE))
    
    def serial_settings(self):
        dialog = simpledialog.askstring("Serial Port", "Enter serial port (e.g., COM9):", initialvalue=self.config.get("serial_port", "COM9"))
        if dialog:
            self.config["serial_port"] = dialog
            self.save_json(CONFIG_FILE, self.config)

if __name__ == "__main__":
    root = tk.Tk()
    app = ConfigureTool(root)
    root.mainloop()
