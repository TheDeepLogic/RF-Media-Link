using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
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
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("emulators")]
    public List<string>? Emulators { get; set; }
}

public class RfidWorker : BackgroundService
{
    private readonly ILogger<RfidWorker> _logger;
    private SerialPort? _serialPort;
    private string? _basePath;
    private Dictionary<string, CatalogEntry>? _catalog;
    private Dictionary<string, Emulator>? _emulators;
    private Config? _config;
    private string _lineBuffer = "";
    private FileSystemWatcher? _catalogWatcher;
    private System.Threading.Timer? _catalogReloadTimer;

    public RfidWorker(ILogger<RfidWorker> logger)
    {
        _logger = logger;
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service");
            // Don't rethrow - let service continue running in degraded mode
        }
    }

    private void FindBasePath()
    {
        // ALWAYS use AppData for the service - that's where it's installed
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RFMediaLink");
        
        if (Directory.Exists(appData))
        {
            _basePath = appData;
            _logger.LogWarning($"DEBUG: Using AppData path: {_basePath}");
            return;
        }

        // Fallback to other locations
        var paths = new[]
        {
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

        // Last resort - use AppData even if it doesn't exist yet
        _basePath = appData;
        _logger.LogWarning($"Could not find config files, using default: {_basePath}");
    }

    private void LoadConfiguration()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var configPath = Path.Combine(_basePath!, "config.json");
            _logger.LogInformation($"Loading config from: {configPath}");
            if (!File.Exists(configPath))
            {
                _logger.LogWarning($"Config file not found: {configPath}");
                _config = new Config { SerialPort = "COM9", BaudRate = 115200 };
                return;
            }
            var configJson = File.ReadAllText(configPath);
            _config = JsonSerializer.Deserialize<Config>(configJson, options);
            _logger.LogInformation($"Config loaded: SerialPort={_config?.SerialPort}, BaudRate={_config?.BaudRate}");

            var catalogPath = Path.Combine(_basePath!, "catalog.json");
            _logger.LogInformation($"Loading catalog from: {catalogPath}");
            if (!File.Exists(catalogPath))
            {
                _logger.LogWarning($"Catalog file not found: {catalogPath}");
                _catalog = new();
                return;
            }
            var catalogJson = File.ReadAllText(catalogPath);
            // Catalog is now a dict with UID as key, not an array
            var catalogDict = JsonSerializer.Deserialize<Dictionary<string, CatalogEntry>>(catalogJson, options);
            _catalog = catalogDict ?? new();
            _logger.LogInformation($"Catalog loaded: {_catalog.Count} entries");

            var emulatorsPath = Path.Combine(_basePath!, "emulators.json");
            _logger.LogInformation($"Loading emulators from: {emulatorsPath}");
            if (!File.Exists(emulatorsPath))
            {
                _logger.LogWarning($"Emulators file not found: {emulatorsPath}");
                _emulators = new();
                return;
            }
            var emulatorsJson = File.ReadAllText(emulatorsPath);
            _emulators = JsonSerializer.Deserialize<Dictionary<string, Emulator>>(emulatorsJson, options) ?? new();
            _logger.LogInformation($"Emulators loaded: {_emulators.Count} emulators");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration, using defaults");
            _config = new Config { SerialPort = "COM9", BaudRate = 115200 };
            _catalog = new();
            _emulators = new();
        }

        // Start watching catalog.json for changes
        SetupCatalogWatcher();
    }

    private void SetupCatalogWatcher()
    {
        if (string.IsNullOrEmpty(_basePath))
            return;

        try
        {
            _catalogWatcher = new FileSystemWatcher(_basePath, "catalog.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = false  // Disable initially to avoid race conditions
            };

            _catalogWatcher.Changed += (sender, e) =>
            {
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

    private void InitializeSerialPort()
    {
        try
        {
            if (string.IsNullOrEmpty(_config?.SerialPort))
            {
                _logger.LogWarning("SerialPort not configured, running without RFID scanner");
                return;
            }

            _serialPort = new SerialPort(_config.SerialPort, _config.BaudRate, Parity.None, 8, StopBits.One);
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();
            _logger.LogInformation($"Serial port opened: {_config.SerialPort} @ {_config.BaudRate} baud");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error initializing serial port {_config?.SerialPort}. Service will run without RFID scanner.");
            _serialPort = null;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading serial data");
        }
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
        _logger.LogWarning($"DEBUG: ProcessRfidTag called with UID: {uid}");

        // Write to last_scan.txt for configure.py to pick up
        try
        {
            if (_basePath == null)
            {
                _logger.LogError("_basePath is null - cannot write scan file");
                return;
            }
            
            var scanFile = Path.Combine(_basePath, "last_scan.txt");
            _logger.LogWarning($"DEBUG: Writing to {scanFile}");
            
            var content = $"{uid}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            File.WriteAllText(scanFile, content);
            _logger.LogWarning($"DEBUG: Successfully wrote scan to {scanFile}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to write scan file for {uid}");
        }

        // Catalog is now auto-reloaded by FileSystemWatcher when file changes
        // Just look up the UID in current _catalog
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning($"DEBUG: Catalog has {_catalog?.Count ?? 0} entries");
            _logger.LogWarning($"DEBUG: Looking for UID: '{uid}'");
        }

        if (_catalog == null || !_catalog.TryGetValue(uid, out var entry))
        {
            _logger.LogWarning($"Tag not found in catalog: {uid}");
            return;
        }

        _logger.LogInformation($"Tag matched: {entry.Name} (Action: {entry.ActionType})");

        try
        {
            switch (entry.ActionType?.ToLower())
            {
                case "emulator":
                    LaunchEmulator(entry.ActionTarget, entry.ActionArgs);
                    break;
                case "file":
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
        if (string.IsNullOrEmpty(emulatorName) || _emulators == null || !_emulators.TryGetValue(emulatorName, out var emulator))
        {
            _logger.LogError($"Emulator not found: {emulatorName}");
            return;
        }

        TerminateOtherEmulators(emulatorName, emulator);

        var cmdlineArgs = BuildArguments(emulator, args);
        _logger.LogInformation($"Launching {emulatorName} with args: {cmdlineArgs}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = emulator.Executable!,
                Arguments = cmdlineArgs,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error launching {emulatorName}");
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
                    if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(arg.Flag))
                    {
                        args.Add($"{arg.Flag}");
                        args.Add($"\"{value}\"");
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
        if (emulator.CloseOnLaunch?.Enabled != true || emulator.CloseOnLaunch.Emulators == null)
            return;

        foreach (var toClose in emulator.CloseOnLaunch.Emulators)
        {
            TerminateEmulatorInstances(toClose);
        }
    }

    private void TerminateEmulatorInstances(string emulatorName)
    {
        if (_emulators == null || !_emulators.TryGetValue(emulatorName, out var emulator))
            return;

        var processName = Path.GetFileNameWithoutExtension(emulator.Path);
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
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            _logger.LogInformation($"Launched file: {path}");
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
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RF Media Link Service stopping...");
        _serialPort?.Dispose();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("RF Media Link Service stopped");
    }
}
