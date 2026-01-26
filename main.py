import serial
import subprocess
import time
import os
import webbrowser
import pyautogui
import json
import tkinter as tk
from tkinter import filedialog, simpledialog, messagebox
from datetime import datetime
from typing import Optional
import threading
import sys
import msvcrt
import psutil

# ---- Console colors ----
COLOR_RESET = "\033[0m"
COLOR_GREEN = "\033[92m"  # success/launch
COLOR_YELLOW = "\033[93m"  # add/catalog actions
COLOR_CYAN = "\033[96m"   # serial input
COLOR_RED = "\033[91m"    # errors/unknown

def color_text(text: str, color: str) -> str:
    if not sys.stdout.isatty():
        return text
    return f"{color}{text}{COLOR_RESET}"

PRODUCT_NAME = "RetroNFC"
BAUD = 115200
CATALOG_FILE = os.path.join(os.path.dirname(__file__), "catalog.json")
CONFIG_FILE = os.path.join(os.path.dirname(__file__), "config.json")
EMULATORS_FILE = os.path.join(os.path.dirname(__file__), "emulators.json")


# ---- Catalog Management ----
def load_catalog() -> dict:
    """Load UID→file mappings from catalog.json"""
    if os.path.exists(CATALOG_FILE):
        try:
            with open(CATALOG_FILE, 'r') as f:
                return json.load(f)
        except Exception as e:
            print(f"Error loading catalog: {e}")
    return {}


def save_catalog(catalog: dict):
    """Save UID→file mappings to catalog.json"""
    try:
        with open(CATALOG_FILE, 'w') as f:
            json.dump(catalog, f, indent=2)
        print(color_text(f"Catalog saved to {CATALOG_FILE}", COLOR_YELLOW))
    except Exception as e:
        print(f"Error saving catalog: {e}")


def load_config() -> dict:
    """Load user preferences (serial port, last path used, etc.)"""
    if os.path.exists(CONFIG_FILE):
        try:
            with open(CONFIG_FILE, 'r') as f:
                return json.load(f)
        except Exception as e:
            print(f"Error loading preferences: {e}")
    return {
        "serial_port": "COM9",
        "serial_baud": 115200,
        "last_browse_path": os.path.expanduser("~"),
        "last_command_type": "file",
        "last_mode": "single"
    }


def save_config(config: dict):
    """Save user preferences"""
    try:
        with open(CONFIG_FILE, 'w') as f:
            json.dump(config, f, indent=2)
    except Exception as e:
        print(f"Error saving preferences: {e}")


def load_emulators() -> dict:
    """Load emulator definitions from emulators.json"""
    if os.path.exists(EMULATORS_FILE):
        try:
            with open(EMULATORS_FILE, 'r') as f:
                return json.load(f)
        except Exception as e:
            print(f"Error loading emulators: {e}")
    return {}


def save_emulators(emulators: dict):
    """Save emulator definitions to emulators.json"""
    try:
        with open(EMULATORS_FILE, 'w') as f:
            json.dump(emulators, f, indent=2)
        print(color_text(f"Emulators saved to {EMULATORS_FILE}", COLOR_YELLOW))
    except Exception as e:
        print(f"Error saving emulators: {e}")


# ---- Emulator Process Management ----
def get_emulator_executable_name(executable_path: str) -> str:
    """Extract just the filename from a full executable path"""
    return os.path.basename(executable_path).lower()


def find_running_emulator_by_executable(executable_path: str) -> list:
    """Find all running processes matching the given executable path
    
    Returns:
        List of Process objects matching the executable
    """
    exe_name = get_emulator_executable_name(executable_path)
    running_processes = []
    
    try:
        for proc in psutil.process_iter(['pid', 'name']):
            try:
                if proc.info['name'].lower() == exe_name:
                    running_processes.append(proc)
            except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
                pass
    except Exception as e:
        print(f"Error scanning processes: {e}")
    
    return running_processes


def terminate_emulator_instances(executable_path: str, max_instances: int = 0) -> int:
    """Terminate running instances of a specific emulator
    
    Args:
        executable_path: Full path to the emulator executable
        max_instances: If > 0, only terminate if count exceeds this number
    
    Returns:
        Number of processes terminated
    """
    processes = find_running_emulator_by_executable(executable_path)
    
    if max_instances > 0 and len(processes) <= max_instances:
        return 0
    
    count = 0
    for proc in processes:
        try:
            exe_name = get_emulator_executable_name(executable_path)
            proc.terminate()
            try:
                proc.wait(timeout=3)
            except psutil.TimeoutExpired:
                proc.kill()
                proc.wait(timeout=3)
            count += 1
            print(color_text(f"  Terminated {exe_name} (PID {proc.pid})", COLOR_YELLOW))
        except Exception as e:
            print(f"  Warning: Could not terminate process {proc.pid}: {e}")
    
    return count


def terminate_other_emulators(emulator_id: str, emulators: dict) -> int:
    """Terminate all emulators except the specified one
    
    Args:
        emulator_id: The ID of the emulator to keep running
        emulators: The emulators dictionary
    
    Returns:
        Total number of processes terminated
    """
    total_terminated = 0
    
    for other_id, other_def in emulators.items():
        if other_id == emulator_id:
            continue
        
        executable = other_def.get("executable", "")
        if executable and os.path.exists(executable):
            terminated = terminate_emulator_instances(executable)
            total_terminated += terminated
    
    return total_terminated


