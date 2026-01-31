using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Threading.Tasks;

class RFMediaLinkConfigurator
{
    // P/Invoke for ANSI color support
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    private static bool AnsiSupported = false;

    // ANSI color codes
    private static class Color
    {
        public const string Reset = "\x1b[0m";
        public const string Bold = "\x1b[1m";
        public const string Black = "\x1b[30m";
        public const string Red = "\x1b[31m";
        public const string Green = "\x1b[32m";
        public const string Yellow = "\x1b[33m";
        public const string Blue = "\x1b[34m";
        public const string Magenta = "\x1b[35m";
        public const string Cyan = "\x1b[36m";
        public const string White = "\x1b[37m";
        public const string BrightBlack = "\x1b[90m";
        public const string BrightRed = "\x1b[91m";
        public const string BrightGreen = "\x1b[92m";
        public const string BrightYellow = "\x1b[93m";
        public const string BrightBlue = "\x1b[94m";
        public const string BrightMagenta = "\x1b[95m";
        public const string BrightCyan = "\x1b[96m";
        public const string BrightWhite = "\x1b[97m";
    }

    private static string ConfigDir;
    private static string ConfigFile;
    private static string CatalogFile;
    private static string EmulatorsFile;
    private static string BackupDir;
    private static string Version = "1.0.0";
    
    private static JsonDocument? ConfigDoc;
    private static JsonDocument? CatalogDoc;
    private static JsonDocument? EmulatorsDoc;
    
    private static FileSystemWatcher? _catalogWatcher;
    private static System.Threading.Timer? _catalogReloadTimer;
    
    private static JsonElement Config => ConfigDoc?.RootElement ?? default;
    private static JsonElement Catalog => CatalogDoc?.RootElement ?? default;
    private static JsonElement Emulators => EmulatorsDoc?.RootElement ?? default;

    // Helper method to find a tag UID case-insensitively
    private static string? FindTagUid(string searchUid)
    {
        if (Catalog.ValueKind != JsonValueKind.Object) return null;
        
        var searchUidUpper = searchUid.ToUpperInvariant();
        foreach (var tag in Catalog.EnumerateObject())
        {
            if (tag.Name.ToUpperInvariant() == searchUidUpper)
            {
                return tag.Name;
            }
        }
        return null;
    }

    [STAThread]
    static void Main(string[] args)
    {
        EnableAnsiSupport();
        LoadVersion();
        FindConfigDir();
        InitializeBackupDir();
        CheckForAutoRestore();
        LoadAllData();
        SetupCatalogWatcher();
        MainMenu();
    }

