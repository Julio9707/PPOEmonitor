using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

class Program
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0; // 隱藏視窗

    static void Main()
    {
        // 隱藏 Console 視窗
        IntPtr hWnd = GetConsoleWindow();
        if (hWnd != IntPtr.Zero)
        {
            ShowWindow(hWnd, SW_HIDE);
        }

        Log("程式啟動...");

        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hinet.bat");

        while (true)
        {
            if (!IsPPPoEConnected())
            {
                Log("偵測到 PPPoE 連線中斷，執行 hinet.bat");
                RunBatScript(scriptPath);
                Thread.Sleep(180000); // 等待 3 分鐘

                if (!IsPPPoEConnected()) // 3 分鐘後再次檢測
                {
                    Log("3 分鐘後仍未連線，重新執行 hinet.bat");
                    RunBatScript(scriptPath);
                }
            }
            else
            {
                Log("PPPoE 連線正常");
            }
            Thread.Sleep(300000); // 每 5 分鐘檢測一次
        }
    }

    static bool IsPPPoEConnected()
    {
        var pppoeInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.NetworkInterfaceType == NetworkInterfaceType.Ppp);

        bool isConnected = pppoeInterfaces.Any(n => n.OperationalStatus == OperationalStatus.Up);
        Log($"PPPoE 連線狀態: {(isConnected ? "連線中" : "未連線")}");
        return isConnected;
    }

    static void RunBatScript(string scriptPath)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true // 隱藏 CMD 視窗
            };

            Process process = new Process { StartInfo = psi };
            process.Start();
            Log($"執行 {scriptPath}");
        }
        catch (Exception ex)
        {
            Log($"執行 {scriptPath} 發生錯誤: {ex.Message}");
        }
    }

    static void Log(string message)
    {
        try
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log"); // 建立 log 資料夾
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            string logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log"); // 按日期建立檔案
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            File.AppendAllText(logFile, logMessage + Environment.NewLine);
        }
        catch { } // 忽略錯誤，避免影響程式執行
    }
}