def ask_add_to_catalog(uid: str) -> bool:
    """GUI yes/no dialog to add an unknown UID to catalog."""
    root = tk.Tk()
    root.withdraw()
    root.attributes('-topmost', True)
    root.update_idletasks()
    root.lift()
    root.focus_force()
    result = messagebox.askyesno(
        title=f"{PRODUCT_NAME} - Unknown RFID Tag",
        message=f"Unknown UID: {uid}\n\nAdd to catalog?",
        parent=root
    )
    root.destroy()
    return bool(result)


def pick_file(config: dict) -> Optional[str]:
    """Show file picker dialog, remember the last used path"""
    root = tk.Tk()
    root.withdraw()  # Hide the root window
    root.attributes('-topmost', True)
    root.lift()
    root.focus_force()
    center_window(root)
    
    initial_dir = config.get("last_browse_path", os.path.expanduser("~"))
    if not os.path.isdir(initial_dir):
        initial_dir = os.path.expanduser("~")
    
    file_path = filedialog.askopenfilename(
        initialdir=initial_dir,
        title=f"{PRODUCT_NAME} - Select File"
    )
    
    root.destroy()
    
    if file_path:
        # Remember the directory
        config["last_browse_path"] = os.path.dirname(file_path)
        save_config(config)
        return file_path
    
    return None




def add_tag_from_uid(uid: str, catalog: dict, config: dict, command_type: str, emulators: dict = None) -> bool:
    """
    Add a tag that was just scanned with its UID.
    
    Args:
        uid: The UID that was scanned
        catalog: The catalog dict
        config: The config dict
        command_type: Type of action ('emulator', 'file', 'url', 'command', 'hotkey', 'shell')
        emulators: Emulator definitions dict (required if command_type is 'emulator')
    
    Returns:
        True if added successfully, False if cancelled
    """
    if uid in catalog:
        print(f"  (Already mapped to: {catalog[uid]})")
        response = input("  Overwrite? [y/N]: ").strip().lower()
        if response != 'y':
            return False
    
    # Get the appropriate input based on command_type
    action = {}
    
    if command_type == "emulator":
        if not emulators:
            print("No emulators configured.")
            return False
        
        emu_id = prompt_select_emulator(emulators)
        if not emu_id:
            print("Cancelled.")
            return False
        
        emu_def = emulators[emu_id]
        emu_config = prompt_emulator_config(emu_id, emu_def, config)
        if emu_config is None:
            print("Cancelled.")
            return False
        
        action = {"emulator": emu_id, "config": emu_config}
    
    elif command_type == "file":
        file_path = pick_file(config)
        if not file_path:
            print("Cancelled.")
            return False
        action = {"file": file_path}
    
    elif command_type == "url":
        root = tk.Tk()
        root.withdraw()
        root.attributes('-topmost', True)
        root.update_idletasks()
        root.lift()
        root.focus_force()
        center_window(root)
        url = simpledialog.askstring(f"{PRODUCT_NAME} - Enter URL", "URL:", parent=root)
        root.destroy()
        if not url:
            print("Cancelled.")
            return False
        action = {"url": url}
    
    elif command_type == "command":
        root = tk.Tk()
        root.withdraw()
        root.attributes('-topmost', True)
        root.update_idletasks()
        root.lift()
        root.focus_force()
        center_window(root)
        cmd = simpledialog.askstring(f"{PRODUCT_NAME} - Enter Command", "Command name (e.g., close_window, screenshot):", parent=root)
        root.destroy()
        if not cmd:
            print("Cancelled.")
            return False
        action = {"command": cmd}
    
    elif command_type == "hotkey":
        root = tk.Tk()
        root.withdraw()
        root.attributes('-topmost', True)
        root.update_idletasks()
        root.lift()
        root.focus_force()
        center_window(root)
        keys_str = simpledialog.askstring(f"{PRODUCT_NAME} - Enter Hotkey", "Keys comma-separated (e.g., alt,f4):", parent=root)
        root.destroy()
        if not keys_str:
            print("Cancelled.")
            return False
        keys = [k.strip().lower() for k in keys_str.split(",")]
        action = {"hotkey": keys}
    
    elif command_type == "shell":
        root = tk.Tk()
        root.withdraw()
        root.attributes('-topmost', True)
        root.update_idletasks()
        root.lift()
        root.focus_force()
        center_window(root)
        shell_cmd = simpledialog.askstring(f"{PRODUCT_NAME} - Enter Shell Command", "Shell command (e.g., notepad.exe):", parent=root)
        root.destroy()
        if not shell_cmd:
            print("Cancelled.")
            return False
        action = {"shell": shell_cmd.split()}
    
    # Save to catalog
    catalog[uid] = action
    save_catalog(catalog)
    print(color_text(f"✓ Saved: {uid} → {action}", COLOR_YELLOW))
    return True


