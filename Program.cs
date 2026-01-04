using System;
using System.Collections.Generic;
using System.Threading;
using System.Security.Principal;
using System.Diagnostics;
using NightHunterV2.engines;
using NightHunterV2.ui;
using NightHunterV2.core;

namespace NightHunterV2
{
    class Program
    {
        private static readonly SentryEngine Engine = new SentryEngine();
        private static readonly HashSet<IntPtr> MaskedEntries = new HashSet<IntPtr>();
        private static bool _enforcePolicy = false;

        static void Main()
        {
            if (!IsElevated())
            {
                Console.Title = "FATAL: PRIVILEGE_NOT_HELD";
                Console.WriteLine("\n[!] ADMINISTRATIVE PRIVILEGES REQUIRED");
                Thread.Sleep(3000);
                return;
            }

            SetupEnvironment();

            while (true)
            {
                try
                {
                    if (NativeMethods.IsDebuggerPresent()) Environment.Exit(1);

                    Engine.RefreshTargets();

                    if (_enforcePolicy)
                    {
                        foreach (var target in Engine.ActiveTargets)
                            Engine.Kill(target.Pid);
                    }

                    foreach (var hWnd in MaskedEntries)
                    {
                        Engine.MaskWindow(hWnd);
                    }

                    Renderer.RenderMain(Engine, _enforcePolicy, MaskedEntries);

                    if (Console.KeyAvailable) HandleInput();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                Thread.Sleep(200);
            }
        }

        private static void SetupEnvironment()
        {
            Console.Title = "NightHunter v1.1 | Last build 05.01.26 ALPHA";
            Console.CursorVisible = false;
            Engine.DisableQuickEdit();
        }

        private static void HandleInput()
        {
            var input = Console.ReadKey(true);
            var key = input.Key;

            if (key == ConsoleKey.M) _enforcePolicy = !_enforcePolicy;
            if (key == ConsoleKey.F6) Environment.Exit(0);

            if (char.IsDigit(input.KeyChar))
            {
                int index = int.Parse(input.KeyChar.ToString());
                if (index < Engine.ActiveTargets.Count)
                {
                    ExecuteActionMenu(Engine.ActiveTargets[index]);
                }
            }
        }

        private static void ExecuteActionMenu(SentryEngine.ProcessMetadata target)
        {
            Renderer.RenderActionMenu(target.Name);
            var action = Console.ReadKey(true).Key;

            switch (action)
            {
                case ConsoleKey.K:
                    Engine.Kill(target.Pid);
                    break;
                case ConsoleKey.P:
                    if (MaskedEntries.Add(target.WindowHandle)) Notify("ENTRY_MASKED", true);
                    break;
                case ConsoleKey.U:
                    if (MaskedEntries.Remove(target.WindowHandle))
                    {
                        NativeMethods.SetWindowText(target.WindowHandle, target.Name);
                        Notify("MASK_DEACTIVATED", true);
                    }
                    break;
                case ConsoleKey.X:
                    Notify(Engine.SpoofEAC(target.Pid) ? "HEADERS_PURGED" : "ACCESS_DENIED", true);
                    break;
                case ConsoleKey.R:
                    Notify(Engine.RenameInPEB(target.Pid, "svchost") ? "PEB_SPOOF_OK" : "PEB_WRITE_ERR", true);
                    break;
                case ConsoleKey.A:
                    Notify(Engine.HardCloakASM(target.Pid) ? "SYSCALL_BYPASS_ON" : "ASM_ERR", true);
                    break;
            }
        }

        private static void Notify(string msg, bool success)
        {
            Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"\n  {(success ? "[+]" : "[-]")} {msg}");
            Console.ResetColor();
            Thread.Sleep(800);
        }

        private static bool IsElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}