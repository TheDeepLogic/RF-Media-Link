using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;

namespace RFMediaLinkService;

// Data models
public class Config
{
    [JsonPropertyName("serial_port")]
    public string? SerialPort { get; set; }

    [JsonPropertyName("baud_rate")]
    public int BaudRate { get; set; } = 115200;

    [JsonPropertyName("default_emulator")]
    public string? DefaultEmulator { get; set; }
}

public class CatalogEntry
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("action_type")]
    public string? ActionType { get; set; }

    [JsonPropertyName("action_target")]
    public string? ActionTarget { get; set; }

    [JsonPropertyName("action_args")]
    public Dictionary<string, string>? ActionArgs { get; set; }
}

public class Emulator
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("executable")]
    public string? Executable { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("close_on_launch")]
    public CloseOnLaunchSettings? CloseOnLaunch { get; set; }

    [JsonPropertyName("arguments")]
    public List<EmulatorArgument>? Arguments { get; set; }
}

public class EmulatorArgument
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("flag")]
    public string? Flag { get; set; }

    [JsonPropertyName("choices")]
    public List<string>? Choices { get; set; }

    [JsonPropertyName("default")]
    public JsonElement? Default { get; set; }

    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class CloseOnLaunchSettings
{
    [JsonPropertyName("close_other_instances")]
    public bool CloseOtherInstances { get; set; }

    [JsonPropertyName("close_other_emulators")]
    public bool CloseOtherEmulators { get; set; }
}

