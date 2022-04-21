using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using dnlib.DotNet;

namespace XUnion
{
    class Program
    {
        private static bool _stg2Export = false, _fixXor = true, _fixNames = true, _fixDelegates = true;
        private static ConsoleColor back1 = ConsoleColor.Gray, back2 = ConsoleColor.DarkGray;

        static int SelectMode(int defaultSelect = 0)
        {
            int selection = defaultSelect;
            bool noStg2 = v4.IsStage2ASM(asm);
            SetColorsFromPosition();
            while (true)
            {
                string[] options = new string[]
                {
                    "Unpack",
                    "Deobfuscate",
                    "",
                    "Extract Stage 2"
                };
                for (int x = 0; x < options.Length; x++)
                {
                    switch (x)
                    {
                        case 2: continue;
                        case 3: if (noStg2) { continue; } break;
                    }
                    Console.SetCursorPosition(1, x+2);
                    if (x == selection) { Console.Write(" > " + options[x] + "\n"); }
                    else                { Console.Write("   " + options[x] + "\n"); }
                }
                ConsoleKeyInfo key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow: if (selection > 0) { if (selection - 1 == 2) { selection -= 2; } else { selection--; } } break;
                    case ConsoleKey.DownArrow: if (selection <= (noStg2 ? 0 : options.Length - 2)) { if (selection + 1 == 2) { selection += 2; } else { selection++; } } break;
                    case ConsoleKey.Enter: return selection;
                }
            }
        }

        // Working but scrapped since each deobfuscation layer requires each other.
        /*static int[] SelectDeobMode(int defaultSelect = 0)
        {
            int selection = -1;
            bool clear = true;
            Console.BackgroundColor = Console.CursorLeft < Console.WindowWidth / 2 ? back2 : back1;
            Console.ForegroundColor = Console.CursorLeft < Console.WindowWidth / 2 ? back1 : back2;
            while (true)
            {
                if (clear)
                {
                    for (int y = 5; y < 10; y++)
                    {
                        Console.SetCursorPosition(2, y);
                        Console.Write("                     ");
                    }
                    if (selection != -1) { return new int[] { 0, selection }; }
                    clear = false;
                    selection = defaultSelect;
                }
                string[] options = new string[]
                {
                    "%OPT%Fix XORs",
                    "%OPT%Fix Names",
                    "%OPT%Remove Delegates",
                    "",
                    "Start"
                };
                bool[] optionsValues = new bool[3] { _fixXor, _fixNames, _fixDelegates };
                for (int x = 0; x < options.Length; x++)
                {
                    if (options[x] == "") { continue; }
                    Console.SetCursorPosition(2, x + 5);
                    ConsoleColor ogFore = Console.ForegroundColor;
                    if (options[x].StartsWith("%OPT%"))
                    {
                        if (optionsValues[x])
                        {
                            //if (selection == x) { Console.Write("> "); } else { Console.Write("  "); }
                            if (selection == x) { Console.ForegroundColor = ConsoleColor.Black; }
                            Console.Write("[");
                            if (selection == x) { Console.ForegroundColor = ConsoleColor.Magenta; } else { Console.ForegroundColor = ConsoleColor.Green; }
                            Console.Write("x");
                            if (selection == x) { Console.ForegroundColor = ConsoleColor.Black; } else { Console.ForegroundColor = ogFore; }
                            Console.Write("] ");
                        }
                        else
                        {
                            if (selection == x) { Console.ForegroundColor = ConsoleColor.Black; }
                            Console.Write("[ ] ");
                        }
                        Console.ForegroundColor = ogFore;
                        Console.Write(options[x].Replace("%OPT%", null) + "\n");
                    }
                    else
                    {
                        if (x == selection) { Console.Write("  > " + options[x] + "\n"); }
                        else { Console.Write("    " + options[x] + "\n"); }
                    }
                }
                ConsoleKeyInfo key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow: if (selection > 0) { if (selection - 1 == 3) { selection -= 2; } else { selection--; } } break;
                    case ConsoleKey.DownArrow: if (selection <= options.Length - 2) { if (selection + 1 == 3) { selection += 2; } else { selection++; } } break;
                    case ConsoleKey.Backspace: clear = true; continue;
                    case ConsoleKey.Enter:
                    if (options[selection].StartsWith("%OPT%"))
                    {
                        switch (selection)
                        {
                            case 0: _fixXor = !_fixXor; break;
                            case 1: _fixNames = !_fixNames; break;
                            case 2: _fixDelegates = !_fixDelegates; break;
                            default: return new int[] { 1, selection };
                        }
                    }
                    break;
                }
            }
        }*/

        static void InitConsole(bool clear = false)
        {
            Console.CursorVisible = false;
            SetColorsFromPosition();
            drawOnBottomCorner(' ');           // no scrollbars
            if (clear) { Console.Clear(); }
        }

