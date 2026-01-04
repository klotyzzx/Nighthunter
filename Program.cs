using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace NightHunter.Sentry
{
    internal static class NativeMethods
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtResumeProcess(IntPtr processHandle);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }

    public class SentryEngine
    {
        private readonly string[] _targetSignatures = { "dnspy", "cheatengine", "x64dbg", "processhacker", "httpdebugger", "fiddler", "wireshark", "debugger", "reclass" };
        private readonly string[] _exclusionList = { "devenv", "vsdebugconsole", "msbuild", "nighthunter" };
        private readonly int _currentPid = Process.GetCurrentProcess().Id;

        public List<ProcessMetadata> ActiveTargets { get; private set; } = new List<ProcessMetadata>();

        public struct ProcessMetadata
        {
            public int Pid;
            public string Name;
            public string Description;
            public IntPtr Handle;
        }

        public void RefreshTargets()
        {
            var detected = new List<ProcessMetadata>();
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.Id == _currentPid || _exclusionList.Any(x => proc.ProcessName.ToLower().Contains(x)))
                        continue;

                    string identity = (proc.ProcessName + (proc.MainModule?.FileVersionInfo.FileDescription ?? "")).ToLower();

                    if (_targetSignatures.Any(sig => identity.Contains(sig)))
                    {
                        detected.Add(new ProcessMetadata
                        {
                            Pid = proc.Id,
                            Name = proc.ProcessName,
                            Description = proc.MainModule?.FileVersionInfo.FileDescription ?? "N/A",
                            Handle = proc.Handle
                        });
                    }
                }
                catch { /* Quietly ignore access denied */ }
            }
            ActiveTargets = detected;
        }
    }

    class Program
    {
        private static readonly SentryEngine Engine = new SentryEngine();
        private static bool _autoEnforce = false;
        private const int PanicKey = 0x75; // F6

        static void Main()
        {
            if (!IsPrivileged())
            {
                Elevate();
                return;
            }

            Console.Title = "Runtime Broker Host";
            InitializeBackgroundTasks();

            int lastHash = 0;

            while (true)
            {
                Engine.RefreshTargets();

                if (_autoEnforce && Engine.ActiveTargets.Any())
                {
                    Engine.ActiveTargets.ForEach(t => Terminate(t.Pid));
                }

                int currentHash = string.Join(",", Engine.ActiveTargets.Select(t => t.Pid)).GetHashCode() ^ _autoEnforce.GetHashCode();

                if (currentHash != lastHash)
                {
                    RenderUI();
                    lastHash = currentHash;
                }

                HandleInput();
                Thread.Sleep(700);
            }
        }

        private static void InitializeBackgroundTasks()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (NativeMethods.GetAsyncKeyState(PanicKey) != 0)
                        Environment.Exit(0);
                    Thread.Sleep(100);
                }
            });
        }

        private static void HandleInput()
        {
            if (!Console.KeyAvailable) return;

            var key = Console.ReadKey(true);
            if (char.IsDigit(key.KeyChar))
            {
                int idx = int.Parse(key.KeyChar.ToString());
                if (idx < Engine.ActiveTargets.Count)
                    InvokeControlMenu(Engine.ActiveTargets[idx]);
            }
            else if (key.Key == ConsoleKey.M)
            {
                _autoEnforce = !_autoEnforce;
            }
        }

        private static void InvokeControlMenu(SentryEngine.ProcessMetadata target)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[ACTION REQUIRED: {target.Name.ToUpper()}]");
            Console.WriteLine("K: Terminate | F: Suspend | R: Resume | P: Perfect_Mask");

            var action = Console.ReadKey(true).Key;
            try
            {
                var proc = Process.GetProcessById(target.Pid);
                switch (action)
                {
                    case ConsoleKey.K: proc.Kill(); break;
                    case ConsoleKey.F: NativeMethods.NtSuspendProcess(proc.Handle); break;
                    case ConsoleKey.R: NativeMethods.NtResumeProcess(proc.Handle); break;
                    case ConsoleKey.P: PerformDeepSpoof(proc); break;
                }
            }
            catch (Exception ex) { Console.WriteLine($"Fault: {ex.Message}"); Thread.Sleep(1000); }
        }

        private static void PerformDeepSpoof(Process target)
        {
            string origin = target.MainModule.FileName;
            target.Kill();
            Thread.Sleep(1100);
            string shadowPath = Path.Combine(Path.GetDirectoryName(origin), "host_service_worker.exe");
            File.Copy(origin, shadowPath, true);
            Process.Start(new ProcessStartInfo(shadowPath) { UseShellExecute = true });
        }

        private static void Terminate(int pid) { try { Process.GetProcessById(pid).Kill(); } catch { } }

        private static bool IsPrivileged() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private static void Elevate()
        {
            Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName) { UseShellExecute = true, Verb = "runas" });
            Environment.Exit(0);
        }

        private static void RenderUI()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(@"
    _  _ _       _   _   _             _             
   | \| (_)__ _| |_| |_| |_ _  _ _ _| |_ ___ _ _ 
   | .` | / _` | ' \  _| ' \ || | ' \  _/ -_) '_|
   |_|\_|_\__, |_||_\__|_||_\_,_|_||_|\__\___|_|  
          |___/      - [ Last build: 04.01.26 ] -");

            Console.ResetColor();
            Console.WriteLine($"\n Status: {(_autoEnforce ? "ENFORCING" : "IDLE")} | Active Targets: {Engine.ActiveTargets.Count}");
            Console.WriteLine(new string('-', 55));

            if (Engine.ActiveTargets.Any())
            {
                for (int i = 0; i < Engine.ActiveTargets.Count; i++)
                {
                    var t = Engine.ActiveTargets[i];
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($" [{i}] {t.Name,-15} | {t.Description}");
                }
                Console.ResetColor();
                Console.WriteLine("\n [0-9] Select | [M] Toggle Mode | [F6] Panic");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(" Listening for intrusion signatures...");
            }
            Console.Write("\nCMD > ");
        }
    }
}