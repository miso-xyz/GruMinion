using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone;
using Gee.External.Capstone.X86;

namespace XUnionPE
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.SetWindowSize(50, 15);
            Console.CursorVisible = false;
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.Gray;
            drawOnBottomCorner(' ');
            Console.Clear();
            Console.WriteLine();
            new Decompiler(Path.GetFullPath(args[0])).Unpack();
        }

        private static void drawOnBottomCorner(char ch)
        {
            Console.SetBufferSize(Console.WindowWidth + 1, Console.WindowHeight);
            Console.SetCursorPosition(Console.WindowWidth - 1, Console.WindowHeight - 1);
            Console.Write(ch);
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
        }
    }
}
