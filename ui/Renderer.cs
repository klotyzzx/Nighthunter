using System;
using System.Collections.Generic;
using NightHunterV2.engines;

namespace NightHunterV2.ui
{
    public static class Renderer
    {
        public static void RenderMain(SentryEngine engine, bool auto, HashSet<IntPtr> maskedWindows)
        {
            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
    _   _ _       _     _   _  _               _           
   | \ | (_)     | |   | | | || |             | |          
   |  \| |_  __ _| |__ | |_| || |_   _ _ __ | |_ ___ _ __ 
   | . ` | |/ _` | '_ \| __| || | | | | '_ \| __/ _ \ '__|
   | |\  | | (_| | | | | |_| || | |_| | | | | ||  __/ |   
   |_| \_|_|\__, |_| |_|\__|_||_|\__,_|_| |_|\__\___|_|   
             __/ | v1.1 [05.01.26] Love youuu!                   
            |___/                                          ");

            Console.ResetColor();

            Console.Write("  Timestamp: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{DateTime.Now:HH:mm:ss}");
            Console.ResetColor();

            Console.Write(" | Active: ");
            Console.ForegroundColor = (engine.ActiveTargets.Count > 0) ? ConsoleColor.Red : ConsoleColor.Gray;
            Console.Write($"{engine.ActiveTargets.Count,-3}");
            Console.ResetColor();

            Console.Write(" | Status: ");
            if (auto)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PROTECTION ACTIVE  ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("LISTENING MODE     ");
            }

            Console.WriteLine(new string('─', 64));

            var targets = engine.ActiveTargets;
            for (int i = 0; i < 10; i++)
            {
                if (i < targets.Count)
                {
                    var t = targets[i];
                    bool isMasked = maskedWindows.Contains(t.WindowHandle);

                    Console.Write($"  [{i}] ");
                    Console.ForegroundColor = isMasked ? ConsoleColor.Yellow : ConsoleColor.Red;
                    Console.Write($"{t.Name,-20}");
                    Console.ResetColor();

                    string state = isMasked ? "HIDDEN " : "VISIBLE";
                    Console.WriteLine($" | PID: {t.Pid,-6} | STATE: {state}");
                }
                else
                {
                    Console.WriteLine(new string(' ', 64));
                }
            }

            Console.WriteLine(new string('─', 64));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  [ID] Interact | [M] Toggle Filter | [F6] Emergency Shutdown");
            Console.ResetColor();

            Console.SetCursorPosition(0, 22);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  PROMPT >                                                     ");
            Console.SetCursorPosition(11, 22);
            Console.ResetColor();
        }

        public static void RenderActionMenu(string name)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  " + new string('═', 58));
            Console.WriteLine($"    SESSION: {name.ToUpper()}");
            Console.WriteLine("  " + new string('═', 58));
            Console.ResetColor();

            Console.WriteLine("\n  VECTOR SELECTION:");

            RenderOption("K", "Terminate process thread");
            RenderOption("P", "Persistent title overwrite");
            RenderOption("U", "Restore original window context");
            RenderOption("X", "Header metadata obfuscation (MZ)");
            RenderOption("R", "PEB ImagePath spoofing");
            RenderOption("A", "Kernel-mode syscall bypass");

            Console.WriteLine("\n  " + new string('─', 58));
            Console.ForegroundColor = ConsoleColor.Gray;
            RenderOption("B", "Return to primary buffer");
            Console.ResetColor();
        }

        private static void RenderOption(string key, string desc)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  [{key}] ");
            Console.ResetColor();
            Console.WriteLine(desc);
        }
    }
}