    private static void EnableAnsiSupport()
    {
        try
        {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (GetConsoleMode(handle, out uint mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                if (SetConsoleMode(handle, mode))
                {
                    AnsiSupported = true;
                    // Set black background
                    Console.Write("\x1b[40m\x1b[2J\x1b[H");
                }
            }
        }
        catch
        {
            AnsiSupported = false;
        }
    }

    private static string C(string color, string text)
    {
        return AnsiSupported ? $"{color}{text}{Color.Reset}" : text;
    }

    private static void LoadVersion()
    {
        try
        {
            var versionFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VERSION");
            if (File.Exists(versionFile))
            {
                Version = File.ReadAllText(versionFile).Trim();
            }
        }
        catch { }
    }

    private static void SetupCatalogWatcher()
    {
        try
        {
            _catalogWatcher = new FileSystemWatcher(ConfigDir, "catalog.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = false
            };

            _catalogWatcher.Changed += (sender, e) =>
            {
                // Debounce rapid changes
                _catalogReloadTimer?.Dispose();
                _catalogReloadTimer = new System.Threading.Timer(_ =>
                {
                    ReloadCatalogFromDisk();
                }, null, 300, System.Threading.Timeout.Infinite);
            };

            _catalogWatcher.EnableRaisingEvents = true;
        }
        catch { }
    }

    private static void ReloadCatalogFromDisk()
    {
        try
        {
            if (!File.Exists(CatalogFile))
                return;

            // Force flush
            using (var fs = File.Open(CatalogFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Flush();
            }

            // Copy and read
            var tempPath = Path.Combine(ConfigDir, $"catalog_reload_{Guid.NewGuid()}.json");
            File.Copy(CatalogFile, tempPath, true);
            CatalogDoc?.Dispose();
            CatalogDoc = JsonDocument.Parse(File.ReadAllText(tempPath));
            try { File.Delete(tempPath); } catch { }
        }
        catch { }
    }

    private static void FindConfigDir()
    {
        // Check ProgramData first (new default location for service)
        string programDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RFMediaLink");
        if (Directory.Exists(programDataPath) && File.Exists(Path.Combine(programDataPath, "config.json")))
        {
            ConfigDir = programDataPath;
        }
        else
        {
            // Fallback to LocalAppData for backward compatibility
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RFMediaLink");
            if (Directory.Exists(appDataPath) && File.Exists(Path.Combine(appDataPath, "config.json")))
            {
                ConfigDir = appDataPath;
            }
            else
            {
                string progFilesPath = @"C:\Program Files\RFMediaLink";
                if (Directory.Exists(progFilesPath) && File.Exists(Path.Combine(progFilesPath, "config.json")))
                {
                    ConfigDir = progFilesPath;
                }
                else
                {
                    // Default to ProgramData if nothing found
                    ConfigDir = programDataPath;
                }
            }
        }

        ConfigFile = Path.Combine(ConfigDir, "config.json");
        CatalogFile = Path.Combine(ConfigDir, "catalog.json");
        EmulatorsFile = Path.Combine(ConfigDir, "emulators.json");
    }

    private static void LoadAllData()
    {
        // Force flush and copy files to ensure fresh data from disk
        if (File.Exists(ConfigFile))
        {
            try 
            { 
                // Force OS to flush the file
                using (var fs = File.Open(ConfigFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.Flush();
                }
                
                var tempPath = Path.Combine(ConfigDir, $"config_temp_{Guid.NewGuid()}.json");
                File.Copy(ConfigFile, tempPath, true);
                ConfigDoc?.Dispose();
                ConfigDoc = JsonDocument.Parse(File.ReadAllText(tempPath));
                try { File.Delete(tempPath); } catch { }
            }
            catch { }
        }

        if (File.Exists(CatalogFile))
        {
            try 
            { 
                // Force OS to flush the file
                using (var fs = File.Open(CatalogFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.Flush();
                }
                
                var tempPath = Path.Combine(ConfigDir, $"catalog_temp_{Guid.NewGuid()}.json");
                File.Copy(CatalogFile, tempPath, true);
                CatalogDoc?.Dispose();
                CatalogDoc = JsonDocument.Parse(File.ReadAllText(tempPath));
                try { File.Delete(tempPath); } catch { }
            }
            catch { }
        }

        if (File.Exists(EmulatorsFile))
        {
            try 
            { 
                // Force OS to flush the file
                using (var fs = File.Open(EmulatorsFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.Flush();
                }
                
                var tempPath = Path.Combine(ConfigDir, $"emulators_temp_{Guid.NewGuid()}.json");
                File.Copy(EmulatorsFile, tempPath, true);
                EmulatorsDoc?.Dispose();
                EmulatorsDoc = JsonDocument.Parse(File.ReadAllText(tempPath));
                try { File.Delete(tempPath); } catch { }
            }
            catch { }
        }
    }

    private static void MainMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
            Console.WriteLine(C(Color.Bold + Color.BrightWhite, $"  RF Media Link Configuration Tool") + C(Color.BrightYellow, $" v{Version}"));
            Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
            Console.WriteLine(C(Color.BrightBlack, $"Config Location: {ConfigDir}"));
            Console.WriteLine();
            Console.WriteLine(C(Color.BrightGreen, "1.") + " Manage Tags");
            Console.WriteLine(C(Color.BrightGreen, "2.") + " Manage Emulators");
            Console.WriteLine(C(Color.BrightGreen, "3.") + " Service Control");
            Console.WriteLine(C(Color.BrightGreen, "4.") + " View Logs");
            Console.WriteLine(C(Color.BrightGreen, "5.") + " Backup & Restore");
            Console.WriteLine(C(Color.BrightGreen, "6.") + " Settings");
            Console.WriteLine(C(Color.BrightRed, "7.") + " Exit");
            Console.WriteLine();
            Console.Write(C(Color.BrightCyan, "Select option (1-7): "));

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    ManageTags();
                    break;
                case "2":
                    ManageEmulators();
                    break;
                case "3":
                    ServiceControl();
                    break;
                case "4":
                    ViewLogs();
                    break;
                case "5":
                    BackupAndRestore();
                    break;
                case "6":
                    Settings();
                    break;
                case "7":
                    return;
            }
        }
    }
    private static void EditTag()
    {
        // Reload to get latest tags
        LoadAllData();
        
        Console.Clear();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Edit RFID Tag");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        // Show current tags
        var tagList = new List<(string id, string name)>();
        if (Catalog.ValueKind == JsonValueKind.Object)
        {
            foreach (var tag in Catalog.EnumerateObject())
            {
                var tagName = tag.Value.TryGetProperty("name", out var tn) ? tn.GetString() : "Unknown";
                tagList.Add((tag.Name, tagName ?? "Unknown"));
            }

            if (tagList.Count > 0)
            {
                Console.WriteLine("Select tag to edit:");
                for (int i = 0; i < tagList.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. [{tagList[i].id}] {tagList[i].name}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("(No tags configured)");
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                return;
            }
        }

        Console.Write("Enter number (or 0 to cancel): ");
        string input = (Console.ReadLine() ?? "").Trim();

        if (!int.TryParse(input, out int choice) || choice < 1 || choice > tagList.Count)
        {
            if (choice != 0)
                Console.WriteLine("Invalid selection.");
            System.Threading.Thread.Sleep(1000);
            return;
        }

        // Get the selected tag UID
        string uid = tagList[choice - 1].id;
        var foundUid = FindTagUid(uid);
        if (foundUid == null)
        {
            Console.WriteLine($"Tag {uid} not found.");
            System.Threading.Thread.Sleep(1500);
            return;
        }

        var existingTag = Catalog.GetProperty(foundUid);

        // Get existing values
        string existingName = existingTag.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        string existingActionType = existingTag.TryGetProperty("action_type", out var at) ? at.GetString() ?? "" : "";
        string existingTarget = existingTag.TryGetProperty("action_target", out var tgt) ? tgt.GetString() ?? "" : "";

        Console.WriteLine();
        Console.WriteLine($"Current values for {uid}:");
        Console.WriteLine($"  Name: {existingName}");
        Console.WriteLine($"  Action Type: {existingActionType}");
        Console.WriteLine($"  Target: {existingTarget}");
        Console.WriteLine();

        // Edit name
        Console.Write($"New Name (or Enter to keep '{existingName}'): ");
        string newName = Console.ReadLine() ?? "";
        if (string.IsNullOrWhiteSpace(newName))
            newName = existingName;

        // Edit action type
        Console.WriteLine();
        Console.WriteLine("Action Type:");
        Console.WriteLine("  1. emulator");
        Console.WriteLine("  2. file");
        Console.WriteLine("  3. url");
        Console.WriteLine("  4. command");
        Console.Write($"Select (1-4, or Enter to keep '{existingActionType}'): ");
        string actionChoice = Console.ReadLine() ?? "";
        string newActionType = existingActionType;
        
        if (!string.IsNullOrWhiteSpace(actionChoice))
        {
            newActionType = actionChoice switch
            {
                "2" => "file",
                "3" => "url",
                "4" => "command",
                "1" => "emulator",
                _ => existingActionType
            };
        }

        Console.WriteLine();

        // Handle target based on action type
        if (newActionType == "emulator")
        {
            // Show emulators list
            var emuList = new List<(string id, string name)>();
            if (Emulators.ValueKind == JsonValueKind.Object)
            {
                foreach (var emu in Emulators.EnumerateObject())
                {
                    var name = emu.Value.TryGetProperty("name", out var emn) ? emn.GetString() : emu.Name;
                    emuList.Add((emu.Name, name));
                }
            }

            if (emuList.Count > 0)
            {
                Console.WriteLine("Select Emulator:");
                for (int i = 0; i < emuList.Count; i++)
                {
                    string marker = (emuList[i].id == existingTarget) ? " (current)" : "";
                    Console.WriteLine($"  {i + 1}. {emuList[i].name}{marker}");
                }
                Console.Write($"Select (number, or Enter to keep current): ");
                
                string emuChoice = Console.ReadLine() ?? "";
                if (!string.IsNullOrWhiteSpace(emuChoice) && int.TryParse(emuChoice, out int idx) && idx > 0 && idx <= emuList.Count)
                {
                    string selectedEmuId = emuList[idx - 1].id;
                    var emuElement = Emulators.GetProperty(selectedEmuId);

                    // Get emulator arguments
                    var argsList = new List<(string name, JsonElement def)>();
                    if (emuElement.TryGetProperty("arguments", out var argsElement) && argsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var arg in argsElement.EnumerateArray())
                        {
                            if (arg.TryGetProperty("name", out var argName))
                            {
                                argsList.Add((argName.GetString() ?? "", arg));
                            }
                        }
                    }

                    // Get existing argument values
                    var existingArgs = new Dictionary<string, string>();
                    if (existingTag.TryGetProperty("action_args", out var existingArgsElement) && existingArgsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var argProp in existingArgsElement.EnumerateObject())
                        {
                            existingArgs[argProp.Name] = argProp.Value.GetString() ?? "";
                        }
                    }

                    Console.Clear();
                    Console.WriteLine($"Configuring: {emuList[idx - 1].name}");
                    Console.WriteLine();

                    var argValues = new Dictionary<string, string>();

                    // Collect argument values
                    for (int i = 0; i < argsList.Count; i++)
                    {
                        var (argName, argDef) = argsList[i];
                        string defaultVal = argDef.TryGetProperty("default", out var d) ? d.ToString() : "";
                        string type = argDef.TryGetProperty("type", out var t) ? t.GetString() ?? "string" : "string";
                        string existingVal = existingArgs.ContainsKey(argName) ? existingArgs[argName] : defaultVal;

                        Console.WriteLine($"[{i + 1}/{argsList.Count}] {argName} ({type})");
                        if (!string.IsNullOrEmpty(existingVal))
                            Console.WriteLine($"  Current: {existingVal}");

                        string value = "";
                        
                        if (argDef.TryGetProperty("choices", out var choicesElement) && choicesElement.ValueKind == JsonValueKind.Array)
                        {
                            var choices = new List<string>();
                            foreach (var choiceItem in choicesElement.EnumerateArray())
                            {
                                choices.Add(choiceItem.GetString() ?? "");
                            }

                            Console.WriteLine("  Options:");
                            for (int j = 0; j < choices.Count; j++)
                            {
                                string marker = (choices[j] == existingVal) ? " (current)" : "";
                                Console.WriteLine($"    {j + 1}. {choices[j]}{marker}");
                            }
                            Console.Write("  Select (number or Enter to keep current): ");
                            string choice_input = Console.ReadLine() ?? "";
                            
                            if (string.IsNullOrWhiteSpace(choice_input))
                            {
                                value = existingVal;
                            }
                            else if (int.TryParse(choice_input, out int choiceIdx) && choiceIdx > 0 && choiceIdx <= choices.Count)
                            {
                                value = choices[choiceIdx - 1];
                            }
                            else
                            {
                                value = existingVal;
                            }
                        }
                        else if (type == "file")
                        {
                            Console.Write("  [B] Browse, or Enter new path (or Enter to keep current): ");
                            string fileInput = Console.ReadLine() ?? "";
                            
                            if (fileInput.Trim().ToUpper() == "B")
                            {
                                value = BrowseForFile();
                                if (string.IsNullOrEmpty(value))
                                    value = existingVal;
                            }
                            else if (string.IsNullOrWhiteSpace(fileInput))
                            {
                                value = existingVal;
                            }
                            else
                            {
                                value = fileInput;
                            }
                        }
                        else
                        {
                            Console.Write("  New value (or Enter to keep current): ");
                            string valueInput = Console.ReadLine() ?? "";
                            value = string.IsNullOrWhiteSpace(valueInput) ? existingVal : valueInput;
                        }

                        argValues[argName] = value;
                        Console.WriteLine();
                    }

                    // Review and save
                    Console.Clear();
                    Console.WriteLine("═══════════════════════════════════════════════════════");
                    Console.WriteLine($"UID:     {uid}");
                    Console.WriteLine($"Name:    {newName}");
                    Console.WriteLine($"Action:  emulator");
                    Console.WriteLine($"Target:  {selectedEmuId}");
                    Console.WriteLine();
                    Console.WriteLine("Arguments:");
                    foreach (var (name, value) in argValues)
                    {
                        Console.WriteLine($"  {name}: {value}");
                    }
                    Console.WriteLine();
                    Console.Write("Save changes? (Y/N): ");

                    if (Console.ReadLine()?.ToUpper() == "Y")
                    {
                        SaveTag(uid, newName, "emulator", selectedEmuId, argValues);
                        Console.WriteLine("Tag updated!");
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                else
                {
                    // Keep existing emulator and args
                    Console.WriteLine("Keeping existing emulator configuration.");
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
        else
        {
            // For non-emulator actions
            Console.Write($"New Target (or Enter to keep '{existingTarget}'): ");
            string newTarget = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(newTarget))
                newTarget = existingTarget;

            Console.WriteLine();
            Console.WriteLine($"UID:    {uid}");
            Console.WriteLine($"Name:   {newName}");
            Console.WriteLine($"Action: {newActionType}");
            Console.WriteLine($"Target: {newTarget}");
            Console.WriteLine();
            Console.Write("Save changes? (Y/N): ");

            if (Console.ReadLine()?.ToUpper() == "Y")
            {
                SaveTag(uid, newName, newActionType, newTarget);
                Console.WriteLine("Tag updated!");
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
    private static void ManageTags()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
            Console.WriteLine(C(Color.Bold + Color.BrightWhite, "  Manage RFID Tags"));
            Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
            Console.WriteLine();

            var tagList = new List<(string uid, string name)>();

            if (Catalog.ValueKind == JsonValueKind.Object)
            {
                int count = 0;
                foreach (var tag in Catalog.EnumerateObject())
                {
                    count++;
                    var name = tag.Value.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                    tagList.Add((tag.Name, name));
                    Console.WriteLine(C(Color.BrightYellow, $"{count}.") + C(Color.BrightBlack, $" [{tag.Name}]") + $" {name}");
                }
            }

            if (tagList.Count == 0)
            {
                Console.WriteLine(C(Color.BrightBlack, "(No tags configured)"));
            }

            Console.WriteLine();
            Console.WriteLine(C(Color.BrightGreen, "A.") + " Add Tag");
            Console.WriteLine(C(Color.BrightGreen, "E.") + " Edit Tag");
            Console.WriteLine(C(Color.BrightGreen, "D.") + " Delete Tag");
            Console.WriteLine(C(Color.BrightRed, "B.") + " Back to Menu");
            Console.WriteLine();
            Console.Write(C(Color.BrightCyan, "Select option (A/E/D/B): "));

            string choice = Console.ReadLine()?.ToUpper() ?? "";
            switch (choice)
            {
                case "A":
                    AddTag();
                    break;
                case "E":
                    EditTag();
                    break;
                case "D":
                    DeleteTag();
                    break;
                case "B":
                    return;
            }
        }
    }

    private static void AddTag()
    {
        Console.Clear();
        Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
        Console.WriteLine(C(Color.Bold + Color.BrightWhite, "  Add RFID Tag"));
        Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
        Console.WriteLine();
        
        // Step 1: Get UID (default to scanning)
        Console.WriteLine(C(Color.BrightYellow, "Waiting for scan (place tag on reader)..."));
        Console.WriteLine(C(Color.BrightBlack, "Press Enter to enter UID manually."));
        string uid = WaitForScan();

        if (string.IsNullOrEmpty(uid))
        {
            Console.WriteLine("No tag scanned. Press any key...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"UID: {uid}");
        Console.WriteLine();

        // Step 2: Get tag name
        Console.Write("Tag Name: ");
        string name = Console.ReadLine() ?? "";

        Console.WriteLine();
        Console.WriteLine("Action Type:");
        Console.WriteLine("  1. emulator");
        Console.WriteLine("  2. file");
        Console.WriteLine("  3. url");
        Console.WriteLine("  4. command");
        Console.Write("Select (1-4): ");
        string actionChoice = Console.ReadLine() ?? "1";
        string actionType = actionChoice switch
        {
            "2" => "file",
            "3" => "url",
            "4" => "command",
            _ => "emulator"
        };

        Console.WriteLine();

        if (actionType == "emulator")
        {
            SelectAndConfigureEmulator(uid, name);
        }
        else
        {
            bool addMore = true;
            string lastTarget = "";
            
            while (addMore)
            {
                if (string.IsNullOrEmpty(lastTarget))
                {
                    Console.Write($"Target ({actionType}): ");
                    lastTarget = Console.ReadLine() ?? "";
                }

                Console.WriteLine();
                Console.WriteLine($"UID:    {uid}");
                Console.WriteLine($"Name:   {name}");
                Console.WriteLine($"Action: {actionType}");
                Console.WriteLine($"Target: {lastTarget}");
                Console.WriteLine();
                Console.Write("Save? (Y/N): ");

                if (Console.ReadLine()?.ToUpper() == "Y")
                {
                    SaveTag(uid, name, actionType, lastTarget);
                    Console.WriteLine("Tag saved!");
                    System.Threading.Thread.Sleep(1000);
                    
                    // Ask if they want to add another
                    Console.WriteLine();
                    Console.Write($"Add another tag with same settings ({actionType}/{lastTarget})? (Y/N): ");
                    if (Console.ReadLine()?.ToUpper() == "Y")
                    {
                        // Get new UID and name for next tag
                        Console.WriteLine();
                        uid = WaitForScan();
                        if (string.IsNullOrEmpty(uid))
                            break;
                        
                        Console.Write("Tag Name: ");
                        name = Console.ReadLine() ?? "";
                        Console.Clear();
                    }
                    else
                    {
                        addMore = false;
                    }
                }
                else
                {
                    addMore = false;
                }
            }
        }
    }

    private static string BrowseForFile()
    {
        try
        {
            // Use Windows.Storage.Pickers if available, otherwise fallback
            var openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Select File",
                Filter = "All files (*.*)|*.*|Executables (*.exe)|*.exe|ROM files (*.rom;*.bin;*.cue;*.iso)|*.rom;*.bin;*.cue;*.iso",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
        }
        catch
        {
            // If dialog fails, just return empty and user can type manually
        }
        return "";
    }

    private static string WaitForScan()
    {
        string scanFile = Path.Combine(ConfigDir, "scan_null.log");
        
        // Delete any existing scan file FIRST
        try { File.Delete(scanFile); } catch { }
        
        Console.WriteLine();
        Console.WriteLine(">>> Press Enter to enter UID manually, or place tag on reader <<<");
        Console.WriteLine();

        // Check if any key is already pressed (non-blocking)
        Task<string> manualTask = Task.Run(() =>
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.Write("Enter UID manually: ");
                return Console.ReadLine() ?? "";
            }
            return "";
        });

        // Wait for either manual entry or scan
        int attempts = 0;
        while (attempts < 300)  // 30 seconds
        {
            // Check if manual entry was started
            if (manualTask.IsCompleted)
            {
                return manualTask.Result;
            }

            // Check for scan file
            if (File.Exists(scanFile))
            {
                try
                {
                    string content = File.ReadAllText(scanFile);
                    string uid = content.Split('\n')[0].Trim();
                    if (!string.IsNullOrEmpty(uid))
                    {
                        // Delete the file immediately after reading
                        try { File.Delete(scanFile); } catch { }
                        return uid;
                    }
                }
                catch { }
            }

            System.Threading.Thread.Sleep(100);
            attempts++;
        }

        // Wait for manual entry to complete (or timeout)
        try { manualTask.Wait(TimeSpan.FromSeconds(1)); }
        catch { }

        return "";
    }

    private static void SelectAndConfigureEmulator(string uid, string tagName)
    {
        // Show list of emulators to select from
        var emuList = new List<(string id, string name)>();

        if (Emulators.ValueKind == JsonValueKind.Object)
        {
            foreach (var emu in Emulators.EnumerateObject())
            {
                var name = emu.Value.TryGetProperty("name", out var n) ? n.GetString() : emu.Name;
                emuList.Add((emu.Name, name));
            }
        }

        if (emuList.Count == 0)
        {
            Console.WriteLine("No emulators configured.");
            Console.WriteLine("Press any key...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Select Emulator:");
        for (int i = 0; i < emuList.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {emuList[i].name}");
        }
        Console.Write("Select (number): ");

        string choice = Console.ReadLine() ?? "1";
        if (!int.TryParse(choice, out int idx) || idx < 1 || idx > emuList.Count)
            idx = 1;

        string selectedEmuId = emuList[idx - 1].id;
        var emuElement = Emulators.GetProperty(selectedEmuId);

        // Get the arguments definition
        var argsList = new List<(string name, JsonElement def)>();
        if (emuElement.TryGetProperty("arguments", out var argsElement))
        {
            if (argsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var arg in argsElement.EnumerateArray())
                {
                    if (arg.TryGetProperty("name", out var argName))
                    {
                        argsList.Add((argName.GetString() ?? "", arg));
                    }
                }
            }
        }

        // Loop for adding multiple tags with same emulator
        bool addMore = true;
        Dictionary<string, string>? lastArgValues = null;
        
        while (addMore)
        {
            Console.Clear();
            Console.WriteLine($"Configuring: {emuList[idx - 1].name}");
            Console.WriteLine();

            var argValues = lastArgValues ?? new Dictionary<string, string>();

            // Collect argument values (reuse last values if available)
            for (int i = 0; i < argsList.Count; i++)
            {
                var (argName, argDef) = argsList[i];
                string defaultVal = argDef.TryGetProperty("default", out var d) ? d.ToString() : "";
                string type = argDef.TryGetProperty("type", out var t) ? t.GetString() ?? "string" : "string";
                
                // If we have last values and this is a non-file argument, reuse it
                string lastValue = "";
                if (lastArgValues != null && lastArgValues.ContainsKey(argName) && type != "file")
                {
                    lastValue = lastArgValues[argName];
                    argValues[argName] = lastValue;
                    Console.WriteLine($"[{i + 1}/{argsList.Count}] {argName}: {lastValue} (reusing)");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine($"[{i + 1}/{argsList.Count}] {argName} ({type})");
                if (!string.IsNullOrEmpty(defaultVal))
                    Console.WriteLine($"  Default: {defaultVal}");

                // Check if this argument has choices
                string value = "";
                if (argDef.TryGetProperty("choices", out var choicesElement) && choicesElement.ValueKind == JsonValueKind.Array)
                {
                    var choices = new List<string>();
                    foreach (var choiceItem in choicesElement.EnumerateArray())
                    {
                        choices.Add(choiceItem.GetString() ?? "");
                    }

                    Console.WriteLine("  Options:");
                    for (int j = 0; j < choices.Count; j++)
                    {
                        string marker = (!string.IsNullOrEmpty(defaultVal) && choices[j] == defaultVal) ? " (default)" : "";
                        Console.WriteLine($"    {j + 1}. {choices[j]}{marker}");
                    }
                    Console.Write("  Select (number or Enter for default): ");
                    string choice_input = Console.ReadLine() ?? "";
                    if (string.IsNullOrWhiteSpace(choice_input))
                    {
                        value = defaultVal;
                    }
                    else if (int.TryParse(choice_input, out int choiceIdx) && choiceIdx > 0 && choiceIdx <= choices.Count)
                    {
                        value = choices[choiceIdx - 1];
                    }
                    else
                    {
                        value = defaultVal;
                    }
                }
                else
                {
                    // For file types, offer file browser or manual entry
                    if (type == "file")
                    {
                        Console.Write("  [B] Browse, or Enter path (or S to skip): ");
                        string input = Console.ReadLine() ?? "";
                        
                        if (input.Trim().ToUpper() == "B")
                        {
                            value = BrowseForFile();
                        }
                        else if (input.Trim().ToUpper() == "S" && i < argsList.Count - 1)
                        {
                            // Fill remaining with defaults
                            for (int j = i; j < argsList.Count; j++)
                            {
                                var (nextName, nextDef) = argsList[j];
                                string nextDefault = nextDef.TryGetProperty("default", out var nd) ? nd.ToString() : "";
                                argValues[nextName] = nextDefault;
                            }
                            break;
                        }
                        else
                        {
                            value = string.IsNullOrWhiteSpace(input) ? defaultVal : input;
                        }
                    }
                    else
                    {
                        Console.Write("  Value (or Enter for default, or S to skip rest): ");
                        string input = Console.ReadLine() ?? "";
                        
                        if (input.Trim().ToUpper() == "S" && i < argsList.Count - 1)
                        {
                            // Fill remaining with defaults
                            for (int j = i; j < argsList.Count; j++)
                            {
                                var (nextName, nextDef) = argsList[j];
                                string nextDefault = nextDef.TryGetProperty("default", out var nd) ? nd.ToString() : "";
                                argValues[nextName] = nextDefault;
                            }
                            break;
                        }
                        
                        value = string.IsNullOrWhiteSpace(input) ? defaultVal : input;
                    }
                }

                argValues[argName] = value;
                Console.WriteLine();
            }

            // Review and save
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine($"UID:     {uid}");
            Console.WriteLine($"Name:    {tagName}");
            Console.WriteLine($"Action:  emulator");
            Console.WriteLine($"Target:  {selectedEmuId}");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            foreach (var (name, value) in argValues)
            {
                Console.WriteLine($"  {name}: {value}");
            }
            Console.WriteLine();
            Console.Write("Save? (Y/N): ");

            if (Console.ReadLine()?.ToUpper() == "Y")
            {
                SaveTag(uid, tagName, "emulator", selectedEmuId, argValues);
                Console.WriteLine("Tag saved!");
                System.Threading.Thread.Sleep(1000);
                
                // Store the arg values for reuse
                lastArgValues = new Dictionary<string, string>(argValues);
                
                // Ask if they want to add another
                Console.WriteLine();
                Console.Write($"Add another tag with same emulator ({emuList[idx - 1].name})? (Y/N): ");
                if (Console.ReadLine()?.ToUpper() == "Y")
                {
                    // Get new UID and name for next tag
                    Console.WriteLine();
                    uid = WaitForScan();
                    if (string.IsNullOrEmpty(uid))
                        break;
                    
                    Console.Write("Tag Name: ");
                    tagName = Console.ReadLine() ?? "";
                }
                else
                {
                    addMore = false;
                }
            }
            else
            {
                addMore = false;
            }
        }
    }

    private static void SaveTag(string uid, string name, string actionType, string target, Dictionary<string, string> emuArgs = null)
    {
        var catalog = new Dictionary<string, object>();

        // Load existing catalog
        if (File.Exists(CatalogFile))
        {
            try
            {
                var json = JsonDocument.Parse(File.ReadAllText(CatalogFile));
                foreach (var prop in json.RootElement.EnumerateObject())
                {
                    var tagDict = new Dictionary<string, object>
                    {
                        { "name", prop.Value.GetProperty("name").GetString() ?? "" },
                        { "action_type", prop.Value.GetProperty("action_type").GetString() ?? "" },
                        { "action_target", prop.Value.GetProperty("action_target").GetString() ?? "" }
                    };
                    if (prop.Value.TryGetProperty("action_args", out var args))
                    {
                        tagDict["action_args"] = JsonSerializer.Deserialize<Dictionary<string, string>>(args.GetRawText());
                    }
                    catalog[prop.Name] = tagDict;
                }
            }
            catch { }
        }

        // Add/update tag
        var newTag = new Dictionary<string, object>
        {
            { "name", name },
            { "action_type", actionType },
            { "action_target", target }
        };

        if (emuArgs != null && emuArgs.Count > 0)
            newTag["action_args"] = emuArgs;

        catalog[uid] = newTag;

        // Serialize to JSON string
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(catalog, options);
        
        // Write to file
        File.WriteAllText(CatalogFile, jsonString);
        
        // Backup after save
        BackupConfigFiles();
        
        // Update in-memory catalog from the same JSON we just wrote
        CatalogDoc?.Dispose();
        CatalogDoc = JsonDocument.Parse(jsonString);
    }

    private static void DeleteTag()
    {
        // Reload to get latest tags
        LoadAllData();
        
        Console.Clear();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Delete RFID Tag");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        // Show current tags
        var tagList = new List<(string id, string name)>();
        if (Catalog.ValueKind == JsonValueKind.Object)
        {
            foreach (var tag in Catalog.EnumerateObject())
            {
                var name = tag.Value.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                tagList.Add((tag.Name, name ?? "Unknown"));
            }
        }

        if (tagList.Count > 0)
        {
            Console.WriteLine("Select tag to delete:");
            for (int i = 0; i < tagList.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. [{tagList[i].id}] {tagList[i].name}");
            }
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("(No tags configured)");
            Console.WriteLine();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
            return;
        }

        Console.Write("Enter number (or 0 to cancel): ");
        string input = Console.ReadLine() ?? "";

        if (!int.TryParse(input, out int choice) || choice < 1 || choice > tagList.Count)
        {
            if (choice != 0)
                Console.WriteLine("Invalid selection.");
            System.Threading.Thread.Sleep(1000);
            return;
        }

        string uid = tagList[choice - 1].id;

        Console.WriteLine();
        Console.WriteLine($"Delete: [{uid}] {tagList[choice - 1].name}");
        Console.Write("Confirm delete? (Y/N): ");
        if (Console.ReadLine()?.ToUpper() == "Y")
        {
                var catalog = new Dictionary<string, object>();
                if (File.Exists(CatalogFile))
                {
                    try
                    {
                        var json = JsonDocument.Parse(File.ReadAllText(CatalogFile));
                        foreach (var prop in json.RootElement.EnumerateObject())
                        {
                            if (prop.Name != uid)
                            {
                                var tagDict = new Dictionary<string, object>
                                {
                                    { "name", prop.Value.GetProperty("name").GetString() ?? "" },
                                    { "action_type", prop.Value.GetProperty("action_type").GetString() ?? "" },
                                    { "action_target", prop.Value.GetProperty("action_target").GetString() ?? "" }
                                };
                                if (prop.Value.TryGetProperty("action_args", out var args))
                                {
                                    tagDict["action_args"] = JsonSerializer.Deserialize<Dictionary<string, string>>(args.GetRawText());
                                }
                                catalog[prop.Name] = tagDict;
                            }
                        }
                    }
                    catch { }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(catalog, options);
                File.WriteAllText(CatalogFile, jsonString);
                
                // Backup after delete
                BackupConfigFiles();
                
                // Update in-memory catalog
                CatalogDoc?.Dispose();
                CatalogDoc = JsonDocument.Parse(jsonString);
                
                Console.WriteLine("Tag deleted!");
        }

        Console.WriteLine("Press any key...");
        Console.ReadKey();
    }

    private static void ViewEmulators()
    {
        Console.Clear();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Emulators");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        if (Emulators.ValueKind == JsonValueKind.Object)
        {
            foreach (var emu in Emulators.EnumerateObject())
            {
                var name = emu.Value.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                var exe = emu.Value.TryGetProperty("executable", out var e) ? e.GetString() : "N/A";
                Console.WriteLine($"{name}");
                Console.WriteLine($"  ID: {emu.Name}");
                Console.WriteLine($"  Path: {exe}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("No emulators configured.");
        }

        Console.WriteLine("Press any key...");
        Console.ReadKey();
    }

    private static void Settings()
    {
        Console.Clear();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Settings");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        string currentPort = "COM9";
        string currentBaud = "115200";

        if (Config.ValueKind == JsonValueKind.Object)
        {
            if (Config.TryGetProperty("serial_port", out var p))
                currentPort = p.GetString() ?? "COM9";
            if (Config.TryGetProperty("baud_rate", out var b))
                currentBaud = b.GetInt32().ToString();
        }

        Console.WriteLine($"Serial Port (current: {currentPort})");
        Console.Write("Enter new port or press Enter to keep: ");
        string newPort = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(newPort)) newPort = currentPort;

        Console.WriteLine();
        Console.WriteLine($"Baud Rate (current: {currentBaud})");
        Console.Write("Enter new baud or press Enter to keep: ");
        string newBaud = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(newBaud)) newBaud = currentBaud;

        Console.WriteLine();
        Console.Write("Save? (Y/N): ");
        if (Console.ReadLine()?.ToUpper() == "Y")
        {
            var config = new Dictionary<string, object>
            {
                { "serial_port", newPort },
                { "baud_rate", int.Parse(newBaud) }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(config, options));
            Console.WriteLine("Settings saved!");
        }

        Console.WriteLine("Press any key...");
        Console.ReadKey();
    }

    private static void ServiceControl()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Service Control");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            // Check if service is running
            var process = Process.GetProcessesByName("RFMediaLinkService").FirstOrDefault();
            if (process != null)
            {
                Console.WriteLine($"Service Status: RUNNING (PID: {process.Id})");
            }
            else
            {
                Console.WriteLine("Service Status: STOPPED");
            }
            Console.WriteLine();

            Console.WriteLine("1. Start Service");
            Console.WriteLine("2. Stop Service");
            Console.WriteLine("3. Restart Service");
            Console.WriteLine("4. Back to Main Menu");
            Console.WriteLine();
            Console.Write("Select option (1-4): ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    StartService();
                    break;
                case "2":
                    StopService();
                    break;
                case "3":
                    RestartService();
                    break;
                case "4":
                    return;
            }
        }
    }

    private static void StartService()
    {
        Console.WriteLine();
        Console.WriteLine("Starting service...");
        
        var running = Process.GetProcessesByName("RFMediaLinkService").FirstOrDefault();
        if (running != null)
        {
            Console.WriteLine($"Service is already running (PID: {running.Id})");
            System.Threading.Thread.Sleep(2000);
            return;
        }

        var installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RFMediaLink");
        var exePath = Path.Combine(installDir, "RFMediaLinkService.exe");

        if (!File.Exists(exePath))
        {
            Console.WriteLine($"ERROR: Service executable not found at {exePath}");
            System.Threading.Thread.Sleep(3000);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = installDir,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            });

            System.Threading.Thread.Sleep(2000);
            running = Process.GetProcessesByName("RFMediaLinkService").FirstOrDefault();
            if (running != null)
            {
                Console.WriteLine($"Service started successfully (PID: {running.Id})");
            }
            else
            {
                Console.WriteLine("Service may not have started properly.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }

        System.Threading.Thread.Sleep(2000);
    }

    private static void StopService()
    {
        Console.WriteLine();
        Console.WriteLine("Stopping service...");

        var processes = Process.GetProcessesByName("RFMediaLinkService");
        if (processes.Length == 0)
        {
            Console.WriteLine("Service is not running.");
            System.Threading.Thread.Sleep(2000);
            return;
        }

        try
        {
            foreach (var proc in processes)
            {
                proc.Kill();
                proc.WaitForExit(5000);
            }
            Console.WriteLine("Service stopped successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }

        System.Threading.Thread.Sleep(2000);
    }

    private static void RestartService()
    {
        Console.WriteLine();
        Console.WriteLine("Restarting service...");
        StopService();
        System.Threading.Thread.Sleep(1000);
        StartService();
    }

    private static void ViewLogs()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
            Console.WriteLine(C(Color.Bold + Color.BrightWhite, "  Service Logs"));
            Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
            Console.WriteLine();
            Console.WriteLine(C(Color.BrightGreen, "1.") + " View Recent Logs (Last 50)");
            Console.WriteLine(C(Color.BrightGreen, "2.") + " View Errors & Warnings");
            Console.WriteLine(C(Color.BrightGreen, "3.") + " View All Logs Today");
            Console.WriteLine(C(Color.BrightGreen, "4.") + " Open Event Viewer");
            Console.WriteLine(C(Color.BrightRed, "B.") + " Back to Main Menu");
            Console.WriteLine();
            Console.Write(C(Color.BrightCyan, "Select option: "));

            string choice = Console.ReadLine()?.ToUpper() ?? "";
            switch (choice)
            {
                case "1":
                    ShowEventLogs("Information", 50);
                    break;
                case "2":
                    ShowEventLogs("ErrorAndWarning", 50);
                    break;
                case "3":
                    ShowEventLogs("Information", 500, DateTime.Today);
                    break;
                case "4":
                    OpenEventViewer();
                    return;
                case "B":
                    return;
            }
        }
    }

    private static void ShowEventLogs(string level, int maxEntries, DateTime? since = null)
    {
        Console.Clear();
        Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
        Console.WriteLine(C(Color.Bold + Color.BrightWhite, $"  Service Logs - {level}"));
        Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
        Console.WriteLine();

        try
        {
            var sinceFilter = since.HasValue ? $" and TimeCreated >= '{since.Value:yyyy-MM-ddTHH:mm:ss}'" : "";
            var query = $"*[System[Provider[@Name='RFMediaLinkService'] and (Level=1 or Level=2 or Level=3 or Level=4){sinceFilter}]]";
            
            if (level == "ErrorAndWarning")
            {
                query = $"*[System[Provider[@Name='RFMediaLinkService'] and (Level=1 or Level=2 or Level=3){sinceFilter}]]";
            }

            // Escape single quotes for PowerShell by doubling them
            var escapedQuery = query.Replace("'", "''");

            // Get events as JSON for structured parsing
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"Get-WinEvent -LogName Application -FilterXPath '{escapedQuery}' -MaxEvents {maxEntries} -ErrorAction SilentlyContinue | Select-Object -First {maxEntries} | ConvertTo-Json\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (string.IsNullOrWhiteSpace(output) || output.Trim() == "null")
                {
                    Console.WriteLine(C(Color.BrightBlack, "No log entries found."));
                    Console.WriteLine();
                    Console.WriteLine(C(Color.BrightBlack, "Press any key to return..."));
                    Console.ReadKey();
                    return;
                }

                // Parse JSON and display numbered list
                var logEntries = new List<Dictionary<string, object>>();
                try
                {
                    var jsonDoc = JsonDocument.Parse(output);
                    if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in jsonDoc.RootElement.EnumerateArray())
                        {
                            var entry = new Dictionary<string, object>();
                            foreach (var prop in item.EnumerateObject())
                            {
                                entry[prop.Name] = prop.Value.ToString();
                            }
                            logEntries.Add(entry);
                        }
                    }
                    else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var entry = new Dictionary<string, object>();
                        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
                        {
                            entry[prop.Name] = prop.Value.ToString();
                        }
                        logEntries.Add(entry);
                    }
                }
                catch
                {
                    Console.WriteLine(C(Color.BrightRed, "Failed to parse log data."));
                    Console.WriteLine();
                    Console.WriteLine(C(Color.BrightBlack, "Press any key to return..."));
                    Console.ReadKey();
                    return;
                }

                // Display numbered list
                for (int i = 0; i < logEntries.Count; i++)
                {
                    var entry = logEntries[i];
                    var time = entry.ContainsKey("TimeCreated") ? entry["TimeCreated"].ToString() : "Unknown";
                    var levelName = entry.ContainsKey("LevelDisplayName") ? entry["LevelDisplayName"].ToString() : "Info";
                    var message = entry.ContainsKey("Message") ? entry["Message"].ToString() : "";
                    
                    // Truncate message for list view
                    if (message.Length > 60)
                        message = message.Substring(0, 60) + "...";

                    var levelColor = levelName.Contains("Error") ? Color.BrightRed : 
                                   levelName.Contains("Warning") ? Color.BrightYellow : 
                                   Color.BrightGreen;

                    Console.WriteLine(C(Color.BrightCyan, $"{i + 1,3}.") + 
                                    C(Color.BrightBlack, $" [{time}]") + 
                                    C(levelColor, $" {levelName,-10}") + 
                                    $" {message}");
                }

                Console.WriteLine();
                Console.Write(C(Color.BrightCyan, "Enter number to view details (or 0 to return): "));
                var input = Console.ReadLine();

                if (int.TryParse(input, out int selection) && selection > 0 && selection <= logEntries.Count)
                {
                    ShowLogDetails(logEntries[selection - 1]);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(C(Color.BrightRed, $"ERROR: {ex.Message}"));
            Console.WriteLine();
            Console.WriteLine(C(Color.BrightBlack, "Press any key to return..."));
            Console.ReadKey();
        }
    }

    private static void ShowLogDetails(Dictionary<string, object> entry)
    {
        Console.Clear();
        Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
        Console.WriteLine(C(Color.Bold + Color.BrightWhite, "  Log Entry Details"));
        Console.WriteLine(C(Color.BrightCyan, "═══════════════════════════════════════════════════════"));
        Console.WriteLine();

        foreach (var kvp in entry)
        {
            // Skip some technical fields
            if (kvp.Key == "Keywords" || kvp.Key == "Bookmark" || kvp.Key == "RecordId" || 
                kvp.Key == "ProviderId" || kvp.Key == "Qualifiers" || kvp.Key == "Version" ||
                kvp.Key == "ProviderName" && kvp.Value.ToString() == "RFMediaLinkService")
                continue;

            var key = kvp.Key;
            var value = kvp.Value.ToString();

            // Color code certain fields
            var keyColor = key == "LevelDisplayName" ? Color.BrightYellow :
                          key == "TimeCreated" ? Color.BrightCyan :
                          key == "Message" ? Color.BrightWhite :
                          Color.BrightGreen;

            Console.WriteLine(C(keyColor, $"{key}:"));
            
            // Word wrap long values
            if (value.Length > 80)
            {
                var words = value.Split(' ');
                var line = "  ";
                foreach (var word in words)
                {
                    if (line.Length + word.Length + 1 > 80)
                    {
                        Console.WriteLine(line);
                        line = "  " + word + " ";
                    }
                    else
                    {
                        line += word + " ";
                    }
                }
                if (line.Trim().Length > 0)
                    Console.WriteLine(line);
            }
            else
            {
                Console.WriteLine($"  {value}");
            }
            Console.WriteLine();
        }

        Console.WriteLine(C(Color.BrightBlack, "Press any key to return..."));
        Console.ReadKey();
    }

    private static void OpenEventViewer()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "eventvwr.msc",
                Arguments = "/c:Application",
                UseShellExecute = true
            });
            Console.WriteLine("Opening Event Viewer...");
            System.Threading.Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            System.Threading.Thread.Sleep(2000);
        }
    }

    private static void ManageEmulators()
    {
        while (true)
        {
            LoadAllData(); // Reload to get latest
            
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Manage Emulators");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            if (Emulators.ValueKind == JsonValueKind.Object)
            {
                var emulatorList = new List<(string id, string name)>();
                foreach (var emu in Emulators.EnumerateObject())
                {
                    var name = emu.Value.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                    emulatorList.Add((emu.Name, name ?? "Unknown"));
                }

                Console.WriteLine("Configured Emulators:");
                foreach (var (id, name) in emulatorList)
                {
                    Console.WriteLine($"  {id} - {name}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("No emulators configured.");
                Console.WriteLine();
            }

            Console.WriteLine("1. View Emulator Details");
            Console.WriteLine("2. Edit emulators.json in Notepad");
            Console.WriteLine("3. Back to Main Menu");
            Console.WriteLine();
            Console.Write("Select option (1-3): ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    ViewEmulators();
                    break;
                case "2":
                    EditEmulatorsFile();
                    break;
                case "3":
                    return;
            }
        }
    }

    private static void EditEmulatorsFile()
    {
        Console.WriteLine();
        Console.WriteLine("Opening emulators.json in Notepad...");
        
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{EmulatorsFile}\"",
                UseShellExecute = true
            });
            Console.WriteLine("File opened. Press any key when done editing...");
            Console.ReadKey();
            LoadAllData(); // Reload after editing
            BackupConfigFiles(); // Backup after editing
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            System.Threading.Thread.Sleep(3000);
        }
    }