        /*static readonly decimal version = 1.00m;
          static readonly string appText = "XUnion v" + version + " - misonothx";

        private static void DrawAppText(ConsoleColor fore = ConsoleColor.Gray, ConsoleColor back = ConsoleColor.Blue)
        {
            Console.ForegroundColor = fore; Console.BackgroundColor = back;
            int topTextIndex = 0;
            for (int x = 0; x < Console.BufferWidth; x++)
            {
                char ch = x < (Console.BufferWidth / 2) - (appText.Length / 2) ? ' ' : (topTextIndex < appText.Length ? appText[topTextIndex++] : ' ');
                Console.Write(ch);
            }
        }
        private static void DrawSideFrame(ConsoleColor color1 = ConsoleColor.Blue, ConsoleColor color2 = ConsoleColor.Cyan)
        {
            for (int y = 0; y < Console.WindowHeight - 1; y++)
            {
                if (y % 2 == 1) { Console.BackgroundColor = color1; }
                else            { Console.BackgroundColor = color2; }
                Console.SetCursorPosition(Console.WindowWidth - 1, y);
                Console.Write(' ');
                Console.SetCursorPosition(0, y);
                Console.Write(' ');
            }
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.BackgroundColor = color1;
            for (int x = 0; x < Console.BufferWidth; x++) { Console.Write(' '); }
        }*/

        private static void AlternateColors()
        {
            Console.BackgroundColor = Console.BackgroundColor == ConsoleColor.Gray ? ConsoleColor.DarkGray : ConsoleColor.Gray;
            Console.ForegroundColor = Console.BackgroundColor == ConsoleColor.Gray ? ConsoleColor.DarkGray : ConsoleColor.Gray;
        }
        private static void SetColorsFromPosition()
        {
            Console.BackgroundColor = Console.CursorLeft < Console.WindowWidth / 2 ? back2 : back1;
            Console.ForegroundColor = Console.CursorLeft < Console.WindowWidth / 2 ? back1 : back2;
        }

        private static void DrawSide(int sideNum)
        {
            for (int x = 0; x < Console.WindowWidth; x++)
            {
                SetColorsFromPosition();
                for (int y = 0; y < Console.WindowHeight; y++)
                {
                    Console.SetCursorPosition(x, y);
                    Console.Write(' ');
                }
            }
            drawOnBottomCorner('▓');
        }

        private static void DrawLayout(int activeSide)
        {
            for (int x = 0; x < Console.WindowWidth; x++)
            {
                SetColorsFromPosition();
                if (x == (Console.WindowWidth / 2) - 1) { AlternateColors(); }
                for (int y = 0; y < Console.WindowHeight; y++)
                {
                    Console.SetCursorPosition(x, y);
                    if (activeSide == 1 && x < (Console.WindowWidth / 2)) { Console.Write(' '); }
                    else
                    {
                        if (activeSide == 2 && x > (Console.WindowWidth / 2)) { Console.Write(' '); }
                        Console.Write('▓');
                    }
                }
            }
            drawOnBottomCorner('▓');
            /*Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = back1;
            int drawHeight = 1;
            for (int x = 0; x < Console.WindowWidth-1; x++)
            {
                if (x == (Console.WindowWidth / 2) - 3 ||
                    x == (Console.WindowWidth / 2) + 3)
                {
                    Console.ForegroundColor = x > (Console.WindowWidth/2) + 1?back1:back2;
                    for (int y = 1; y < Console.BufferHeight - drawHeight; y++)
                    {
                        Console.SetCursorPosition(x, y);
                        Console.Write('│');
                    }
                    Console.SetCursorPosition(x, x == (Console.WindowWidth / 2) + 3 ? Console.BufferHeight - drawHeight : 0);
                }

                if (x == Console.WindowWidth / 2)
                {
                    for (int y = 0; y < Console.BufferHeight - drawHeight; y++)
                    {
                        Console.BackgroundColor = y%2==1 ? back1 : back2;
                        Console.SetCursorPosition(x, y);
                        Console.Write(' ');
                    }
                    Console.SetCursorPosition(x, Console.BufferHeight - drawHeight);
                }
                Console.BackgroundColor = x >= Console.BufferWidth / 2?back2:back1;
                Console.Write(' ');
            }*/
        }

        private static void drawOnBottomCorner(char ch)
        {
            Console.SetBufferSize(Console.WindowWidth+1, Console.WindowHeight);
            Console.SetCursorPosition(Console.WindowWidth - 1, Console.WindowHeight - 1);
            Console.Write(ch);
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
        }

        static ModuleDefMD asm;
        static v4 mng;

