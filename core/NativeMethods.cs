using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NightHunterV2.core
{
    internal static class NativeMethods
    {
        public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeProcessHandle() : base(true) { }
            protected override bool ReleaseHandle() => CloseHandle(handle);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr Reserved3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public long Luid;
            public uint Attributes;
        }

        public static class MemoryFlags
        {
            public const uint MEM_COMMIT = 0x1000;
            public const uint MEM_RESERVE = 0x2000;
            public const uint PAGE_EXECUTE_READWRITE = 0x40;
            public const uint PAGE_READWRITE = 0x04;
        }

        public static class TokenPrivileges
        {
            public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
            public const uint TOKEN_ADJUST_PRIVILEGES = 0x00000020;
            public const uint TOKEN_QUERY = 0x00000008;
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            VMRead = 0x0010,
            VMWrite = 0x0020,
            VMOperation = 0x0008,
            QueryInformation = 0x0400,
            Synchronize = 0x00100000
        }

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(SafeProcessHandle processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeProcessHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(SafeProcessHandle hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(SafeProcessHandle hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(SafeProcessHandle hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        public static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);
    }
}