public class RfidWorker : BackgroundService
{
    // Windows API declarations for window activation
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);
    
    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;
    private const byte VK_MENU = 0x12; // ALT key
    private const uint KEYEVENTF_KEYUP = 0x0002;
    
    private readonly ILogger<RfidWorker> _logger;
    private SerialPort? _serialPort;
    private string? _basePath;
    private Dictionary<string, CatalogEntry>? _catalog;
    private Dictionary<string, Emulator>? _emulators;
    private Config? _config;
    private string _lineBuffer = "";
    private FileSystemWatcher? _catalogWatcher;
    private System.Threading.Timer? _catalogReloadTimer;
    private bool _isSerialPortConnected = false;
    private DateTime _lastConnectionAttempt = DateTime.MinValue;
    private const int RECONNECT_INTERVAL_SECONDS = 5;

    public RfidWorker(ILogger<RfidWorker> logger)
    {
        _logger = logger;
    }

    private void ShowNotification(string title, string message, bool isError = false)
    {
        try
        {
            // Escape for PowerShell
            var escapedTitle = title.Replace("'", "''").Replace("`", "``");
            var escapedMessage = message.Replace("'", "''").Replace("`", "``");
            var icon = isError ? "Error" : "Warning";
            
            // Use MessageBox with TopMost form via PowerShell - works from service context
            // For tag not found, offer to launch configurator
            var msgScript = $@"
Add-Type -AssemblyName System.Windows.Forms
$form = New-Object System.Windows.Forms.Form
$form.TopMost = $true
$form.Visible = $false
$result = [System.Windows.Forms.MessageBox]::Show($form, '{escapedMessage}', '{escapedTitle}', 'YesNo', '{icon}')
if ($result -eq 'Yes') {{
    $configPath = Join-Path $env:ProgramData 'RFMediaLink\RFMediaLink.exe'
    if (Test-Path $configPath) {{
        Start-Process -FilePath $configPath
    }}
}}
$form.Dispose()
";
            
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{msgScript.Replace("\"", "`\"")}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(5000); // Wait up to 5 seconds for user response
                proc.Dispose();
            }
        }
        catch
        {
            // Silently fail if notifications don't work
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RF Media Link Service starting...");
        try
        {
            _logger.LogInformation($"Base directory: {AppDomain.CurrentDomain.BaseDirectory}");
            FindBasePath();
            _logger.LogInformation($"Config directory: {_basePath}");
            LoadConfiguration();
            _logger.LogInformation("Configuration loaded successfully");
            InitializeSerialPort();
            _logger.LogInformation("Serial port initialized");
            await base.StartAsync(cancellationToken);
            _logger.LogInformation("RF Media Link Service started successfully");
            
            // Schedule a delayed catalog reload to catch any changes made during startup
            // This ensures we pick up the latest catalog even if FileSystemWatcher missed events
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // Wait 2 seconds after startup
                _logger.LogInformation("Performing post-startup catalog reload...");
                ReloadCatalogFromDisk();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service");
            // Don't rethrow - let service continue running in degraded mode
        }
    }

    private void FindBasePath()
    {
        // Use ProgramData for service data - shared location accessible by all users
        var programData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RFMediaLink");
        
        if (Directory.Exists(programData))
        {
            _basePath = programData;
            _logger.LogInformation($"Using ProgramData path: {_basePath}");
            return;
        }

        // Fallback to other locations (for backward compatibility)
        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RFMediaLink"),
            @"C:\Program Files\RFMediaLink",
            AppDomain.CurrentDomain.BaseDirectory,
            Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? "",
            Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)) ?? ""
        };

        foreach (var path in paths)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, "config.json")))
            {
                _basePath = path;
                _logger.LogInformation($"Base path found (fallback): {_basePath}");
                return;
            }
        }

        // Last resort - use ProgramData even if it doesn't exist yet
        _basePath = programData;
        _logger.LogWarning($"Could not find config files, using default: {_basePath}");
        
        // Create the directory if it doesn't exist
        try
        {
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.LogInformation($"Created directory: {_basePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to create directory: {_basePath}");
        }
    }

    private void LoadConfiguration()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Load config.json
        try
        {
            var configPath = Path.Combine(_basePath!, "config.json");
            _logger.LogInformation($"Loading config from: {configPath}");
            if (!File.Exists(configPath))
            {
                _logger.LogWarning($"Config file not found: {configPath}, creating default");
                _config = new Config { SerialPort = "COM9", BaudRate = 115200 };
                // Create default config.json
                var defaultConfig = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, defaultConfig);
                _logger.LogInformation($"Created default config.json at {configPath}");
            }
            else
            {
                var configJson = File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<Config>(configJson, options);
                _logger.LogInformation($"Config loaded: SerialPort={_config?.SerialPort}, BaudRate={_config?.BaudRate}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading config.json");
            _config = new Config { SerialPort = "COM9", BaudRate = 115200 };
        }

        // Load catalog.json
        try
        {
            var catalogPath = Path.Combine(_basePath!, "catalog.json");
            _logger.LogInformation($"Loading catalog from: {catalogPath}");
            if (!File.Exists(catalogPath))
            {
                _logger.LogWarning($"Catalog file not found: {catalogPath}, creating empty catalog");
                _catalog = new();
                // Create empty catalog.json
                var defaultCatalog = "{}";
                File.WriteAllText(catalogPath, defaultCatalog);
                _logger.LogInformation($"Created empty catalog.json at {catalogPath}");
            }
            else
            {
                var catalogJson = File.ReadAllText(catalogPath);
                // Catalog is now a dict with UID as key, not an array
                var catalogDict = JsonSerializer.Deserialize<Dictionary<string, CatalogEntry>>(catalogJson, options);
                _catalog = catalogDict ?? new();
                _logger.LogInformation($"Catalog loaded: {_catalog.Count} entries");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading catalog.json");
            _catalog = new();
        }

        // Load emulators.json
        try
        {
            var emulatorsPath = Path.Combine(_basePath!, "emulators.json");
            _logger.LogInformation($"Loading emulators from: {emulatorsPath}");
            if (!File.Exists(emulatorsPath))
            {
                _logger.LogWarning($"Emulators file not found: {emulatorsPath}, creating empty emulators");
                _emulators = new();
                // Create empty emulators.json
                var defaultEmulators = "{}";
                File.WriteAllText(emulatorsPath, defaultEmulators);
                _logger.LogWarning($"Created empty emulators.json at {emulatorsPath}");
            }
            else
            {
                _logger.LogInformation($"emulators.json exists, reading file...");
                var emulatorsJson = File.ReadAllText(emulatorsPath);
                _logger.LogInformation($"File content length: {emulatorsJson.Length} characters");
                _emulators = JsonSerializer.Deserialize<Dictionary<string, Emulator>>(emulatorsJson, options) ?? new();
                _logger.LogInformation($"Emulators loaded: {_emulators.Count} emulators");
                if (_emulators.Count > 0)
                {
                    _logger.LogInformation($"Emulator names: {string.Join(", ", _emulators.Keys)}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading emulators.json");
            _emulators = new();
        }

        // Start watching config files for changes
        SetupFileWatchers();
    }

    private void SetupFileWatchers()
    {
        SetupCatalogWatcher();
        SetupEmulatorsWatcher();
    }

    private void SetupEmulatorsWatcher()
    {
        if (string.IsNullOrEmpty(_basePath))
            return;

        try
        {
            var emulatorsWatcher = new FileSystemWatcher(_basePath, "emulators.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                EnableRaisingEvents = false
            };

            emulatorsWatcher.Changed += (sender, e) =>
            {
                _logger.LogInformation("Emulators file change detected, reloading...");
                System.Threading.Thread.Sleep(100); // Brief delay to ensure file is written
                LoadEmulatorsFromDisk();
            };

            emulatorsWatcher.Created += (sender, e) =>
            {
                _logger.LogInformation("Emulators file created, loading...");
                System.Threading.Thread.Sleep(100);
                LoadEmulatorsFromDisk();
            };

            emulatorsWatcher.EnableRaisingEvents = true;
            _logger.LogInformation("Emulators file watcher started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup emulators watcher");
        }
    }

    private void LoadEmulatorsFromDisk()
    {
        try
        {
            var emulatorsPath = Path.Combine(_basePath!, "emulators.json");
            _logger.LogInformation($"LoadEmulatorsFromDisk: Loading from {emulatorsPath}");
            
            if (!File.Exists(emulatorsPath))
            {
                _logger.LogError($"Emulators file not found at: {emulatorsPath}");
                _emulators = new();
                return;
            }
            
            var emulatorsJson = File.ReadAllText(emulatorsPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _emulators = JsonSerializer.Deserialize<Dictionary<string, Emulator>>(emulatorsJson, options) ?? new();
            _logger.LogInformation($"LoadEmulatorsFromDisk: Loaded {_emulators.Count} emulators");
            
            if (_emulators.Count > 0)
            {
                _logger.LogInformation($"Available emulators: {string.Join(", ", _emulators.Keys)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading emulators from disk");
            _emulators = new();
        }
    }

    private void SetupCatalogWatcher()
    {
        if (string.IsNullOrEmpty(_basePath))
            return;

        try
        {
            _catalogWatcher = new FileSystemWatcher(_basePath, "catalog.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                EnableRaisingEvents = false  // Disable initially to avoid race conditions
            };

            _catalogWatcher.Changed += (sender, e) =>
            {
                _logger.LogInformation("Catalog file change detected");
                // Debounce: multiple change events fire rapidly
                _catalogReloadTimer?.Dispose();
                _catalogReloadTimer = new System.Threading.Timer(_ =>
                {
                    ReloadCatalogFromDisk();
                }, null, TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
            };

            _catalogWatcher.Created += (sender, e) =>
            {
                _logger.LogInformation("Catalog file created");
                // Debounce: multiple change events fire rapidly
                _catalogReloadTimer?.Dispose();
                _catalogReloadTimer = new System.Threading.Timer(_ =>
                {
                    ReloadCatalogFromDisk();
                }, null, TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
            };

            _catalogWatcher.EnableRaisingEvents = true;
            _logger.LogInformation("Catalog file watcher started");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not start catalog file watcher");
        }
    }

    private void ReloadCatalogFromDisk()
    {
        try
        {
            var catalogPath = Path.Combine(_basePath!, "catalog.json");
            if (!File.Exists(catalogPath))
                return;

            // Check if file is being processed (contains PROCESSING marker)
            string firstBytes = "";
            int attempts = 0;
            while (attempts < 20) // Wait up to 2 seconds
            {
                try
                {
                    using (var fs = File.Open(catalogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        firstBytes = reader.ReadLine() ?? "";
                    }
                    
                    if (firstBytes.Contains("PROCESSING"))
                    {
                        System.Threading.Thread.Sleep(100);
                        attempts++;
                        continue;
                    }
                    else
                    {
                        break; // File is ready
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                    attempts++;
                }
            }

            // Read the complete file
            string jsonContent = File.ReadAllText(catalogPath);
            
            // Parse and update catalog
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var newCatalog = JsonSerializer.Deserialize<Dictionary<string, CatalogEntry>>(jsonContent, options);
            
            if (newCatalog != null)
            {
                _catalog = newCatalog;
                _logger.LogInformation($"Catalog reloaded from disk: {_catalog.Count} tags");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error auto-reloading catalog");
        }
    }

    private bool InitializeSerialPort()
    {
        try
        {
            // Close existing port if open
            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort.DataReceived -= SerialPort_DataReceived;
                    _serialPort.Dispose();
                }
                catch { }
                _serialPort = null;
            }

            if (string.IsNullOrEmpty(_config?.SerialPort))
            {
                _logger.LogWarning("SerialPort not configured, running without RFID scanner");
                _isSerialPortConnected = false;
                return false;
            }

            _serialPort = new SerialPort(_config.SerialPort, _config.BaudRate, Parity.None, 8, StopBits.One);
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.ErrorReceived += SerialPort_ErrorReceived;
            _serialPort.Open();
            _logger.LogInformation($"Serial port opened: {_config.SerialPort} @ {_config.BaudRate} baud");
            _isSerialPortConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error initializing serial port {_config?.SerialPort}. Will retry...");
            _serialPort = null;
            _isSerialPortConnected = false;
            return false;
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            while (_serialPort.BytesToRead > 0)
            {
                char c = (char)_serialPort.ReadChar();
                if (c == '\n')
                {
                    ProcessLine(_lineBuffer.Trim());
                    _lineBuffer = "";
                }
                else if (c != '\r')
                {
                    _lineBuffer += c;
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Port was closed/disconnected
            _logger.LogWarning("Serial port disconnected during read operation");
            _isSerialPortConnected = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading serial data");
            // Check if error indicates disconnection
            if (ex is IOException || ex is TimeoutException)
            {
                _isSerialPortConnected = false;
            }
        }
    }

    private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        _logger.LogWarning($"Serial port error received: {e.EventType}");
        // Mark as disconnected so the monitor will try to reconnect
        _isSerialPortConnected = false;
    }

    private void ProcessLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        // Ignore "Type: RFID" lines, only process UID lines
        if (line.StartsWith("Type:"))
            return;

        if (line.StartsWith("UID: "))
        {
            var uid = line.Substring("UID: ".Length).Trim();
            ProcessRfidTag(uid);
        }
    }

    private void ProcessRfidTag(string uid)
    {
        // Catalog is now auto-reloaded by FileSystemWatcher when file changes
        // Just look up the UID in current _catalog

        CatalogEntry? entry = null;
        bool foundInCatalog = _catalog != null && _catalog.TryGetValue(uid, out entry);

        // Write scan files
        try
        {
            if (_basePath == null)
            {
                _logger.LogError("_basePath is null - cannot write scan file");
                return;
            }
            
            // Always write scan_last.log
            var scanLastFile = Path.Combine(_basePath, "scan_last.log");
            var content = $"{uid}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            File.WriteAllText(scanLastFile, content);
            
            // Only write scan_null.log if not found in catalog
            if (!foundInCatalog)
            {
                var scanNullFile = Path.Combine(_basePath, "scan_null.log");
                File.WriteAllText(scanNullFile, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to write scan file for {uid}");
        }
        
        if (!foundInCatalog || entry == null)
        {
            _logger.LogWarning($"Tag not found in catalog: {uid}");
            ShowNotification("Tag Not Found", $"Tag {uid} not in catalog.\n\nOpen Configure to add this tag?", false);
            return;
        }

        _logger.LogInformation($"Tag matched: {entry.Name} (Action: {entry.ActionType})");
        _logger.LogInformation($"Action type: '{entry.ActionType}', Target: '{entry.ActionTarget}'");

        try
        {
            var actionType = entry.ActionType?.ToLower();
            _logger.LogInformation($"Processing action type: {actionType}");
            
            switch (actionType)
            {
                case "emulator":
                    _logger.LogInformation($"Calling LaunchEmulator with emulator='{entry.ActionTarget}'");
                    LaunchEmulator(entry.ActionTarget, entry.ActionArgs);
                    break;
                case "file":
                    _logger.LogInformation($"Calling LaunchFile with file='{entry.ActionTarget}'");
                    LaunchFile(entry.ActionTarget);
                    break;
                case "url":
                    LaunchUrl(entry.ActionTarget);
                    break;
                case "command":
                    LaunchCommand(entry.ActionTarget);
                    break;
                default:
                    _logger.LogWarning($"Unknown action type: {entry.ActionType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing tag {uid}");
        }
    }

    private void LaunchEmulator(string? emulatorName, Dictionary<string, string>? args)
    {
        _logger.LogInformation($"LaunchEmulator called with: {emulatorName}");
        _logger.LogInformation($"_emulators is null: {_emulators == null}");
        _logger.LogInformation($"_emulators count: {_emulators?.Count ?? 0}");
        
        if (_emulators == null || _emulators.Count == 0)
        {
            _logger.LogWarning("Emulators not loaded, attempting to reload...");
            LoadEmulatorsFromDisk();
        }
        
        if (string.IsNullOrEmpty(emulatorName))
        {
            _logger.LogError($"Emulator name is null or empty");
            return;
        }
        
        if (_emulators == null || !_emulators.TryGetValue(emulatorName, out var emulator))
        {
            _logger.LogError($"Emulator not found: {emulatorName}");
            if (_emulators != null)
            {
                _logger.LogError($"Available emulators: {string.Join(", ", _emulators.Keys)}");
            }
            ShowNotification("Configuration Error", $"Emulator '{emulatorName}' not found", true);
            return;
        }

        _logger.LogInformation($"Emulator found! Name: {emulator.Name}, Executable: {emulator.Executable}");

        TerminateOtherEmulators(emulatorName, emulator);

        var cmdlineArgs = BuildArguments(emulator, args);
        _logger.LogInformation($"Launching {emulatorName} with args: {cmdlineArgs}");

        try
        {
            _logger.LogInformation($"About to start process: {emulator.Executable}");
            var psi = new ProcessStartInfo
            {
                FileName = emulator.Executable!,
                Arguments = cmdlineArgs,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            var process = Process.Start(psi);
            _logger.LogInformation($"Process.Start() called successfully!");
            
            // Use ALT+TAB simulation to bring window to foreground
            // This is more reliable than SetForegroundWindow and works without admin rights
            if (process != null && !process.HasExited)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // Wait for window to be created
                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            await Task.Delay(300);
                            process.Refresh();
                            
                            if (process.HasExited)
                                break;
                                
                            var handle = process.MainWindowHandle;
                            if (handle != IntPtr.Zero)
                            {
                                // Simulate ALT key press and release to allow SetForegroundWindow
                                keybd_event(VK_MENU, 0, 0, IntPtr.Zero);
                                SetForegroundWindow(handle);
                                ShowWindow(handle, SW_RESTORE);
                                ShowWindow(handle, SW_SHOW);
                                BringWindowToTop(handle);
                                SetForegroundWindow(handle);
                                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
                                
                                _logger.LogInformation($"Activated {emulatorName} window (attempt {attempt + 1})");
                                
                                // Try a few more times to be sure
                                if (attempt >= 2)
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not activate window");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error launching {emulatorName}");
            ShowNotification("Launch Error", $"Failed to launch {emulatorName}", true);
        }
    }

    private string BuildArguments(Emulator emulator, Dictionary<string, string>? argValues)
    {
        if (emulator.Arguments == null || argValues == null)
            return "";

        var args = new List<string>();

        foreach (var arg in emulator.Arguments)
        {
            if (string.IsNullOrEmpty(arg.Name))
                continue;

            if (!argValues.TryGetValue(arg.Name, out var value))
                continue;

            var type = arg.Type?.ToLower() ?? "flag";

            switch (type)
            {
                case "toggle":
                    if (!string.IsNullOrEmpty(value) && (value.ToLower() == "true" || value.ToLower() == "1"))
                    {
                        args.Add($"{arg.Flag}");
                    }
                    break;
                case "flag":
                case "choice":
                case "file":
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!string.IsNullOrEmpty(arg.Flag))
                        {
                            // Flag is specified, add flag then value
                            args.Add($"{arg.Flag}");
                            args.Add($"\"{value}\"");
                        }
                        else
                        {
                            // No flag specified, treat as positional argument
                            args.Add($"\"{value}\"");
                        }
                    }
                    break;
                case "positional":
                    if (!string.IsNullOrEmpty(value))
                    {
                        args.Add($"\"{value}\"");
                    }
                    break;
            }
        }

        return string.Join(" ", args);
    }

    private void TerminateOtherEmulators(string activeEmulator, Emulator emulator)
    {
        if (emulator.CloseOnLaunch == null)
            return;

        // Close other instances of the same emulator
        if (emulator.CloseOnLaunch.CloseOtherInstances)
        {
            TerminateEmulatorInstances(activeEmulator);
        }

        // Close all other emulators
        if (emulator.CloseOnLaunch.CloseOtherEmulators && _emulators != null)
        {
            foreach (var otherEmulatorName in _emulators.Keys)
            {
                if (otherEmulatorName != activeEmulator)
                {
                    TerminateEmulatorInstances(otherEmulatorName);
                }
            }
        }
    }

    private void TerminateEmulatorInstances(string emulatorName)
    {
        if (_emulators == null || !_emulators.TryGetValue(emulatorName, out var emulator))
            return;

        if (string.IsNullOrEmpty(emulator.Executable))
            return;

        var processName = Path.GetFileNameWithoutExtension(emulator.Executable);
        try
        {
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                proc.Kill();
                _logger.LogInformation($"Terminated {processName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error terminating {emulatorName}");
        }
    }

    private void LaunchFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            _logger.LogInformation($"Launched file: {path}");
            
            // If the file is an executable, try to bring it to focus
            var extension = Path.GetExtension(path)?.ToLower();
            if (extension == ".exe" || extension == ".bat" || extension == ".cmd")
            {
                // Use ALT+TAB simulation to bring window to foreground
                // This is more reliable than SetForegroundWindow and works without admin rights
                if (process != null && !process.HasExited)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Wait for window to be created
                            for (int attempt = 0; attempt < 10; attempt++)
                            {
                                await Task.Delay(300);
                                process.Refresh();
                                
                                if (process.HasExited)
                                    break;
                                    
                                var handle = process.MainWindowHandle;
                                if (handle != IntPtr.Zero)
                                {
                                    // Simulate ALT key press and release to allow SetForegroundWindow
                                    keybd_event(VK_MENU, 0, 0, IntPtr.Zero);
                                    SetForegroundWindow(handle);
                                    ShowWindow(handle, SW_RESTORE);
                                    ShowWindow(handle, SW_SHOW);
                                    BringWindowToTop(handle);
                                    SetForegroundWindow(handle);
                                    keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
                                    
                                    _logger.LogInformation($"Activated window for {Path.GetFileName(path)} (attempt {attempt + 1})");
                                    
                                    // Try a few more times to be sure
                                    if (attempt >= 2)
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not activate window");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error launching file: {path}");
        }
    }

    private void LaunchUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _logger.LogInformation($"Launched URL: {url}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error launching URL: {url}");
        }
    }

    private void LaunchCommand(string? command)
    {
        if (string.IsNullOrEmpty(command))
            return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = true
            };
            Process.Start(psi);
            _logger.LogInformation($"Launched command: {command}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error launching command: {command}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting serial port monitoring loop...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if serial port needs reconnection
                if (!_isSerialPortConnected && !string.IsNullOrEmpty(_config?.SerialPort))
                {
                    // Only attempt reconnect if enough time has passed
                    var timeSinceLastAttempt = DateTime.Now - _lastConnectionAttempt;
                    if (timeSinceLastAttempt.TotalSeconds >= RECONNECT_INTERVAL_SECONDS)
                    {
                        _lastConnectionAttempt = DateTime.Now;
                        _logger.LogInformation($"Attempting to reconnect to serial port {_config.SerialPort}...");
                        
                        if (InitializeSerialPort())
                        {
                            _logger.LogInformation("Serial port reconnected successfully!");
                        }
                    }
                }
                // Verify connection is still valid
                else if (_isSerialPortConnected && _serialPort != null)
                {
                    try
                    {
                        // Quick check if port is still open
                        if (!_serialPort.IsOpen)
                        {
                            _logger.LogWarning("Serial port connection lost (port closed)");
                            _isSerialPortConnected = false;
                        }
                    }
                    catch
                    {
                        _logger.LogWarning("Serial port connection lost (exception during check)");
                        _isSerialPortConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in serial port monitoring loop");
            }
            
            await Task.Delay(1000, stoppingToken);
        }
        
        _logger.LogInformation("Serial port monitoring loop stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RF Media Link Service stopping...");
        
        _isSerialPortConnected = false;
        
        if (_serialPort != null)
        {
            try
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.ErrorReceived -= SerialPort_ErrorReceived;
                _serialPort.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing serial port");
            }
            _serialPort = null;
        }
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("RF Media Link Service stopped");
    }
}
