using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Threading.Tasks;

class RFMediaLinkConfigurator
{
    private static string ConfigDir;
    private static string ConfigFile;
    private static string CatalogFile;
    private static string EmulatorsFile;
    
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
        FindConfigDir();
        LoadAllData();
        SetupCatalogWatcher();
        MainMenu();
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
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  RF Media Link Configuration Tool");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine($"Config Location: {ConfigDir}");
            Console.WriteLine();
            Console.WriteLine("1. Manage Tags");
            Console.WriteLine("2. View Emulators");
            Console.WriteLine("3. Settings");
            Console.WriteLine("4. Exit");
            Console.WriteLine();
            Console.Write("Select option (1-4): ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    ManageTags();
                    break;
                case "2":
                    ViewEmulators();
                    break;
                case "3":
                    Settings();
                    break;
                case "4":
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
        if (Catalog.ValueKind == JsonValueKind.Object)
        {
            var tagList = new List<(string id, string name)>();
            foreach (var tag in Catalog.EnumerateObject())
            {
                var tagName = tag.Value.TryGetProperty("name", out var tn) ? tn.GetString() : "Unknown";
                tagList.Add((tag.Name, tagName));
            }

            if (tagList.Count > 0)
            {
                Console.WriteLine("Current tags:");
                foreach (var (id, name) in tagList)
                {
                    Console.WriteLine($"  {id} - {name}");
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

        Console.Write("Enter Tag UID to edit: ");
        string uid = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrEmpty(uid))
        {
            Console.WriteLine("Cancelled.");
            System.Threading.Thread.Sleep(1000);
            return;
        }

        // Check if tag exists (case-insensitive)
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
                
                string choice = Console.ReadLine() ?? "";
                if (!string.IsNullOrWhiteSpace(choice) && int.TryParse(choice, out int idx) && idx > 0 && idx <= emuList.Count)
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
                            string input = Console.ReadLine() ?? "";
                            
                            if (input.Trim().ToUpper() == "B")
                            {
                                value = BrowseForFile();
                                if (string.IsNullOrEmpty(value))
                                    value = existingVal;
                            }
                            else if (string.IsNullOrWhiteSpace(input))
                            {
                                value = existingVal;
                            }
                            else
                            {
                                value = input;
                            }
                        }
                        else
                        {
                            Console.Write("  New value (or Enter to keep current): ");
                            string input = Console.ReadLine() ?? "";
                            value = string.IsNullOrWhiteSpace(input) ? existingVal : input;
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
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Manage RFID Tags");
            Console.WriteLine("═══════════════════════════════════════════════════════");
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
                    Console.WriteLine($"{count}. [{tag.Name}] {name}");
                }
            }

            if (tagList.Count == 0)
            {
                Console.WriteLine("(No tags configured)");
            }

            Console.WriteLine();
            Console.WriteLine("A. Add Tag");
            Console.WriteLine("E. Edit Tag");
            Console.WriteLine("D. Delete Tag");
            Console.WriteLine("B. Back to Menu");
            Console.WriteLine();
            Console.Write("Select option (A/E/D/B): ");

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
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Add RFID Tag");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();
        
        // Step 1: Get UID (default to scanning)
        Console.WriteLine("Waiting for scan (place tag on reader)...");
        Console.WriteLine("Press Enter to enter UID manually.");
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
        if (Catalog.ValueKind == JsonValueKind.Object)
        {
            var tagList = new List<(string id, string name)>();
            foreach (var tag in Catalog.EnumerateObject())
            {
                var name = tag.Value.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                tagList.Add((tag.Name, name));
            }

            if (tagList.Count > 0)
            {
                Console.WriteLine("Current tags:");
                foreach (var (id, name) in tagList)
                {
                    Console.WriteLine($"  {id} - {name}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("(No tags configured)");
                Console.WriteLine();
            }
        }

        Console.Write("Enter Tag UID to delete: ");
        string uid = Console.ReadLine() ?? "";

        if (string.IsNullOrEmpty(uid))
        {
            Console.WriteLine("Cancelled.");
        }
        else
        {
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
                
                // Update in-memory catalog
                CatalogDoc?.Dispose();
                CatalogDoc = JsonDocument.Parse(jsonString);
                
                Console.WriteLine("Tag deleted!");
            }
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
}
