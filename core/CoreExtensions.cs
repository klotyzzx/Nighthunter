using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NightHunterV2.core
{
    public static class CoreExtensions
    {
        public static string GetLastErrorMessage()
        {
            int code = Marshal.GetLastWin32Error();
            return code == 0 ? "STATUS_OK" : $"[0x{code:X}] {new Win32Exception(code).Message}";
        }

        public static string ParseNtStatus(int status)
        {
            switch ((uint)status)
            {
                case 0x00000000: return "STATUS_SUCCESS";
                case 0xC0000005: return "STATUS_ACCESS_VIOLATION";
                case 0xC0000008: return "STATUS_INVALID_HANDLE";
                case 0xC0000022: return "STATUS_ACCESS_DENIED";
                default: return $"NT_STATUS_0x{status:X8}";
            }
        }

        public static void CheckResult(bool success, string op)
        {
            if (success) return;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {op} -> FAIL: {GetLastErrorMessage()}");
            Console.ResetColor();
        }
    }
}