def add_tag_interactive(ser, catalog: dict, config: dict, emulators: dict, command_type: str, is_batch: bool = False):

    """
    Interactively add a new tag:
    1. Wait for tag scan
    2. Based on command_type, get the necessary input (file, URL, emulator, etc.)
    3. Save to catalog
    
    Args:
        ser: Serial port object
        catalog: The catalog dict
        config: The config dict
        emulators: The emulators dict
        command_type: Type of action ('emulator', 'file', 'url', 'command', 'hotkey', 'shell')
        is_batch: If True, return True to continue batch adding
    """
    print(f"\n=== ADD NEW TAG ({command_type}) ===")
    
    # For emulator type, prompt for emulator selection BEFORE scanning
    selected_emulator_id = None
    if command_type == "emulator":
        try:
            selected_emulator_id = prompt_select_emulator(emulators)
            if not selected_emulator_id:
                print("Cancelled.")
                return is_batch
        except Exception as e:
            print(f"Error selecting emulator: {e}")
            import traceback
            traceback.print_exc()
            return is_batch
    
    print("Ready to scan... (waiting for UID)")
    
    uid = None
    while not uid:
        if ser.in_waiting:
            line = ser.readline().decode(errors="ignore").strip()
            if line.startswith("UID:"):
                uid = line.replace("UID:", "").strip()
                print(f"Got UID: {uid}")
                
                if uid in catalog:
                    print(f"  (Already mapped to: {catalog[uid]})")
                    response = input("  Overwrite? [y/N]: ").strip().lower()
                    if response != 'y':
                        return is_batch  # Return whether to continue batch mode
                break
        time.sleep(0.05)
    
    # Get the appropriate input based on command_type
    action = {}
    
    if command_type == "emulator":
        # Configure emulator arguments for this specific tag
        try:
            emulator_def = emulators[selected_emulator_id]
            emulator_config = prompt_emulator_config(selected_emulator_id, emulator_def, config)
            if not emulator_config:
                print("Cancelled.")
                return is_batch
            action = {"emulator": selected_emulator_id, "config": emulator_config}
        except Exception as e:
            print(f"Error configuring emulator: {e}")
            import traceback
            traceback.print_exc()
            return is_batch
    
    elif command_type == "file":
        file_path = pick_file(config)
        if not file_path:
            print("Cancelled.")
            return is_batch
        action = {"file": file_path}
    
    elif command_type == "url":
        root = tk.Tk()
        root.withdraw()
        center_window(root)
        url = simpledialog.askstring(f"{PRODUCT_NAME} - Enter URL", "URL:")
        root.destroy()
        if not url:
            print("Cancelled.")
            return is_batch
        action = {"url": url}
    
    elif command_type == "command":
        root = tk.Tk()
        root.withdraw()
        center_window(root)
        cmd = simpledialog.askstring(f"{PRODUCT_NAME} - Enter Command", "Command name (e.g., close_window, screenshot):")
        root.destroy()
        if not cmd:
            print("Cancelled.")
            return is_batch
        action = {"command": cmd}
    
    elif command_type == "hotkey":
        root = tk.Tk()
        root.withdraw()
        center_window(root)
        keys_str = simpledialog.askstring(f"{PRODUCT_NAME} - Enter Hotkey", "Keys comma-separated (e.g., alt,f4):")
        root.destroy()
        if not keys_str:
            print("Cancelled.")
            return is_batch
        keys = [k.strip().lower() for k in keys_str.split(",")]
        action = {"hotkey": keys}
    
    elif command_type == "shell":
        root = tk.Tk()
        root.withdraw()
        center_window(root)
        shell_cmd = simpledialog.askstring(f"{PRODUCT_NAME} - Enter Shell Command", "Shell command (e.g., notepad.exe):")
        root.destroy()
        if not shell_cmd:
            print("Cancelled.")
            return is_batch
        action = {"shell": shell_cmd.split()}
    
    # Save to catalog
    catalog[uid] = action
    save_catalog(catalog)
    print(color_text(f"✓ Saved: {uid} → {action}", COLOR_YELLOW))
    return is_batch  # Return whether to continue batch mode



# ---- Built-in command handlers (assignable by id) ----
def close_active_window():
    pyautogui.hotkey("alt", "f4")


def switch_window():
    pyautogui.hotkey("alt", "tab")


def show_desktop():
    pyautogui.hotkey("win", "d")


def lock_screen():
    pyautogui.hotkey("win", "l")


def refresh_page():
    pyautogui.hotkey("ctrl", "shift", "r")


def open_incognito():
    pyautogui.hotkey("ctrl", "shift", "n")


def take_screenshot():
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    filename = f"screenshot-{stamp}.png"
    path = os.path.join(os.getcwd(), filename)
    pyautogui.screenshot(path)
    print(f"Saved screenshot to {path}")


# Commands are plain functions so you can map an RFID straight to a callable name.
COMMANDS = {
    "close_window": close_active_window,
    "switch_window": switch_window,
    "show_desktop": show_desktop,
    "lock_screen": lock_screen,
    "refresh_page": refresh_page,
    "open_incognito": open_incognito,
    "screenshot": take_screenshot,
}


def is_url(target: str) -> bool:
    return target.startswith("http://") or target.startswith("https://")


# ---- RFID Actions ----
# Each UID maps to a lightweight action descriptor. Supported shapes:
#   {"command": "close_window"}          -> runs a named COMMANDS function
#   {"hotkey": ["alt", "tab"]}              -> presses a hotkey combo
#   {"url": "https://..."}                  -> opens a URL
#   {"file": r"C:\\Path\\to\\file.exe"}    -> launches a file
#   {"shell": ["notepad.exe", "notes.txt"]} -> runs a shell command with args
#   "https://..." or r"C:\\Path"              -> also works (legacy shorthand)
ACTIONS = {}  # Loaded from catalog.json at runtime

