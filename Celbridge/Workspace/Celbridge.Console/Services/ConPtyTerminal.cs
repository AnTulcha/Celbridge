using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Celbridge.Terminal;

public sealed class ConPtyTerminal : IDisposable
{
    private IntPtr _pseudoConsoleHandle = IntPtr.Zero;
    private IntPtr _hInputWrite;
    private IntPtr _hOutputRead;

    private Process? _childProcess;
    private Task? _outputReaderTask;

    public event EventHandler<string>? OutputReceived;

    public void Start(string commandLine, int width = 80, int height = 25)
    {
        var sa = new SECURITY_ATTRIBUTES
        {
            nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
            bInheritHandle = true
        };

        CreatePipe(out var hInputRead, out _hInputWrite, ref sa, 0);
        CreatePipe(out _hOutputRead, out var hOutputWrite, ref sa, 0);

        var size = new COORD((short)width, (short)height);
        var result = CreatePseudoConsole(size, hInputRead, hOutputWrite, 0, out _pseudoConsoleHandle);
        if (result != 0)
            throw new Win32Exception(result, "CreatePseudoConsole failed");

        CloseHandle(hInputRead);
        CloseHandle(hOutputWrite);

        var siEx = new STARTUPINFOEX();
        siEx.StartupInfo.cb = Marshal.SizeOf(siEx);
        IntPtr lpSize = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
        siEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);
        InitializeProcThreadAttributeList(siEx.lpAttributeList, 1, 0, ref lpSize);

        UpdateProcThreadAttribute(
            siEx.lpAttributeList,
            0,
            (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
            _pseudoConsoleHandle,
            (IntPtr)IntPtr.Size,
            IntPtr.Zero,
            IntPtr.Zero);

        var pi = new PROCESS_INFORMATION();
        bool success = CreateProcess(
            null,
            new StringBuilder(commandLine),
            IntPtr.Zero,
            IntPtr.Zero,
            true,
            EXTENDED_STARTUPINFO_PRESENT,
            IntPtr.Zero,
            null,
            ref siEx,
            out pi);

        if (!success)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcess failed");

        _childProcess = Process.GetProcessById(pi.dwProcessId);

        _outputReaderTask = Task.Run(ReadOutputLoop);
    }

    private async Task ReadOutputLoop()
    {
        var buffer = new byte[4096];
        using var reader = new FileStream(_hOutputRead, FileAccess.Read);
        while (true)
        {
            int bytesRead = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (bytesRead == 0) break;
            string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            OutputReceived?.Invoke(this, text);
        }
    }

    public void Write(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        WriteFile(_hInputWrite, bytes, bytes.Length, out _, IntPtr.Zero);
    }

    public void Dispose()
    {
        _childProcess?.Kill();
        _childProcess?.Dispose();

        if (_pseudoConsoleHandle != IntPtr.Zero)
            ClosePseudoConsole(_pseudoConsoleHandle);

        if (_hInputWrite != IntPtr.Zero)
            CloseHandle(_hInputWrite);
        if (_hOutputRead != IntPtr.Zero)
            CloseHandle(_hOutputRead);
    }

    #region Win32 Interop

    private const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    private const int EXTENDED_STARTUPINFO_PRESENT = 0x00080000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD size, IntPtr hInput, IntPtr hOutput, uint dwFlags, out IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(
        out IntPtr hReadPipe,
        out IntPtr hWritePipe,
        ref SECURITY_ATTRIBUTES lpPipeAttributes,
        int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, int nNumberOfBytesToWrite,
        out int lpNumberOfBytesWritten, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreateProcess(
        string? lpApplicationName,
        StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        int dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        [In] ref STARTUPINFOEX lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(
        IntPtr lpAttributeList,
        int dwAttributeCount,
        int dwFlags,
        ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(
        IntPtr lpAttributeList,
        uint dwFlags,
        IntPtr attribute,
        IntPtr lpValue,
        IntPtr cbSize,
        IntPtr lpPreviousValue,
        IntPtr lpReturnSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;

        public COORD(short x, short y) { X = x; Y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
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
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    #endregion
}
