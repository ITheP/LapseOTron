using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using LapseOTron;
using LapseOTron;

namespace LapseOTron
{
    internal static class Debugging
    {
        public static StringBuilder DebugInfo { get; set; } = new StringBuilder();

        public static bool LogToFile { get; set; } = false;

        public static string Separator { get; } = new string('━', 88);

        public static void ClearA()
        {
            DebugInfo.Clear();

            if (LogToFile)
                LogFileStream.WriteLine(Separator);
        }

        public static void SetA(string what)
        {
            DebugInfo.Clear();
            DebugInfo.AppendLine(what);

            if (LogToFile)
            {
                LogFileStream.WriteLine(Separator);
                LogFileStream.WriteLine(what);
            }
        }

        public static void AddA(String what)
        {
            DebugInfo.AppendLine(what);

            if (LogToFile)
                LogFileStream.WriteLine(what);
        }

        public static void AddSeparatorA()
        {
            DebugInfo.AppendLine(Separator);

            if (LogToFile)
                LogFileStream.WriteLine(Separator);
        }

        private static StreamWriter LogFileStream { get; set; }

        public static void InitLogToFile()
        {
            LogToFile = SettingsWrangler.GeneralSettings.Debug_LogToFile;

            if (LogToFile)
            {
                if (LogFileStream == null)
                {
                    string filename = Globals.LogFile;
                    if (File.Exists(filename))
                        File.Delete(filename);

                    LogFileStream = new StreamWriter(filename);

                    LogFileStream.WriteLine(Globals.TitleInfo);
                    LogFileStream.WriteLine(Globals.VersionInfo);
                    LogFileStream.WriteLine(Globals.CopyrightInfo);
                    LogFileStream.WriteLine(Globals.CompanyInfo);
                    //LogFileStream.WriteLine(Globals.DescriptionInfo);
                    LogFileStream.WriteLine($"Log started: {DateTime.Now}");
                    LogFileStream.WriteLine($"Application folder: {Globals.AppFolder}\\");
                    LogFileStream.WriteLine($"Master folder: {Globals.IThePFolder}\\");
                    LogFileStream.WriteLine($"Home folder: {Globals.HomeFolder}\\");
                    LogFileStream.WriteLine($"Log folder: {Globals.LogFolder}\\");
                    LogFileStream.WriteLine(Separator);
                    // Include any existing log text
                    LogFileStream.AutoFlush = true;
                    LogFileStream.Write(DebugInfo);
                }
            }
            else
            {
                CloseLogFile();
            }
        }

        public static void CloseLogFile()
        {
            if (LogFileStream != null)
            {
                LogFileStream.Close();
                LogFileStream.Dispose();
            }
        }
    }
}