def execute_action(action, emulators: dict = None):
    try:
        # Legacy shorthand: plain string
        if isinstance(action, str):
            if action in COMMANDS:
                print(f"Running command: {action}")
                COMMANDS[action]()
            elif is_url(action):
                print(f"Opening URL: {action}")
                webbrowser.open(action)
            else:
                print(f"Launching file: {action}")
                os.startfile(action)
            return

        if not isinstance(action, dict):
            print(f"Unsupported action spec: {action}")
            return

        # New: Emulator execution
        if "emulator" in action:
            emu_id = action["emulator"]
            emu_config = action.get("config", {})
            
            if not emulators or emu_id not in emulators:
                print(f"Unknown emulator: {emu_id}")
                return
            
            emu_def = emulators[emu_id]
            executable = emu_def.get("executable", "")
            
            if not os.path.exists(executable):
                print(f"Emulator executable not found: {executable}")
                return
            
            # Handle close_on_launch setting
            close_on_launch = emu_def.get("close_on_launch", "others")
            
            if close_on_launch == "same":
                # Close any existing instances of THIS emulator
                print(f"Closing existing instances of {emu_def.get('name', emu_id)}...")
                terminate_emulator_instances(executable)
            elif close_on_launch == "others":
                # Close all OTHER emulators
                print(f"Closing other emulator instances...")
                terminate_other_emulators(emu_id, emulators)
            # If close_on_launch == "none", don't close anything
            
            # Build command line arguments
            executable_quoted = f'"{executable}"'  # Always quote executable
            cmd_parts = [executable_quoted]
            arguments = emu_def.get("arguments", [])
            
            for arg_def in arguments:
                arg_name = arg_def["name"]
                arg_value = emu_config.get(arg_name)
                
                if arg_value is None:
                    continue
                
                arg_type = arg_def["type"]
                flag = arg_def.get("flag", "")
                
                if arg_type == "toggle":
                    # Toggle is boolean - only add flag if True
                    if arg_value:
                        cmd_parts.append(flag)
                elif flag:
                    # Has flag - add flag then value
                    cmd_parts.append(flag)
                    # Always quote file paths and text values
                    cmd_parts.append(f'"{arg_value}"')
                else:
                    # No flag - positional argument
                    # Always quote file paths and text values
                    cmd_parts.append(f'"{arg_value}"')
            
            cmd_string = ' '.join(cmd_parts)
            
            print(color_text(f"Launching {emu_def.get('name', emu_id)}: {cmd_string}", COLOR_GREEN))
            
            # Use shell=True on Windows for proper command execution
            subprocess.Popen(cmd_string, shell=True)
            return

        if "command" in action:
            name = action["command"]
            func = COMMANDS.get(name)
            if func:
                print(f"Running command: {name}")
                func()
            else:
                print(f"Unknown command: {name}")
            return

        if "hotkey" in action:
            keys = action["hotkey"]
            print(f"Pressing hotkey: {keys}")
            pyautogui.hotkey(*keys)
            return

        if "url" in action:
            url = action["url"]
            print(f"Opening URL: {url}")
            webbrowser.open(url)
            return

        if "file" in action:
            target = action["file"]
            print(f"Launching file: {target}")
            os.startfile(target)
            return

        if "shell" in action:
            cmd = action["shell"]
            print(f"Running shell: {cmd}")
            subprocess.Popen(cmd)
            return

        print(f"Unsupported action spec: {action}")

    except Exception as e:
        print(f"Error executing action {action}: {e}")


def has_keyboard_input() -> bool:
    """Check if there's keyboard input waiting (Windows-safe)"""
    try:
        return msvcrt.kbhit()
    except Exception:
        return False


# No drain helpers (live serial stream)


# Global variable for keyboard input from thread
keyboard_input_queue = []
input_thread_stop = False

def keyboard_input_thread():
    """Background thread to read keyboard input non-blockingly"""
    global keyboard_input_queue, input_thread_stop
    try:
        while not input_thread_stop:
            if has_keyboard_input():
                try:
                    cmd = input().strip().lower()
                    keyboard_input_queue.append(cmd)
                except EOFError:
                    break
            time.sleep(0.05)
    except Exception as e:
        print(f"Keyboard input thread error: {e}")


def get_keyboard_input() -> Optional[str]:
    """Get a keyboard input if available (non-blocking)"""
    global keyboard_input_queue
    if keyboard_input_queue:
        return keyboard_input_queue.pop(0)
    return None