        static void CheckForUpdates()
        {
            if (!Updater.HasInternetConnection()) { return; }
            Updater update = new Updater(Updater.GetUpdate());
            string text = "New update available!\n\nCurrent Version: " + Updater.CurrentVersion + "\nLatest Version: " + update.LatestVersion + "\n\nChangelog:\n\n";
            foreach (string cl_text in update.ChangeLog) { text += cl_text + "\n"; }
            text += "\nDownload now?";
            if (!update.IsRunningLatest())
            {
                if (MessageBox.Show(text, "Update Available!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    update.DownloadUpdate();
                    Application.Exit();
                }
            }
        }

        public static string path; // used by Updater.cs

        [STAThread]
        static void Main(string[] args)
        {
            path = Path.GetFullPath(args[0]);
            Console.SetWindowSize(50, 15);
            InitConsole(true);
            Console.SetCursorPosition(1, 1);
            CheckForUpdates();
            Console.WriteLine("Loading '" + Path.GetFileName(args[0]) + "'...");
            Console.WriteLine();
            try { ModuleDefMD tempAsm = ModuleDefMD.Load(Path.GetFullPath(args[0])); }
            catch (BadImageFormatException)
            {
                Console.WriteLine(" Native executable detected!");
                Console.WriteLine(" Loading 'XUnionPE.exe'...");
                Process.Start("XUnionPE.exe", '"' + Path.GetFullPath(args[0]) + '"').WaitForExit();
                return;
            }
            //DrawSideFrame();
            //DrawAppText();
            asm = ModuleDefMD.Load(Path.GetFullPath(args[0]));
            mng = new v4(asm, false, v4.IsStage2ASM(asm));
            int mode = 0/*, deobHover = 0*/;
            SaveFileDialog sfd = new SaveFileDialog();
            bool exit = false;
            Console.Clear();
            DrawLayout(1);
            drawOnBottomCorner(' ');
            while (!exit)
            {
                bool selectModeBackspace = false;
                mode = SelectMode(mode);
                switch (mode)
                {
                    case 0:
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            Console.Clear();
                            Console.SetCursorPosition(0, 1);
                            Console.WriteLine(" Unpacking '" + Path.GetFileName(mng.Shared.path) + "'...");
                            mng.Unpack(sfd.FileName);
                            Console.WriteLine("Successfully unpacked '" + Path.GetFileName(mng.Shared.path) + "'!");
                            exit = true;
                        }
                        break;
                    case 1:
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            Deobfuscators.v4 deob = null;
                            if (mng.Shared.Stage2Only)
                            {
                                mng.Stage2Deobfuscator.Deobfuscate();
                                deob = mng.Stage2Deobfuscator;
                            }
                            else
                            {
                                mng.Stage1Deobfuscator.Deobfuscate();
                                deob = mng.Stage1Deobfuscator;
                            }
                            Console.Clear();
                            Console.SetCursorPosition(0, 1);
                            Console.WriteLine(" Deobfuscating '" + Path.GetFileName(mng.Shared.path) + "'...");
                            Console.WriteLine();
                            //Console.SetCursorPosition((Console.BufferWidth / 2) + 2, 2);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(" " + deob.DelegateFixCount    + " Delegates Removed");
                            Console.WriteLine(" " + deob.XORStringFixCount   + " Strings   Fixed");
                            Console.WriteLine(" " + deob.XORFixCount         + " Numbers   Fixed");
                            Console.WriteLine(" " + deob.RenameFixCount      + " Elements  Renamed");
                            Console.WriteLine();
                            Console.WriteLine(" '" + Path.GetFileName(mng.Shared.path) + "' Successfully Deobfuscated!");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(" Saving Deobfuscated File...");
                            mng.Shared.asm.Write(sfd.FileName);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine();
                            Console.WriteLine(" Deobfuscated File Successfully Saved!");
                            exit = true;
                        }
                        break;
                    case 3:
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            Console.Clear();
                            Console.SetCursorPosition(0, 1);
                            Console.Write(" Exporting Stage 2... (" + mng.Shared.Stage2RawFile.Length + " bytes)");
                            File.WriteAllBytes(sfd.FileName, mng.Shared.Stage2RawFile);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write(" Stage 2 Exported!");
                            exit = true;
                        }
                        break;
                }
                if (selectModeBackspace) { continue; }
            }
            
            /*ModuleDefMD asm = ModuleDefMD.Load(Path.GetFullPath(args[0]));
            v4 mng = new v4(asm, false, v4.IsStage2ASM(asm));
            mng.Stage1Deobfuscator.Deobfuscate();
            mng.Stage2Deobfuscator.Deobfuscate();
            mng.Shared.asm.Write(Path.GetFileNameWithoutExtension(args[0]) + "-stg1-unpk_XUnion" + Path.GetExtension(args[0]));
            mng.Shared.Stage2asm.Write(Path.GetFileNameWithoutExtension(args[0]) + "-stg2-unpk_XUnion" + Path.GetExtension(args[0]));
            File.WriteAllBytes(Path.GetFileNameWithoutExtension(args[0]) + "-app-unpk_XUnion" + Path.GetExtension(args[0]), mng.Shared.OutputRawFile);*/
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(0, Console.BufferHeight - 1);
            for (int x = 0; x < Console.BufferWidth - 1; x++) { Console.Write(' '); }
            drawOnBottomCorner(' ');
            Console.SetCursorPosition(2, Console.BufferHeight - 1);
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
