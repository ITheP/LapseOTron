using System;
using System.Collections.ObjectModel;
using System.IO;
//using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using SharpAvi.Codecs;

namespace LapseOTron
{
    public enum SizeHandling
    {
        CropToFit = 0,
        SquashToFit = 1,
        ScaleToFit = 2
    }

    public static class Globals
    {
        public static bool DebuggingEnabled { get; set; } = true;

        public static string TitleInfo { get; set; }
        public static string VersionInfo { get; set; }
        public static string CopyrightInfo { get; set; }
        public static string CompanyInfo { get; set; }
        public static string DescriptionInfo { get; set; }

        public static DateTime Expiry { get; set; }

        /// <summary>
        /// AppFolder points to LapseOTron.exe main program folder
        /// </summary>
        public static string AppFolder { get; set; }
        public static string LogFolder { get; set; }
        /// <summary>
        /// IThePFolder points to `I The P` master folder in My Documents
        /// </summary>
        public static string IThePFolder { get; set; }
        /// <summary>
        /// HomeFolder points to main LapseOTron folder in My Documents
        /// </summary>
        public static string HomeFolder { get; set; }
        //public static string SettingsFolder { get; set; }

        //public static string UserSettingsFile { get; set; }
        //public static string RuntimeSettingsFile { get; set; }
        //public static string SpecotronSettingsFile { get; set; }
        public static string CrashFile { get; set; }
        public static string LogFile { get; set; }


        public static ObservableCollection<string> Mpeg4Codecs { get; } = new ObservableCollection<string>();

        //public static bool Capture_IncludeCursor { get; set; }
        //public static bool Capture_IncludeHighlight { get; set; }
        //public static SizeHandling Capture_SizeHandling { get; set; } = SizeHandling.CropToFit;
        //public static bool Capture_OnlyOnContentChange { get; set; }
        //public static bool Capture_ZoomInToCursorPosition { get; set; }
        //public static bool Capture_HighQualityZoom { get; set; }
        public static bool ContentChanged { get; set; } = false;

        public static long Performance_LastScreenCaptureRate { get; set; }

        public static string RateInfoIcon { get; set; }
        public static ZoomInstruction ZoomInstruction { get; set; }

        public static FontFamily Font_WingDings { get; } = new FontFamily("Wingdings");
        public static FontFamily Font_SegoeMDL2 { get; } = new FontFamily("Segoe MDL2 Assets");

        public static MainWindow MainWindow { get; set; }

        public static bool ZoomRateOverride { get; set; }

        public static void Init()
        {
            InitApplicationInformation();
            InitFolders();
            InitCodecInformation();
        }

        private static void InitCodecInformation()
        {
            var codecs = Mpeg4VideoEncoderVcm.GetAvailableCodecs();

            foreach (var codec in codecs)
                Mpeg4Codecs.Add($"{codec.Codec} - {codec.Name}");
        }

        private static void InitApplicationInformation()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            string versionInfo = $"v {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            Globals.VersionInfo = versionInfo;

            //string title = GetAssemblyAttribute<AssemblyTitleAttribute>(a => a.Title);
            string product = GetAssemblyAttribute<AssemblyProductAttribute>(a => a.Product);
            //if (title != product && !string.IsNullOrWhiteSpace(product))
            //   Globals.TitleInfo = $"{title} - {product}";
            //else
            //    Globals.TitleInfo = title;
            Globals.TitleInfo = product;

            Globals.CopyrightInfo = GetAssemblyAttribute<AssemblyCopyrightAttribute>(a => a.Copyright);
            Globals.CompanyInfo = GetAssemblyAttribute<AssemblyCompanyAttribute>(a => a.Company);
            Globals.DescriptionInfo = GetAssemblyAttribute<AssemblyDescriptionAttribute>(a => a.Description);

        }

        public static string GetAssemblyAttribute<T>(Func<T, string> value) where T : Attribute
        {
            T attribute = (T)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(T));
            return value.Invoke(attribute);
        }


        private static void InitFolders()
        {
            // Note: Debugging window uses a proportional font

            AppFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debugging.AddA($"   ❓ Executable folder: {AppFolder}");

            IThePFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "I The P");
            Debugging.AddA($"   ❓ Master `I The P` folder: {IThePFolder}");
            if (!Directory.Exists(IThePFolder))
            {
                Directory.CreateDirectory(IThePFolder);
                Debugging.AddA($"      ✚ folder was missing, created!");
            }
            HomeFolder = Path.Combine(IThePFolder, "LapseOTron");
            Debugging.AddA($"   ❓ Lapse-O-Tron folder: {HomeFolder}");
            if (!Directory.Exists(HomeFolder))
            {
                Directory.CreateDirectory(HomeFolder);
                Debugging.AddA($"      ✚ folder was missing, created!");
            }

            LogFolder = HomeFolder; // Path.Combine(HomeFolder, "Logs");
            Debugging.AddA($"   ❓ Log folder: {LogFolder}");
            if (!Directory.Exists(LogFolder))
            {
                Directory.CreateDirectory(LogFolder);
                Debugging.AddA($"      ✚ folder was missing, created!");
            }

            LogFile = Path.Combine(LogFolder, "LapseOTron.log.txt");
            Debugging.AddA($"   ❓ Log file: {LogFile}");

            string settingsFolder = Path.Combine(HomeFolder, "Settings");
            Debugging.AddA($"   ❓ Settings folder: {settingsFolder}");
            if (!Directory.Exists(settingsFolder))
            {
                Directory.CreateDirectory(settingsFolder);
                Debugging.AddA($"      ✚ folder was missing, created!");
            }
            SettingsWrangler.SettingsFolder = settingsFolder;

            CrashFile = Path.Combine(LogFolder, "LapseOTron.crash.txt");
            Debugging.AddA($"   ❓ Crash file: {CrashFile}");
        }
    }

}
