using LapseOTron.Settings;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

// Font icon stuff https://docs.microsoft.com/en-us/windows/uwp/design/style/segoe-ui-symbol-font

namespace LapseOTron
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Note, during development am happy for exceptions to stop code from running. At run time, prefer to log what's happened and attempt
            // to continue execution if at all possible.
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (s, ex) => LogUnhandledException((Exception)ex.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            DispatcherUnhandledException += (s, ex) => LogUnhandledException(ex.Exception, "Application.Current.DispatcherUnhandledException");
            TaskScheduler.UnobservedTaskException += (s, ex) => LogUnhandledException(ex.Exception, "TaskScheduler.UnobservedTaskException");
            Debugging.AddA("✔ Runtime global exception trap configured");
#endif

            Window splashScreen = new SplashScreen();
            this.MainWindow = splashScreen;
            splashScreen.Show();

            Globals.Init();

            // Expiry origionally added to allow for forced updates to later releases. Not needed so much now! Will move to auto-update checking anyway.
            Globals.Expiry = new DateTime(2050, 1, 1);

            SettingLoadState loadState = SettingLoadState.Ok;
            Debugging.AddA("Loading settings...");
            SettingsWrangler.GeneralSettings = SettingsWrangler.LoadSettings<GeneralSettings>(ref loadState);
            if (loadState == SettingLoadState.Ok!)
                Debugging.AddA("   ✔ loaded");
            else
                Debugging.AddA("   ✔ using default settings");


            if (DateTime.Now > Globals.Expiry)
            {
                var window = new ExpiredWindow();
                this.MainWindow = window;

                Debugging.AddA($"Expiry Triggered - attempting to run application after {Globals.Expiry}, should install a newer updated version before running the application again");
                Debugging.InitLogToFile();

                splashScreen.Close();
                window.Show();
            }
            else if (Globals.Mpeg4Codecs.Count == 0)
            {
                var window = new CodecWarningWindow();
                this.MainWindow = window;

                Debugging.AddA("Codecs Missing - please install a compatible codec before running the application again");
                Debugging.InitLogToFile();

                splashScreen.Close();
                window.Show();
            }
            else
            {
                var window = new MainWindow();
                this.MainWindow = window;
                Globals.MainWindow = window;

                Debugging.InitLogToFile();

                splashScreen.Close();
                window.Show();
                window.InitAppList();
            }
        }

        /// <summary>
        /// Saves off exception information in to a log file. Only used in releases, not when run in a debug development state
        /// </summary>
        /// <param name="exception">The exception<see cref="Exception"/></param>
        /// <param name="@event">The event<see cref="string"/></param>
        private void LogUnhandledException(Exception exception, string @event)
        {
            MessageBox.Show($"Lapse-O-Tron generated a critical exception. For debugging purposes, it will attempt to dump details of this in {Globals.CrashFile}. We will now do our best to continue!", "Critical error");

            string result = $"Lapse-O-Tron experienced an itsy bitsy problem @{DateTime.Now}..." + Environment.NewLine + Environment.NewLine + "Exception:" + Environment.NewLine + exception.Message;

            if (exception.InnerException != null)
                result += Environment.NewLine + Environment.NewLine + "Inner Exception:" + Environment.NewLine + exception.InnerException;

            if (exception.StackTrace != null)
                result += Environment.NewLine + Environment.NewLine + "Stack Trace:" + Environment.NewLine + exception.StackTrace.ToString();

            try
            {
                File.AppendAllText(Globals.CrashFile, result);
                //System.IO.File.AppendText(LogFile, result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to write log to {Globals.CrashFile}. Not going so well is it!" + Environment.NewLine + ex.Message, "Error handling the error");
            }
        }

        //#endif
        /// <summary>
        /// The Application_Exit
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="ExitEventArgs"/></param>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SettingsWrangler.SaveAllSettings();

            Debugging.CloseLogFile();
        }
    }
}
