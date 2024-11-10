using System;
using System.Runtime.InteropServices;

namespace FireBrowserWinUi3.Services;

public class ProcessStarter
{
    // Structures used by CreateProcess
    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    // P/Invoke for CreateProcess
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreateProcess(
        string lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    // Constants
    private const uint CREATE_NO_WINDOW = 0x08000000;

    // Method to start a process
    public static bool StartProcess(string filePath, string arguments = "", string workingDirectory = null)
    {
        var startupInfo = new STARTUPINFO();
        var processInfo = new PROCESS_INFORMATION();
        startupInfo.cb = Marshal.SizeOf(startupInfo);

        // Construct command line
        string commandLine = $"{filePath} {arguments}".Trim();

        // Attempt to create the process
        bool result = CreateProcess(
            null,                  // Application name
            commandLine,          // Command line
            IntPtr.Zero,          // Process attributes
            IntPtr.Zero,          // Thread attributes
            false,                // Inherit handles
            CREATE_NO_WINDOW,     // Creation flags
            IntPtr.Zero,          // Environment
            workingDirectory,     // Current directory
            ref startupInfo,      // Startup info
            out processInfo);     // Process information

        if (!result)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        // Close handles to the process and thread
        CloseHandle(processInfo.hProcess);
        CloseHandle(processInfo.hThread);

        return true;
    }

    // P/Invoke for CloseHandle
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
