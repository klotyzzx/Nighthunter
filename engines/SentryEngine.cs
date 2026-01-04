using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using NightHunterV2.core;

namespace NightHunterV2.engines
{
    public class SentryEngine
    {
        private readonly string[] _signatures = {
            "dnspy", "x64dbg", "x32dbg", "ollydbg", "windbg", "ida64", "idag", "ghidra",
            "de4dot", "dotpeek", "ilspy", "hiew", "cheatengine", "cheat engine", "scylla",
            "reclass", "processhacker", "process hacker", "systeminformer", "procexp",
            "wireshark", "fiddler", "httpdebugger", "http debugger", "charles",
            "burpsuite", "detect it easy", "die.exe", "resource hacker", "reshacker"
        };

        public List<ProcessMetadata> ActiveTargets { get; private set; } = new List<ProcessMetadata>();

        public struct ProcessMetadata
        {
            public int Pid;
            public string Name;
            public IntPtr WindowHandle;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int NtWriteVM(IntPtr h, IntPtr addr, byte[] buf, uint size, out IntPtr written);

        public SentryEngine()
        {
            AdjustProcessPrivileges();
        }

        private void AdjustProcessPrivileges()
        {
            if (NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), 0x0028, out IntPtr hToken))
            {
                if (NativeMethods.LookupPrivilegeValue(null, "SeDebugPrivilege", out long luid))
                {
                    var tp = new NativeMethods.TOKEN_PRIVILEGES
                    {
                        PrivilegeCount = 1,
                        Luid = luid,
                        Attributes = 0x00000002
                    };
                    NativeMethods.AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                }
                NativeMethods.CloseHandle(hToken);
            }
        }

        public void RefreshTargets()
        {
            var detected = new List<ProcessMetadata>();
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.Id <= 4 || proc.HasExited) continue;

                    string n = proc.ProcessName.ToLower();
                    string t = (proc.MainWindowHandle != IntPtr.Zero) ? proc.MainWindowTitle.ToLower() : "";

                    if (_signatures.Any(s => n.Contains(s) || t.Contains(s)) || MemoryPatternMatch(proc.Id))
                    {
                        detected.Add(new ProcessMetadata
                        {
                            Pid = proc.Id,
                            Name = proc.ProcessName,
                            WindowHandle = proc.MainWindowHandle
                        });
                    }
                }
                catch { continue; }
            }
            ActiveTargets = detected;
        }

        private bool MemoryPatternMatch(int pid)
        {
            using (var h = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.VMRead | NativeMethods.ProcessAccessFlags.QueryInformation, false, pid))
            {
                if (h.IsInvalid) return false;
                byte[] buffer = new byte[2048];
                if (NativeMethods.ReadProcessMemory(h, (IntPtr)0x400000, buffer, buffer.Length, out _))
                {
                    string dump = Encoding.UTF8.GetString(buffer).ToLower();
                    return _signatures.Any(dump.Contains);
                }
            }
            return false;
        }

        public bool RenameInPEB(int pid, string fakeName)
        {
            using (var h = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, pid))
            {
                if (h.IsInvalid) return false;
                var pbi = new NativeMethods.PROCESS_BASIC_INFORMATION();
                if (NativeMethods.NtQueryInformationProcess(h, 0, ref pbi, Marshal.SizeOf(pbi), out _) != 0) return false;

                int pOffset = IntPtr.Size == 8 ? 0x20 : 0x10;
                int bOffset = IntPtr.Size == 8 ? 0x68 : 0x40;

                IntPtr procParams = ReadPtr(h, pbi.PebBaseAddress + pOffset);
                IntPtr bufferAddr = ReadPtr(h, procParams + bOffset);

                byte[] payload = Encoding.Unicode.GetBytes($@"C:\Windows\System32\{fakeName}.exe" + "\0");
                return NativeMethods.WriteProcessMemory(h, bufferAddr, payload, payload.Length, out _);
            }
        }

        public bool HardCloakASM(int pid)
        {
            try
            {
                var syscall = AsmEngine.CreateSyscall<NtWriteVM>("NtWriteVirtualMemory");
                using (var h = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, pid))
                {
                    if (h.IsInvalid) return false;
                    byte[] payload = new byte[128];
                    new Random().NextBytes(payload);
                    return syscall(h.DangerousGetHandle(), (IntPtr)0x400000, payload, (uint)payload.Length, out _) == 0;
                }
            }
            catch { return false; }
        }

        public bool SpoofEAC(int pid)
        {
            using (var proc = Process.GetProcessById(pid))
            using (var h = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, pid))
            {
                if (h.IsInvalid) return false;
                IntPtr @base = proc.MainModule.BaseAddress;
                if (NativeMethods.VirtualProtectEx(h, @base, (UIntPtr)4096, 0x40, out uint old))
                {
                    byte[] noise = new byte[256];
                    new Random().NextBytes(noise);
                    bool res = NativeMethods.WriteProcessMemory(h, @base, noise, noise.Length, out _);
                    NativeMethods.VirtualProtectEx(h, @base, (UIntPtr)4096, old, out _);
                    return res;
                }
                return false;
            }
        }

        private IntPtr ReadPtr(NativeMethods.SafeProcessHandle h, IntPtr addr)
        {
            byte[] b = new byte[IntPtr.Size];
            NativeMethods.ReadProcessMemory(h, addr, b, b.Length, out _);
            return (IntPtr.Size == 8) ? (IntPtr)BitConverter.ToInt64(b, 0) : (IntPtr)BitConverter.ToInt32(b, 0);
        }

        public void MaskWindow(IntPtr hWnd) => NativeMethods.SetWindowText(hWnd, "Service Host: Local System");

        public void Kill(int pid) { try { Process.GetProcessById(pid).Kill(); } catch { } }

        public void DisableQuickEdit()
        {
            IntPtr hIn = NativeMethods.GetStdHandle(-10);
            if (NativeMethods.GetConsoleMode(hIn, out uint m))
                NativeMethods.SetConsoleMode(hIn, (m & ~0x0040u) | 0x0080u);
        }
    }
}