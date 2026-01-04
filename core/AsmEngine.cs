using System;
using System.Runtime.InteropServices;

namespace NightHunterV2.core
{
    public static class AsmEngine
    {
        private static readonly byte[] x64_Stub = { 0x4C, 0x8B, 0xD1, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x05, 0xC3 };

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static T CreateSyscall<T>(string functionName) where T : Delegate
        {
            if (IntPtr.Size != 8)
                return null;

            IntPtr ntdll = GetModuleHandle("ntdll.dll");
            IntPtr procAddr = GetProcAddress(ntdll, functionName);

            if (procAddr == IntPtr.Zero)
                return null;

            int id = Marshal.ReadInt32(procAddr, 4);
            byte[] shellcode = (byte[])x64_Stub.Clone();
            Buffer.BlockCopy(BitConverter.GetBytes(id), 0, shellcode, 4, 4);

            IntPtr buffer = NativeMethods.VirtualAlloc(
                IntPtr.Zero,
                (uint)shellcode.Length,
                NativeMethods.MemoryFlags.MEM_COMMIT | NativeMethods.MemoryFlags.MEM_RESERVE,
                NativeMethods.MemoryFlags.PAGE_EXECUTE_READWRITE
            );

            if (buffer == IntPtr.Zero)
                return null;

            Marshal.Copy(shellcode, 0, buffer, shellcode.Length);
            return Marshal.GetDelegateForFunctionPointer<T>(buffer);
        }
    }
}