def center_window(window):
    """Center a tkinter window on the screen"""
    window.update_idletasks()
    x = (window.winfo_screenwidth() // 2) - (window.winfo_width() // 2)
    y = (window.winfo_screenheight() // 2) - (window.winfo_height() // 2)
    window.geometry(f"+{x}+{y}")


def prompt_select_emulator(emulators: dict) -> Optional[str]:
    """Show dialog to select an emulator from the available emulators"""
    if not emulators:
        messagebox.showwarning(f"{PRODUCT_NAME} - No Emulators", "No emulators configured. Press 'e' to add an emulator first.")
        return None
    
    dialog = tk.Tk()
    dialog.title(f"{PRODUCT_NAME} - Select Emulator")
    dialog.resizable(False, False)
    dialog.attributes('-topmost', True)
    
    emu_list = list(emulators.keys())
    selected_value = tk.StringVar(value=emu_list[0] if emu_list else "")
    
    label = tk.Label(dialog, text="Select emulator:", font=("Arial", 10, "bold"))
    label.pack(pady=10)
    
    for emu_id in emu_list:
        emu_name = emulators[emu_id].get("name", emu_id)
        rb = tk.Radiobutton(dialog, text=emu_name, variable=selected_value, value=emu_id, font=("Arial", 10))
        rb.pack(anchor=tk.W, padx=20, pady=5)
    
    def ok_clicked():
        dialog.result = selected_value.get()
        dialog.destroy()
    
    def cancel_clicked():
        dialog.result = None
        dialog.destroy()
    
    button_frame = tk.Frame(dialog)
    button_frame.pack(pady=15)
    
    ok_btn = tk.Button(button_frame, text="OK", command=ok_clicked, width=10)
    ok_btn.pack(side=tk.LEFT, padx=5)
    
    cancel_btn = tk.Button(button_frame, text="Cancel", command=cancel_clicked, width=10)
    cancel_btn.pack(side=tk.LEFT, padx=5)
    
    dialog.bind('<Return>', lambda e: ok_clicked())
    dialog.bind('<Escape>', lambda e: cancel_clicked())
    
    dialog.result = None
    dialog.update()
    height = min(150 + len(emu_list) * 35, 500)
    dialog.geometry(f"350x{height}")
    
    screen_width = dialog.winfo_screenwidth()
    screen_height = dialog.winfo_screenheight()
    x = (screen_width // 2) - 175
    y = (screen_height // 2) - (height // 2)
    dialog.geometry(f"350x{height}+{x}+{y}")
    
    dialog.focus_force()
    dialog.wait_window()
    
    return dialog.result


def prompt_emulator_config(emulator_id: str, emulator_def: dict, config: dict) -> Optional[dict]:
    """Show dialog to configure emulator arguments"""
    dialog = tk.Tk()
    dialog.title(f"{PRODUCT_NAME} - Configure {emulator_def.get('name', emulator_id)}")
    dialog.resizable(False, True)
    dialog.attributes('-topmost', True)
    
    # Create main container frame
    main_frame = tk.Frame(dialog)
    main_frame.pack(fill=tk.BOTH, expand=True)
    
    # Create scrollable frame with FIXED height
    canvas = tk.Canvas(main_frame, width=500, height=500)
    scrollbar = tk.Scrollbar(main_frame, orient="vertical", command=canvas.yview)
    scrollable_frame = tk.Frame(canvas)
    
    scrollable_frame.bind(
        "<Configure>",
        lambda e: canvas.configure(scrollregion=canvas.bbox("all"))
    )
    
    canvas.create_window((0, 0), window=scrollable_frame, anchor="nw")
    canvas.configure(yscrollcommand=scrollbar.set)
    
    # Store widget values
    arg_widgets = {}
    
    arguments = emulator_def.get("arguments", [])
    for arg in arguments:
        arg_name = arg["name"]
        arg_type = arg["type"]
        label_text = arg.get("label", arg_name)
        required = arg.get("required", False)
        
        frame = tk.Frame(scrollable_frame)
        frame.pack(fill=tk.X, padx=10, pady=5)
        
        lbl = tk.Label(frame, text=f"{label_text}{'*' if required else ''}:", font=("Arial", 9))
        lbl.pack(anchor=tk.W)
        
        if arg_type == "file":
            file_frame = tk.Frame(frame)
            file_frame.pack(fill=tk.X)
            
            entry = tk.Entry(file_frame, width=50)
            entry.pack(side=tk.LEFT, padx=(0, 5))
            if arg.get("default"):
                entry.insert(0, arg["default"])
            
            def browse_file(e=entry):
                path = filedialog.askopenfilename(initialdir=config.get("last_browse_path", "~"))
                if path:
                    e.delete(0, tk.END)
                    e.insert(0, path)
                    config["last_browse_path"] = os.path.dirname(path)
                    save_config(config)
            
            browse_btn = tk.Button(file_frame, text="Browse...", command=browse_file)
            browse_btn.pack(side=tk.LEFT)
            
            arg_widgets[arg_name] = ("entry", entry)
            
        elif arg_type == "text":
            entry = tk.Entry(frame, width=50)
            entry.pack(fill=tk.X)
            if arg.get("default"):
                entry.insert(0, arg["default"])
            arg_widgets[arg_name] = ("entry", entry)
            
        elif arg_type == "choice":
            choices = arg.get("choices", [])
            default = arg.get("default", choices[0] if choices else "")
            
            var = tk.StringVar(value=default)
            dropdown = tk.OptionMenu(frame, var, *choices)
            dropdown.pack(fill=tk.X)
            arg_widgets[arg_name] = ("choice", var)
            
        elif arg_type == "toggle":
            default = arg.get("default", False)
            var = tk.BooleanVar(value=default)
            check = tk.Checkbutton(frame, variable=var)
            check.pack(anchor=tk.W)
            arg_widgets[arg_name] = ("toggle", var)
    
    canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
    scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
    
    def ok_clicked():
        result = {}
        for arg_name, (widget_type, widget) in arg_widgets.items():
            if widget_type == "entry":
                value = widget.get().strip()
                if value:
                    result[arg_name] = value
            elif widget_type == "choice":
                result[arg_name] = widget.get()
            elif widget_type == "toggle":
                if widget.get():  # Only include if True
                    result[arg_name] = True
        
        dialog.result = result
        dialog.destroy()
    
    def cancel_clicked():
        dialog.result = None
        dialog.destroy()
    
    button_frame = tk.Frame(dialog)
    button_frame.pack(side=tk.BOTTOM, fill=tk.X, pady=10, padx=10)
    
    ok_btn = tk.Button(button_frame, text="OK", command=ok_clicked, width=12)
    ok_btn.pack(side=tk.LEFT, padx=5)
    
    cancel_btn = tk.Button(button_frame, text="Cancel", command=cancel_clicked, width=12)
    cancel_btn.pack(side=tk.LEFT, padx=5)
    
    dialog.bind('<Return>', lambda e: ok_clicked())
    dialog.bind('<Escape>', lambda e: cancel_clicked())
    
    dialog.result = None
    dialog.geometry("550x700")
    
    screen_width = dialog.winfo_screenwidth()
    screen_height = dialog.winfo_screenheight()
    x = (screen_width // 2) - 275
    y = (screen_height // 2) - 350
    dialog.geometry(f"550x700+{x}+{y}")
    
    dialog.focus_force()
    dialog.wait_window()
    
    return dialog.result


def prompt_command_type(config: dict) -> Optional[str]:
    """Show radio button dialog to select command type"""
    # Create dialog window directly without parent root
    dialog = tk.Tk()
    dialog.title(f"{PRODUCT_NAME} - Select Command Type")
    dialog.resizable(False, False)
    dialog.attributes('-topmost', True)
    
    # Load last selection from config
    last_type = config.get("last_command_type", "file")
    selected_value = tk.StringVar(value=last_type)
    
    options = [
        ("emulator", "Emulator"),
        ("file", "File"),
        ("url", "URL"),
        ("command", "Command"),
        ("hotkey", "Hotkey"),
        ("shell", "Shell Command")
    ]
    
    label = tk.Label(dialog, text="Select command type:", font=("Arial", 10, "bold"))
    label.pack(pady=10)
    
    radio_buttons = []
    for value, label_text in options:
        rb = tk.Radiobutton(dialog, text=label_text, variable=selected_value, value=value, font=("Arial", 10))
        rb.pack(anchor=tk.W, padx=20, pady=5)
        radio_buttons.append((value, rb))
    
    def ok_clicked():
        dialog.result = selected_value.get()
        # Save selection to config
        config["last_command_type"] = dialog.result
        save_config(config)
        dialog.destroy()
    
    def cancel_clicked():
        dialog.result = None
        dialog.destroy()
    
    def on_arrow_up(event):
        current = selected_value.get()
        current_idx = next((i for i, (val, _) in enumerate(options) if val == current), 0)
        new_idx = (current_idx - 1) % len(options)
        selected_value.set(options[new_idx][0])
        return "break"
    
    def on_arrow_down(event):
        current = selected_value.get()
        current_idx = next((i for i, (val, _) in enumerate(options) if val == current), 0)
        new_idx = (current_idx + 1) % len(options)
        selected_value.set(options[new_idx][0])
        return "break"
    
    button_frame = tk.Frame(dialog)
    button_frame.pack(pady=15)
    
    ok_btn = tk.Button(button_frame, text="OK", command=ok_clicked, width=10)
    ok_btn.pack(side=tk.LEFT, padx=5)
    
    cancel_btn = tk.Button(button_frame, text="Cancel", command=cancel_clicked, width=10)
    cancel_btn.pack(side=tk.LEFT, padx=5)
    
    dialog.bind('<Up>', on_arrow_up)
    dialog.bind('<Down>', on_arrow_down)
    dialog.bind('<Return>', lambda e: ok_clicked())
    dialog.bind('<Escape>', lambda e: cancel_clicked())
    
    dialog.result = None
    dialog.update()
    dialog.geometry("300x320")
    
    # Center on screen
    screen_width = dialog.winfo_screenwidth()
    screen_height = dialog.winfo_screenheight()
    x = (screen_width // 2) - 150
    y = (screen_height // 2) - 160
    dialog.geometry(f"300x320+{x}+{y}")
    
    dialog.focus_force()
    dialog.wait_window()
    
    return dialog.result


def prompt_mode(config: dict) -> Optional[tuple[str, bool]]:
    """Prompt user to select single or batch mode. Returns (mode, is_batch) or None if cancelled"""
    # Create dialog window directly without parent root
    dialog = tk.Tk()
    dialog.title(f"{PRODUCT_NAME} - Select Mode")
    dialog.resizable(False, False)
    dialog.attributes('-topmost', True)
    
    # Load last selection from config
    last_mode = config.get("last_mode", "single")
    selected_value = tk.StringVar(value=last_mode)
    
    label = tk.Label(dialog, text="Select mode:", font=("Arial", 10, "bold"))
    label.pack(pady=10)
    
    rb_single = tk.Radiobutton(dialog, text="Single (add one tag)", variable=selected_value, value="single", font=("Arial", 10), wraplength=350)
    rb_single.pack(anchor=tk.W, padx=20, pady=5)
    
    rb_batch = tk.Radiobutton(dialog, text="Batch (add multiple tags with same command type)", variable=selected_value, value="batch", font=("Arial", 10), wraplength=350)
    rb_batch.pack(anchor=tk.W, padx=20, pady=5)
    
    def ok_clicked():
        dialog.result = selected_value.get()
        # Save selection to config
        config["last_mode"] = dialog.result
        save_config(config)
        dialog.destroy()
    
    def cancel_clicked():
        dialog.result = None
        dialog.destroy()
    
    def on_arrow_up(event):
        if selected_value.get() == "batch":
            selected_value.set("single")
        else:
            selected_value.set("batch")
        return "break"
    
    def on_arrow_down(event):
        if selected_value.get() == "single":
            selected_value.set("batch")
        else:
            selected_value.set("single")
        return "break"
    
    button_frame = tk.Frame(dialog)
    button_frame.pack(pady=15)
    
    ok_btn = tk.Button(button_frame, text="OK", command=ok_clicked, width=10)
    ok_btn.pack(side=tk.LEFT, padx=5)
    
    cancel_btn = tk.Button(button_frame, text="Cancel", command=cancel_clicked, width=10)
    cancel_btn.pack(side=tk.LEFT, padx=5)
    
    dialog.bind('<Up>', on_arrow_up)
    dialog.bind('<Down>', on_arrow_down)
    dialog.bind('<Return>', lambda e: ok_clicked())
    dialog.bind('<Escape>', lambda e: cancel_clicked())
    
    dialog.result = None
    dialog.update()
    dialog.geometry("450x190")
    
    # Center on screen
    screen_width = dialog.winfo_screenwidth()
    screen_height = dialog.winfo_screenheight()
    x = (screen_width // 2) - 225
    y = (screen_height // 2) - 95
    dialog.geometry(f"450x190+{x}+{y}")
    
    dialog.focus_force()
    dialog.wait_window()
    
    return (dialog.result, dialog.result == "batch") if dialog.result else None


def add_emulator_dialog(emulators: dict):
    """
    Dialog to add a new emulator configuration.
    
    Args:
        emulators: The emulators dict (will be modified in-place)
    """
    dialog = tk.Tk()
    dialog.title(f"{PRODUCT_NAME} - Add Emulator")
    
    frame = tk.Frame(dialog, padx=15, pady=15)
    frame.pack(fill=tk.BOTH, expand=True)
    
    # Emulator ID
    tk.Label(frame, text="Emulator ID:", font=("Arial", 10, "bold")).pack(anchor=tk.W, pady=(0, 5))
    emulator_id_var = tk.StringVar()
    emulator_id_entry = tk.Entry(frame, textvariable=emulator_id_var, width=40)
    emulator_id_entry.pack(anchor=tk.W, fill=tk.X, pady=(0, 15))
    
    # Emulator Name
    tk.Label(frame, text="Display Name:", font=("Arial", 10, "bold")).pack(anchor=tk.W, pady=(0, 5))
    emulator_name_var = tk.StringVar()
    emulator_name_entry = tk.Entry(frame, textvariable=emulator_name_var, width=40)
    emulator_name_entry.pack(anchor=tk.W, fill=tk.X, pady=(0, 15))
    
    # Executable
    tk.Label(frame, text="Executable Path:", font=("Arial", 10, "bold")).pack(anchor=tk.W, pady=(0, 5))
    executable_var = tk.StringVar()
    executable_frame = tk.Frame(frame)
    executable_frame.pack(anchor=tk.W, fill=tk.X, pady=(0, 15))
    executable_entry = tk.Entry(executable_frame, textvariable=executable_var, width=30)
    executable_entry.pack(side=tk.LEFT, fill=tk.X, expand=True)
    browse_btn = tk.Button(executable_frame, text="Browse", width=10)
    browse_btn.pack(side=tk.LEFT, padx=(5, 0))
    
    info_label = tk.Label(frame, text="Note: Full emulator configuration can be edited in emulators.json", font=("Arial", 9), fg="gray")
    info_label.pack(anchor=tk.W, pady=10)
    
    def browse_executable():
        path = filedialog.askopenfilename(title="Select Executable", filetypes=[("Executable Files", "*.exe"), ("All Files", "*.*")])
        if path:
            executable_var.set(path)
    
    browse_btn.config(command=browse_executable)
    
    def ok_clicked():
        emulator_id = emulator_id_var.get().strip()
        name = emulator_name_var.get().strip()
        executable = executable_var.get().strip()
        
        if not emulator_id or not name or not executable:
            messagebox.showerror("Error", "All fields are required!")
            return
        
        # Create basic emulator entry
        emulators[emulator_id] = {
            "name": name,
            "executable": executable,
            "arguments": []
        }
        
        save_emulators(emulators)
        print(color_text(f"✓ Created emulator: {emulator_id} ({name})", COLOR_YELLOW))
        print(f"  Edit emulators.json to add arguments and customize further")
        dialog.destroy()
    
    def cancel_clicked():
        dialog.destroy()
    
    button_frame = tk.Frame(frame)
    button_frame.pack(pady=20)
    
    ok_btn = tk.Button(button_frame, text="Create", command=ok_clicked, width=10)
    ok_btn.pack(side=tk.LEFT, padx=5)
    
    cancel_btn = tk.Button(button_frame, text="Cancel", command=cancel_clicked, width=10)
    cancel_btn.pack(side=tk.LEFT, padx=5)
    
    # Set size, then update, then center on screen
    dialog.geometry("500x350")
    dialog.update()
    dialog.update_idletasks()
    
    # Center on screen
    screen_width = dialog.winfo_screenwidth()
    screen_height = dialog.winfo_screenheight()
    x = (screen_width // 2) - 250
    y = (screen_height // 2) - 175
    dialog.geometry(f"500x350+{x}+{y}")
    
    dialog.focus_force()
    dialog.wait_window()


def delete_tag_dialog(catalog: dict, config: dict):
    """
    Dialog to delete a tag from the catalog.
    User selects UID from scrolling picklist to delete with confirmation.
    """
    if not catalog:
        messagebox.showinfo(f"{PRODUCT_NAME} - Delete Tag", "No tags in catalog to delete.")
        return
    
    dialog = tk.Tk()
    dialog.title(f"{PRODUCT_NAME} - Delete Tag")
    dialog.resizable(False, False)
    dialog.attributes('-topmost', True)
    
    # Track dialog state to prevent stalling on close
    dialog_closed = [False]
    
    def on_closing():
        dialog_closed[0] = True
        dialog.destroy()
    
    dialog.protocol("WM_DELETE_WINDOW", on_closing)
    
    frame = tk.Frame(dialog, padx=15, pady=15)
    frame.pack(fill=tk.BOTH, expand=True)
    
    tk.Label(frame, text="Select tag to delete:", font=("Arial", 10, "bold")).pack(anchor=tk.W, pady=(0, 10))
    
    # Create scrollable Listbox instead of radio buttons
    list_frame = tk.Frame(frame)
    list_frame.pack(fill=tk.BOTH, expand=True, pady=(0, 15))
    
    scrollbar = tk.Scrollbar(list_frame)
    scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
    
    listbox = tk.Listbox(list_frame, yscrollcommand=scrollbar.set, font=("Arial", 9), 
                         width=60, height=15, selectmode=tk.EXTENDED)
    listbox.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
    scrollbar.config(command=listbox.yview)
    
    # Populate listbox with UIDs and action previews
    uid_list = []
    for uid, action in sorted(catalog.items()):
        action_str = str(action)[:50]
        text = f"{uid} → {action_str}"
        listbox.insert(tk.END, text)
        uid_list.append(uid)
    
    # Don't select anything by default (safer)
    
    def delete_clicked():
        selections = listbox.curselection()
        if not selections:
            messagebox.showwarning(f"{PRODUCT_NAME} - Delete Tag", "Please select tag(s) to delete.")
            return
        
        uids_to_delete = [uid_list[i] for i in selections]
        
        if len(uids_to_delete) == 1:
            confirm_msg = f"Delete tag {uids_to_delete[0]}?"
        else:
            confirm_msg = f"Delete {len(uids_to_delete)} tags?"
        
        if messagebox.askyesno(f"{PRODUCT_NAME} - Confirm Delete", confirm_msg):
            for uid in uids_to_delete:
                del catalog[uid]
                print(color_text(f"✓ Deleted: {uid}", COLOR_YELLOW))
            save_catalog(catalog)
            dialog_closed[0] = True
            dialog.destroy()
    
    def cancel_clicked():
        dialog_closed[0] = True
        dialog.destroy()
    
    button_frame = tk.Frame(frame)
    button_frame.pack(pady=(0, 0))
    
    delete_btn = tk.Button(button_frame, text="Delete", command=delete_clicked, width=12)
    delete_btn.pack(side=tk.LEFT, padx=5)
    
    cancel_btn = tk.Button(button_frame, text="Cancel", command=cancel_clicked, width=12)
    cancel_btn.pack(side=tk.LEFT, padx=5)
    
    dialog.geometry("550x450")
    screen_width = dialog.winfo_screenwidth()
    screen_height = dialog.winfo_screenheight()
    x = (screen_width // 2) - 275
    y = (screen_height // 2) - 225
    dialog.geometry(f"550x450+{x}+{y}")
    
    dialog.focus_force()
    dialog.wait_window()


def main():
    global input_thread_stop
    
    try:
        # Load config first to get serial port settings
        config = load_config()
        serial_port = config.get("serial_port", "COM9")
        serial_baud = config.get("serial_baud", 115200)
        
        print(f"Opening {serial_port} at {serial_baud} baud...")
        ser = serial.Serial(serial_port, serial_baud, timeout=1)
        time.sleep(2)
    except Exception as e:
        print(f"ERROR: Could not open serial port: {e}")
        print(f"Check that your RFID reader is connected to {serial_port}")
        print(f"Or update 'serial_port' in config.json")
        return

    # Load catalog and emulators
    catalog = load_catalog()
    emulators = load_emulators()
    
    # Merge catalog into ACTIONS for backward compatibility
    ACTIONS.update(catalog)
    
    print("\n" + "="*50)
    print("RFID Catalog Launcher")
    print("="*50)
    print(f"Catalog: {len(catalog)} tags loaded")
    print("Commands:")
    print("  'a' = Add NFC Tag")
    print("  'd' = Delete Tag")
    print("  'e' = Add/Edit Emulator")
    print("  'q' = Quit")
    print("="*50 + "\n")

    # Start keyboard input thread
    input_thread_stop = False
    kb_thread = threading.Thread(target=keyboard_input_thread, daemon=True)
    kb_thread.start()

    batch_mode = False
    batch_command_type = None

    try:
        while True:
            # Check for serial input (non-blocking, with timeout)
            if ser.in_waiting:
                line = ser.readline().decode(errors="ignore").strip()
                if not line:
                    continue

                print(color_text("Serial:", COLOR_CYAN), line)

                if line.startswith("UID:"):
                    uid = line.replace("UID:", "").strip()
                    
                    # Check if uid exists in ACTIONS (which now includes catalog)
                    if uid in ACTIONS:
                        print(color_text(f"Found: {ACTIONS[uid]}", COLOR_GREEN))
                        execute_action(ACTIONS[uid], emulators)
                    else:
                        print(color_text(f"Unknown UID: {uid}", COLOR_RED))
                        if ask_add_to_catalog(uid):
                            command_type = prompt_command_type(config)
                            if command_type:
                                add_tag_from_uid(uid, catalog, config, command_type, emulators)
                                # Update ACTIONS with new entry
                                ACTIONS.update(catalog)
            
            # Check for keyboard input (non-blocking via thread)
            cmd = get_keyboard_input()
            if cmd:
                if cmd == 'a':
                    # Prompt for mode
                    mode_result = prompt_mode(config)
                    if mode_result is None:
                        print("Cancelled.")
                    else:
                        mode, is_batch = mode_result
                        batch_mode = is_batch
                        
                        if batch_mode:
                            batch_command_type = prompt_command_type(config)
                            if not batch_command_type:
                                print("Cancelled.")
                                batch_mode = False
                            else:
                                print(f"Batch mode: {batch_command_type}")
                                print("Type 'done' to exit batch mode")
                        else:
                            command_type = prompt_command_type(config)
                            if command_type:
                                add_tag_interactive(ser, catalog, config, emulators, command_type, False)
                                # Update ACTIONS with new entry
                                ACTIONS.update(catalog)
                
                elif cmd == 'e':
                    # Add/Edit Emulator
                    add_emulator_dialog(emulators)
                
                elif cmd == 'd':
                    # Delete a tag
                    delete_tag_dialog(catalog, config)
                    # Update ACTIONS with new catalog
                    ACTIONS.clear()
                    ACTIONS.update(catalog)
                
                elif cmd == 'done' and batch_mode:
                    batch_mode = False
                    batch_command_type = None
                    print("Batch mode ended.")
                
                elif cmd == 'q':
                    print("Exiting...")
                    break
            
            time.sleep(0.05)  # Small delay to prevent CPU spinning
    
    finally:
        input_thread_stop = True
        ser.close()
        kb_thread.join(timeout=1)


if __name__ == "__main__":
    main()
