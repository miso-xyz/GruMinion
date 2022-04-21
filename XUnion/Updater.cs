using System;
using System.Net;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.IO;

namespace XUnion
{
    class Updater
    {
        public Updater(byte[] updateFileStream)
        {
            string[] fileData = Encoding.Default.GetString(updateFileStream).Split("\n\n".ToArray());
            bool isChangelogData = false;
            List<string> ChangeLogData = new List<string>();
            foreach (string line in fileData)
            {
                if (line.StartsWith("##") || line.StartsWith("//") || line == "") { continue; }
                if (isChangelogData) { ChangeLogData.Add(line); continue; }
                if (line == "[CHANGELOG]") { isChangelogData = true; continue; }
                string p1 = line.Split("=".ToCharArray())[0].Replace("\t", null);
                string p2 = line.Split("=".ToCharArray())[1].Remove(0, 1);
                switch (p1)
                {
                    case "AppName": AppName = p2; break;
                    case "Version": LatestVersion = p2; break;
                    case "DownloadLink": UpdateLink = p2; break;
                    case "SHA256_Checksum": SHA256Checksum = FromHexString(p2.ToUpper()); break;
                    case "UpdateSize": UpdateSize = p2; break;
                }
            }
            UpdateDownloadName = CurrentAppName + "-Update_v" + LatestVersion + ".zip";
            ChangeLog = ChangeLogData.ToArray();
            if (SHA256Checksum == null) { throw new Exception("Missing Checksum for Update!"); } // sekurity 101
        }

        private byte[] FromHexString(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();
        }

        public const string
            CurrentAppName = "XUnion",
            CurrentVersion = "b1.0",
            RepoLink = "https://github.com/miso-xyz/GruMinion",
            UpdateLogLink = "https://raw.githubusercontent.com/miso-xyz/GruMinion/main/latest.info";

        public string AppName { get; set; }
        public string LatestVersion { get; set; }
        public string[] ChangeLog { get; set; }
        public string UpdateLink { get; set; }
        public byte[] SHA256Checksum { get; set; }
        public string UpdateSize { get; set; }
        public string UpdateDownloadName { get; set; }

        public static bool HasInternetConnection() { try { new Ping().Send("github.com", 750); return true; } catch { return false; } }

        public bool IsRunningLatest() { return CurrentVersion == LatestVersion; }
        public bool VerifyChecksum(byte[] data) { return BitConverter.ToString(SHA256.Create().ComputeHash(data)) == BitConverter.ToString(SHA256Checksum); }

        public void DownloadUpdate()
        {
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc_DownloadingUpdateEvent);
                wc.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(wc_DownloadFileCompleted);
                wc.DownloadFileAsync(new Uri(UpdateLink), UpdateDownloadName);
            }
            while (true) { Console.ReadKey(); }
        }

        void wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //Process.Start(AppDomain.CurrentDomain.BaseDirectory + "UniShieldUpdateInstaller.exe", Path.GetFullPath(UpdateDownloadName) + " --restart " + Program.path);
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetCursorPosition(1, 1);
            Console.Write("Comparing Checksums...");
            if (!VerifyChecksum(File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + UpdateDownloadName)))
            {
                Console.SetCursorPosition(1, 1);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Invalid Checksum!");
                Console.ResetColor();
                Console.SetCursorPosition(1, 2);
                Console.WriteLine("Press any key to exit!");
                Console.ReadKey();
                Environment.Exit(0);
            }
            Console.SetCursorPosition(1, 1);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Valid Checksum!");
            Process p = new Process();
            p.StartInfo.FileName = "cmd";
            p.StartInfo.Arguments = "/K timeout /T 1 /nobreak && cd " + AppDomain.CurrentDomain.BaseDirectory + " && 7z.exe e -y " + UpdateDownloadName + " && start " + CurrentAppName + ".exe " + '"' + Program.path + '"' + " && exit";
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.Start();
            Environment.Exit(0);
            //Process.Start(Path.GetFullPath(UpdateDownloadName));
        }

        void wc_DownloadingUpdateEvent(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Clear();
            Console.SetCursorPosition(1, 1);
            Console.Write(e.ProgressPercentage + "% (" + e.BytesReceived + "/" + e.TotalBytesToReceive + ")");
        }

        public static byte[] GetUpdate(string updateLink = UpdateLogLink) { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; return new WebClient().DownloadData(updateLink); }
    }
}