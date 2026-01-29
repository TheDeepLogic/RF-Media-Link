using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RFMediaLinkService
{
    /// <summary>
    /// Helper class to activate a window by process ID.
    /// This runs as a separate entry point to overcome Windows focus stealing prevention.
    /// When called from a freshly spawned process, Windows is more likely to allow focus changes.
    /// </summary>
    public static class ActivateWindow
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
        
        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        
        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
        
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
        private const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;
        private const uint SPIF_SENDCHANGE = 0x0002;

        public static void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: RFMediaLinkService.exe activate <processId>");
                return;
            }

            if (!int.TryParse(args[0], out int processId))
            {
                Console.WriteLine($"Invalid process ID: {args[0]}");
                return;
            }

            // Try multiple times with delays
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Thread.Sleep(200 + (attempt * 150)); // 200, 350, 500, 650, 800, 950, 1100, 1250
                
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(processId);
                    if (process.HasExited)
                    {
                        Console.WriteLine("Process has exited");
                        return;
                    }

                    process.Refresh();
                    var handle = process.MainWindowHandle;
                    
                    if (handle == IntPtr.Zero)
                    {
                        // Window not ready yet
                        continue;
                    }

                    // Temporarily disable foreground lock timeout
                    IntPtr timeout = IntPtr.Zero;
                    SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, SPIF_SENDCHANGE);

                    // Force window activation
                    ForceWindowToForeground(handle);
                    
                    Console.WriteLine($"Activated window (attempt {attempt + 1})");
                    
                    // If this is attempt 3 or later, we succeeded, so exit
                    if (attempt >= 2)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                }
            }
        }

        private static void ForceWindowToForeground(IntPtr hWnd)
        {
            uint foregroundThreadId = 0;
            uint currentThreadId = GetCurrentThreadId();
            IntPtr foregroundWindow = GetForegroundWindow();
            
            if (foregroundWindow != IntPtr.Zero)
            {
                foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            }
            
            // Attach thread input
            if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }
            
            // Restore if minimized
            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
            }
            ShowWindow(hWnd, SW_SHOW);
            
            // Make topmost temporarily
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            
            // Bring to foreground
            BringWindowToTop(hWnd);
            SetForegroundWindow(hWnd);
            
            // Flash the window to draw attention
            Thread.Sleep(50);
            
            // Remove topmost
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            
            // One more SetForegroundWindow for good measure
            SetForegroundWindow(hWnd);
            
            // Detach thread input
            if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }
}
