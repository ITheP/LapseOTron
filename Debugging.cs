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
        public static StringBuilder DebugInfoA { get; set; } = new StringBuilder();
      //  public static StringBuilder DebugInfoB { get; set; } = new StringBuilder();

        public static bool LogToFile { get; set; } = false;

        //public static void ApplyConfig()
        //{
        //    DebugInfoA.ApplySettings();
        //    DebugInfoB.ApplySettings();
        //}

        public static string Separator { get; } = new string('━', 88);

        public static void ClearA()
        {
            DebugInfoA.Clear();

            if (LogToFile)
                LogFileStream.WriteLine(Separator);
        }

        //public static void SetA(StringBuilder what)
        //{
        //    DebugInfoA.Clear();
        //    DebugInfoA.AppendLine(what);

        //    if (LogToFile)
        //    {
        //        LogFileStream.WriteLine(Separator);
        //        LogFileStream.WriteLine(what);
        //    }
        //}

        //public static void SetB(StringBuilder what)
        //{
        //    DebugInfoB.AppendLine(what);
        //}

        //public static void AddA(StringBuilder what)
        //{
        //    if (what != null)
        //        DebugInfoA.Append(what);

        //    if (LogToFile)
        //        LogFileStream.WriteLine(what);
        //}

        //public static void AddB(StringBuilder what)
        //{
        //    if (what != null)
        //        DebugInfoB.Append(what);
        //}

        public static void SetA(string what)
        {
            DebugInfoA.Clear();
            DebugInfoA.AppendLine(what);

            if (LogToFile)
            {
                LogFileStream.WriteLine(Separator);
                LogFileStream.WriteLine(what);
            }
        }

        public static void AddA(String what)
        {
            DebugInfoA.AppendLine(what);

            if (LogToFile)
                LogFileStream.WriteLine(what);
        }

        public static void AddSeparatorA()
        {
            DebugInfoA.AppendLine(Separator);

            if (LogToFile)
                LogFileStream.WriteLine(Separator);
        }

        ////[Conditional("DEBUG")]
        //public static string DebugPrintObjectArray<T>(List<T> raw, int startRow, int maxRows, string title = "")
        //{
        //    StringBuilder result = new StringBuilder();
        //    int rowCount = 0;
        //    string header = "";
        //    string footer = "";
        //    string[] rows;
        //    int maxLen;
        //    int maxRow;
        //    char seperator;
        //    string v;

        //    if (!raw.Any())
        //        result.AppendLine("DebugObjectArray - no data");

        //    if (startRow >= raw.Count)
        //        result.AppendLine("DebugObjectArray - start after last entry");

        //    PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(raw[0]);

        //    if (maxRows == -1)
        //    {
        //        maxRow = raw.Count;
        //    }
        //    else
        //    {
        //        maxRow = startRow + maxRows;
        //        if (maxRow > raw.Count)
        //            maxRow = raw.Count;
        //    }

        //    rows = new string[maxRow];

        //    try
        //    {
        //        // For each property
        //        for (int currentProperty = 0; currentProperty < properties.Count; currentProperty++)
        //        {
        //            // Name of column
        //            maxLen = properties[currentProperty].Name.Length;

        //            // loop through all the rows, getting length of property in question
        //            for (int r = startRow; r < maxRow; r++)
        //            {
        //                if (properties[currentProperty].GetValue(raw[r]) != null)
        //                {
        //                    v = properties[currentProperty].GetValue(raw[r]).ToString();
        //                    v = v.Replace("\n", "\\n").Replace("\r", "\\r");
        //                }
        //                else
        //                {
        //                    v = "[No Value]";
        //                }

        //                if (v.Length > maxLen)
        //                    maxLen = v.Length;
        //            }

        //            if (maxLen > 50)
        //                maxLen = 50;

        //            if (currentProperty == 0)
        //            {
        //                header = "    ";
        //                footer = "━━━━";
        //            }

        //            if (properties[currentProperty].Attributes.OfType<SeparatorAttribute>().Any())
        //            {
        //                header += " ┃ " + properties[currentProperty].Name.PadRight(maxLen, ' ');
        //                footer += "━┻━" + new string('━', maxLen);
        //                seperator = '┃';
        //            }
        //            else
        //            {
        //                header += " ┊ " + properties[currentProperty].Name.PadRight(maxLen, ' ');
        //                footer += "━┷━" + new string('━', maxLen);
        //                seperator = '┊';
        //            }

        //            // loop through all the rows, and fill in their values
        //            rowCount = 0;
        //            for (int r = startRow; r < maxRow; r++)
        //            {
        //                if (currentProperty == 0)
        //                    rows[rowCount] = r.ToString().PadLeft(4, ' ');

        //                if (properties[currentProperty].GetValue(raw[r]) != null)
        //                {
        //                    v = properties[currentProperty].GetValue(raw[r]).ToString();
        //                    v = v.Replace("\n", "\\n").Replace("\r", "\\r");
        //                    if (v.Length > 50)
        //                        v = v.Substring(0, 47) + "...";
        //                }
        //                else
        //                {
        //                    v = "[No Value]";
        //                }

        //                rows[rowCount] += " " + seperator + " " + v.PadRight(maxLen);

        //                rowCount++;
        //            }
        //        }

        //        if (!string.IsNullOrEmpty(title))
        //            title = "██ " + title + " ";

        //        result.AppendLine(title.PadRight(header.Length, '█'));
        //        result.AppendLine(header);

        //        for (int i = 0; i < rowCount; i++)
        //            result.AppendLine(rows[i]);

        //        result.AppendLine(footer);
        //    }
        //    catch (Exception ex)
        //    {
        //        result.AppendLine("Problem processing data in DebugPrintObjectArray...");
        //        result.AppendLine(ex.ToString());

        //        if (ex.InnerException != null)
        //            result.AppendLine(ex.InnerException.Message);
        //    }

        //    return result.ToString();
        //}

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
                    LogFileStream.Write(DebugInfoA);
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