    // ============================================================
    // Backup & Restore System
    // ============================================================

    private static void InitializeBackupDir()
    {
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            BackupDir = Path.Combine(documentsPath, "RFMediaLink", "Backups");
            
            if (!Directory.Exists(BackupDir))
            {
                Directory.CreateDirectory(BackupDir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not initialize backup directory: {ex.Message}");
            BackupDir = Path.Combine(Path.GetTempPath(), "RFMediaLink_Backups");
        }
    }

    private static void CheckForAutoRestore()
    {
        try
        {
            // Check if catalog or emulators are missing but backups exist
            bool catalogMissing = !File.Exists(CatalogFile) || new FileInfo(CatalogFile).Length == 0;
            bool emulatorsMissing = !File.Exists(EmulatorsFile) || new FileInfo(EmulatorsFile).Length == 0;

            if (!catalogMissing && !emulatorsMissing)
                return; // Everything exists, no need to restore

            var backupFiles = Directory.GetFiles(BackupDir, "*.json", SearchOption.TopDirectoryOnly);
            if (backupFiles.Length == 0)
                return; // No backups available

            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Missing Configuration Files Detected!");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();
            
            if (catalogMissing)
                Console.WriteLine("⚠ catalog.json is missing or empty");
            if (emulatorsMissing)
                Console.WriteLine("⚠ emulators.json is missing or empty");
            
            Console.WriteLine();
            Console.WriteLine("Backups found in Documents\\RFMediaLink\\Backups");
            Console.WriteLine();
            Console.Write("Would you like to restore from the most recent backup? (Y/N): ");
            
            if (Console.ReadLine()?.ToUpper() == "Y")
            {
                RestoreLatestBackup(catalogMissing, emulatorsMissing);
            }
        }
        catch { }
    }

    private static void RestoreLatestBackup(bool restoreCatalog, bool restoreEmulators)
    {
        try
        {
            if (restoreCatalog)
            {
                var catalogBackups = Directory.GetFiles(BackupDir, "catalog_*.json")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToArray();
                
                if (catalogBackups.Length > 0)
                {
                    File.Copy(catalogBackups[0], CatalogFile, true);
                    Console.WriteLine($"✓ Restored catalog from: {Path.GetFileName(catalogBackups[0])}");
                }
            }

            if (restoreEmulators)
            {
                var emulatorBackups = Directory.GetFiles(BackupDir, "emulators_*.json")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToArray();
                
                if (emulatorBackups.Length > 0)
                {
                    File.Copy(emulatorBackups[0], EmulatorsFile, true);
                    Console.WriteLine($"✓ Restored emulators from: {Path.GetFileName(emulatorBackups[0])}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Restore complete!");
            System.Threading.Thread.Sleep(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR during restore: {ex.Message}");
            System.Threading.Thread.Sleep(3000);
        }
    }

    private static void BackupConfigFiles()
    {
        try
        {
            BackupFile(CatalogFile, "catalog");
            BackupFile(EmulatorsFile, "emulators");
        }
        catch { }
    }

    private static void BackupFile(string sourceFile, string baseName)
    {
        try
        {
            if (!File.Exists(sourceFile))
                return;

            var sourceInfo = new FileInfo(sourceFile);
            if (sourceInfo.Length == 0)
                return; // Don't backup empty files

            // Find most recent backup
            var existingBackups = Directory.GetFiles(BackupDir, $"{baseName}_*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToArray();

            // Check if we should create a new backup
            bool shouldBackup = false;
            
            if (existingBackups.Length == 0)
            {
                shouldBackup = true; // No backups exist
            }
            else
            {
                var lastBackup = new FileInfo(existingBackups[0]);
                var currentSize = sourceInfo.Length;
                var lastSize = lastBackup.Length;

                if (currentSize > lastSize)
                {
                    // Size increased - definitely backup
                    shouldBackup = true;
                }
                else if (currentSize < lastSize)
                {
                    // Size decreased - still backup but keep the larger one too
                    shouldBackup = true;
                }
                else
                {
                    // Same size - check if content changed
                    var currentHash = GetFileHash(sourceFile);
                    var lastHash = GetFileHash(existingBackups[0]);
                    shouldBackup = currentHash != lastHash;
                }
            }

            if (shouldBackup)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(BackupDir, $"{baseName}_{timestamp}.json");
                File.Copy(sourceFile, backupPath, true);

                // Clean up old backups - keep last 20
                CleanOldBackups(baseName, 20);
            }
        }
        catch { }
    }

    private static string GetFileHash(string filePath)
    {
        try
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        catch
        {
            return "";
        }
    }

    private static void CleanOldBackups(string baseName, int keepCount)
    {
        try
        {
            var backups = Directory.GetFiles(BackupDir, $"{baseName}_*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToArray();

            for (int i = keepCount; i < backups.Length; i++)
            {
                try { File.Delete(backups[i]); } catch { }
            }
        }
        catch { }
    }

    private static void BackupAndRestore()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Backup & Restore");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine($"Backup Location: {BackupDir}");
            Console.WriteLine();

            // Show backup stats
            try
            {
                var catalogBackups = Directory.GetFiles(BackupDir, "catalog_*.json");
                var emulatorBackups = Directory.GetFiles(BackupDir, "emulators_*.json");
                
                Console.WriteLine($"Catalog Backups: {catalogBackups.Length}");
                if (catalogBackups.Length > 0)
                {
                    var latest = catalogBackups.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                    Console.WriteLine($"  Latest: {Path.GetFileName(latest)}");
                    Console.WriteLine($"  Date: {File.GetLastWriteTime(latest)}");
                }
                Console.WriteLine();

                Console.WriteLine($"Emulator Backups: {emulatorBackups.Length}");
                if (emulatorBackups.Length > 0)
                {
                    var latest = emulatorBackups.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                    Console.WriteLine($"  Latest: {Path.GetFileName(latest)}");
                    Console.WriteLine($"  Date: {File.GetLastWriteTime(latest)}");
                }
                Console.WriteLine();
            }
            catch { }

            Console.WriteLine("1. Create Backup Now");
            Console.WriteLine("2. Restore from Recent Backup");
            Console.WriteLine("3. Restore from File");
            Console.WriteLine("4. Open Backup Folder");
            Console.WriteLine("5. Back to Main Menu");
            Console.WriteLine();
            Console.Write("Select option (1-5): ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    CreateBackupNow();
                    break;
                case "2":
                    RestoreFromRecent();
                    break;
                case "3":
                    RestoreFromFile();
                    break;
                case "4":
                    OpenBackupFolder();
                    break;
                case "5":
                    return;
            }
        }
    }

    private static void CreateBackupNow()
    {
        Console.WriteLine();
        Console.WriteLine("Creating backup...");
        
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            if (File.Exists(CatalogFile))
            {
                var backupPath = Path.Combine(BackupDir, $"catalog_{timestamp}.json");
                File.Copy(CatalogFile, backupPath, true);
                Console.WriteLine($"✓ Catalog backed up: {Path.GetFileName(backupPath)}");
            }

            if (File.Exists(EmulatorsFile))
            {
                var backupPath = Path.Combine(BackupDir, $"emulators_{timestamp}.json");
                File.Copy(EmulatorsFile, backupPath, true);
                Console.WriteLine($"✓ Emulators backed up: {Path.GetFileName(backupPath)}");
            }

            Console.WriteLine();
            Console.WriteLine("Backup complete!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }

        System.Threading.Thread.Sleep(2000);
    }

    private static void RestoreFromRecent()
    {
        Console.Clear();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Restore from Recent Backup");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        try
        {
            // Show recent backups
            var catalogBackups = Directory.GetFiles(BackupDir, "catalog_*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Take(5)
                .ToArray();

            var emulatorBackups = Directory.GetFiles(BackupDir, "emulators_*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Take(5)
                .ToArray();

            if (catalogBackups.Length == 0 && emulatorBackups.Length == 0)
            {
                Console.WriteLine("No backups found!");
                System.Threading.Thread.Sleep(2000);
                return;
            }

            Console.WriteLine("Recent Catalog Backups:");
            for (int i = 0; i < catalogBackups.Length; i++)
            {
                var info = new FileInfo(catalogBackups[i]);
                Console.WriteLine($"  {i + 1}. {Path.GetFileName(catalogBackups[i])} ({info.Length} bytes, {info.LastWriteTime})");
            }
            Console.WriteLine();

            Console.WriteLine("Recent Emulator Backups:");
            for (int i = 0; i < emulatorBackups.Length; i++)
            {
                var info = new FileInfo(emulatorBackups[i]);
                Console.WriteLine($"  {i + 1}. {Path.GetFileName(emulatorBackups[i])} ({info.Length} bytes, {info.LastWriteTime})");
            }
            Console.WriteLine();

            Console.WriteLine("Restore Options:");
            Console.WriteLine("1. Restore Both (most recent)");
            Console.WriteLine("2. Restore Catalog Only");
            Console.WriteLine("3. Restore Emulators Only");
            Console.WriteLine("4. Cancel");
            Console.WriteLine();
            Console.Write("Select option (1-4): ");

            string choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    if (catalogBackups.Length > 0)
                    {
                        File.Copy(catalogBackups[0], CatalogFile, true);
                        Console.WriteLine($"✓ Restored catalog from: {Path.GetFileName(catalogBackups[0])}");
                    }
                    if (emulatorBackups.Length > 0)
                    {
                        File.Copy(emulatorBackups[0], EmulatorsFile, true);
                        Console.WriteLine($"✓ Restored emulators from: {Path.GetFileName(emulatorBackups[0])}");
                    }
                    LoadAllData(); // Reload
                    break;
                case "2":
                    if (catalogBackups.Length > 0)
                    {
                        File.Copy(catalogBackups[0], CatalogFile, true);
                        Console.WriteLine($"✓ Restored catalog from: {Path.GetFileName(catalogBackups[0])}");
                        LoadAllData();
                    }
                    break;
                case "3":
                    if (emulatorBackups.Length > 0)
                    {
                        File.Copy(emulatorBackups[0], EmulatorsFile, true);
                        Console.WriteLine($"✓ Restored emulators from: {Path.GetFileName(emulatorBackups[0])}");
                        LoadAllData();
                    }
                    break;
            }

            if (choice != "4")
            {
                Console.WriteLine();
                Console.WriteLine("Restore complete!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }

        System.Threading.Thread.Sleep(2000);
    }

    private static void RestoreFromFile()
    {
        Console.Clear();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Restore from File");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine("Enter the full path to the backup file to restore:");
        Console.Write("> ");
        string filePath = (Console.ReadLine() ?? "").Trim().Trim('"');

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("File not found or invalid path.");
            System.Threading.Thread.Sleep(2000);
            return;
        }

        try
        {
            var fileName = Path.GetFileName(filePath).ToLower();
            
            if (fileName.Contains("catalog"))
            {
                File.Copy(filePath, CatalogFile, true);
                Console.WriteLine($"✓ Restored catalog from: {fileName}");
                LoadAllData();
            }
            else if (fileName.Contains("emulator"))
            {
                File.Copy(filePath, EmulatorsFile, true);
                Console.WriteLine($"✓ Restored emulators from: {fileName}");
                LoadAllData();
            }
            else
            {
                Console.WriteLine("Could not determine file type. File name should contain 'catalog' or 'emulator'.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }

        System.Threading.Thread.Sleep(2000);
    }

    private static void OpenBackupFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = BackupDir,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            System.Threading.Thread.Sleep(2000);
        }
    }
}
