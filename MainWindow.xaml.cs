using LapseOTron;
using LapseOTron.Settings;
using SharpAvi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Design.Behavior;
//using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

//using System.Windows.Shapes;
//using NAudio.CoreAudioApi;

namespace LapseOTron
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int InterfaceFrameRate = 60;
        private bool IsRecording { get; set; }

        private string SelectedFile { get; set; }
        public bool ManualStop { get; set; }


        RecorderParams RecorderParams { get; set; }
        Recorder Recorder { get; set; }

        bool IncludeCursor { get; set; } = true;

        private System.Windows.Input.Cursor PreviousCursor { get; set; }

        private string Filename { get; set; }

        public bool RateChanged { get; set; } = false;
        public bool RateChangeHandled { get; set; } = true;
        public float Rate { get; set; }
        public float PreviousRate { get; set; }
        public float TargetRate { get; set; }
        //  public double StartSpeedRate { get; set; }
        public float TargetRateChange { get; set; }

        public bool ZoomChanged { get; set; } = false;
        public bool ZoomChangeHandled { get; set; } = true;
        public float CurrentZoom { get; set; }
        public float SelectedZoom { get; set; }
        public string ZoomDescription { get; set; }
        public float ZoomChangePeriod { get; set; }
        public float PreviousZoom { get; set; }
        public float TargetZoom { get; set; }
        public float TargetZoomChange { get; set; }

        public bool AllowInvoke { get; set; } = false;

        public const string InfoIcon_Smile = "J";
        public const string InfoIcon_Sad = "L";
        public const string InfoIcon_Hourglass = "6";
        public const string InfoIcon_AutoSpin = "";

        public string InfoIconText { get; set; }
        public string InfoText { get; set; }
        public bool InfoTextUpdated { get; set; } = false;
        public bool AutoText { get; set; } = false;

        private int ScreenshotCounter { get; set; } = 0;
        private string ScreenshotFilename { get; set; }

        AppInfo SelectedApp { get; set; }
        private Size DestSize;
        private bool Initializing { get; set; } = false;

        private int FramesSinceLastInfo { get; set; } = 0;
        private bool LowInfoPriority { get; set; } = false;
        private int FrameCount { get; set; } = 0;

        public string DebugInfoExtra { get; set; }
        private AppInfo ThisApp { get; set; }

        public MainWindow()
        {
            Initializing = true;

            InitializeComponent();

            Options.Visibility = Visibility.Collapsed;

            AppTitle.Text = Globals.TitleInfo;
            Version.Text = Globals.VersionInfo;
            Copyright.Text = Globals.CopyrightInfo;
            Company.Text = Globals.CompanyInfo;
            //Description.Text = Globals.DescriptionInfo;

            KeyBindings.ItemsSource = Hotkeys.RegisteredKeys;
            UI_Rate.ItemsSource = Rates.RateList;
            Capture_SizeHandling.ItemsSource = CaptureSizes.CaptureSizeList;

            // Some defaults - eventually to be replaced by user saved settings
            GeneralSettings settings = SettingsWrangler.GeneralSettings;

            Capture_IncludeCursor.IsChecked = settings.Capture_IncludeCursor;
            Set_UI_IncludeCursor();

            Capture_IncludeHighlight.IsChecked = settings.Capture_IncludeHighlight;
            Set_UI_IncludeHighlight();

            Capture_SkipDuplicateFrames.IsChecked = settings.Capture_OnlyOnContentChange;
            Set_UI_SkipDuplicateFrames();

            Capture_HighQualityZoom.IsChecked = settings.Capture_HighQualityZoom;
            Set_UI_HighQualityZoom();

            Capture_ZoomInToCursorPosition.IsChecked = settings.Capture_ZoomInToCursorPosition;
            Set_UI_ZoomInToCursorPosition();

            Capture_Size.SelectedValue = settings.Capture_Size;
            Capture_SizeHandling.SelectedValue = settings.Capture_SizeHandling;

            // These should become user preferences/last time loaded settings
            UI_Rate.SelectedValue = settings.Rate; // Index = 11; // default to 10x speed
            UI_RateChangePeriod.SelectedValue = settings.RateChangePeriod; // Index = 13;

            InitRate(0);

            UI_Zoom.SelectedValue = settings.Zoom; // Index = 7;
            UI_ZoomChangePeriod.SelectedValue = settings.ZoomChangePeriod; // Index = 2; // default to 2 seconds

            CurrentZoom = 1.0F; //  Start zoomed
            SelectedZoom = float.Parse(((ComboBoxItem)UI_Zoom.SelectedItem).Tag.ToString());
            ZoomDescription = "Normal"; // UI_Zoom.SelectedValue.ToString();
            ZoomChangePeriod = float.Parse(((ComboBoxItem)UI_ZoomChangePeriod.SelectedItem).Tag.ToString());

            // 60 fps is way more than needed for our needs here.
            //  Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata() { DefaultValue = InterfaceFrameRate });

            License.Text = $"This version stops working on {Globals.Expiry.ToLongDateString()}";

            InfoEffect = ((Storyboard)FindResource("HighlightTextFade")); //.Clone();

            // Used for when no window is selected
            IntPtr thisWindow = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
            ThisApp = new AppInfo() { HWnd = thisWindow };

            InitSizeHandling();

            SetLockedInfo(false);

            Set_UI_IncludeCursor();
            Set_UI_IncludeHighlight();
            Set_UI_SkipDuplicateFrames();
            Set_UI_ZoomInToCursorPosition();
            Set_UI_HighQualityZoom();

            Initializing = false;

            this.Width = 300;
            this.Height = 450;

            CompositionTarget.Rendering += OnRender;

            SetInfoText(InfoIcon_Smile, "Waiting...");
        }

        private const string TXT_Locked = "\uE72E";
        private const string TXT_Unlocked = "\uE785";

        private void SetRateChangeInfo()
        {
            string rateChangeDescription = ((Rate)UI_Rate.SelectedItem).Description;
            string rateChangePeriod = ((ComboBoxItem)UI_RateChangePeriod.SelectedItem).Content.ToString();
            rateChangePeriod = rateChangePeriod.Replace("sec", "real second", StringComparison.CurrentCulture);
            rateChangePeriod = rateChangePeriod.Replace("min", "real minute", StringComparison.CurrentCulture);
            RateChangeInfo.Text = $"When selected, rate will change to {rateChangeDescription}, over a period of {rateChangePeriod}";
        }

        private void SetZoomChangeInfo()
        {
            string zoomDescription = ((ComboBoxItem)UI_Zoom.SelectedItem).Content.ToString();
            string zoomPeriod = ((ComboBoxItem)UI_ZoomChangePeriod.SelectedItem).Content.ToString();
            zoomPeriod = zoomPeriod.Replace("sec", "real second", StringComparison.CurrentCulture);
            zoomPeriod = zoomPeriod.Replace("min", "real minute", StringComparison.CurrentCulture);

            ZoomChangeInfo.Text = $"When 'Zoom in' is triggered, zoom will change to {zoomDescription}x zoom in real time, over a period of {zoomPeriod}";
        }

        private void SetLockedInfo(bool isLocked)
        {
            string txt;
            Brush color;
            if (isLocked)
            {
                txt = TXT_Locked;
                color = Brushes.Red; // DarkRed;
            }
            else
            {
                txt = TXT_Unlocked;
                color = Brushes.Black;
            }

            UI_SelectedAppWindowLock.Text = txt;
            UI_SelectedAppWindowLock.Foreground = color;
            DestinationSizeInfoLock.Text = txt;
            DestinationSizeInfoLock.Foreground = color;
            Capture_SizeLocked.Text = txt;
            Capture_SizeLocked.Foreground = color;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hotkeys.RegisterDefaults(this);

            this.Width = 300;
            this.Height = 600;

            //// Some extra control initialisation - make sure this doesnt screw anything up
            //Initializing = true;

            //KeyBindings.ItemsSource = Hotkeys.RegisteredKeys;
            //Rate.ItemsSource = Rates.RateList;

            //// Some defaults
            //// These should become user preferences/last time loaded settings
            //Rate.SelectedIndex = 4;
            //RateChangePeriod.SelectedIndex = 13;

            //Zoom.SelectedIndex = 7;
            //ZoomChangePeriod.SelectedIndex = 13;

            //Initialising = false;
        }

        public void InitAppList()
        {
            PopulateAppWindows();

            //// Find this!
            //foreach (AppInfo appInfo in UI_SelectedAppWindow.ItemsSource)
            //{
            //    if (appInfo.HWnd == ThisApp.HWnd && appInfo.HWnd != IntPtr.Zero)
            //    {
            //        UI_SelectedAppWindow.SelectedItem = appInfo;
            //        break;
            //    }
            //}
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hotkeys.UnregisterDefaults();
        }

        public void SetInfoText(string icon, string text, bool lowPriority = false)
        {
            LowInfoPriority = lowPriority;
            InfoIconText = icon;
            InfoText = text;
            InfoTextUpdated = true;
        }

        public void SetInfoText(string text, bool lowPriority = false)
        {
            LowInfoPriority = lowPriority;
            InfoText = text;
            InfoTextUpdated = true;
        }

        public void SetRateInfoIcon(string text)
        {
            if (RateInfoIcon.Text != text)
                RateInfoIcon.Text = text;

        }
        public void SetInfoTextIconToSpinner()
        {
            InfoIconText = InfoIcon_AutoSpin;
        }


        private Storyboard InfoEffect { get; set; }

        /// <summary>
        /// Set info with a little effect to help draw attention to it
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="content"></param>
        /// <param name="delay">Delay in frames (60fps)</param>
        private void SetInfoWithEffect(string icon, string content, bool lowInfoPriority = false)
        {
            SetInfoText(icon, content, lowInfoPriority);
            InfoEffect.Begin(Info);
        }

        public BitmapSource NewScreenshot { get; set; }

        // ToDo: Get rectangle size from their original size
        // Rectangles are 17 pixels heigh
        private double SetVisual(System.Windows.Shapes.Rectangle visual, System.Windows.Shapes.Rectangle visualBorder, double start, double current, double end, bool autoColor = true)
        {
            double maxHeight = visualBorder.Height;
            double range = end - start;         // e.g. 15.0 -> 5.0 = 10.0
            current -= start;                   // e.g. 7.5 = 2.5
            double percDifference = current / range;
            if (percDifference > 1.0)
                percDifference = 1.0;
            double height = maxHeight * percDifference;

            //if (height > maxHeight)
            //    height = maxHeight;

            visual.Height = height;

            if (autoColor)
            {
                if (height == maxHeight)
                    visual.Fill = System.Windows.Media.Brushes.LimeGreen;
                else
                    visual.Fill = System.Windows.Media.Brushes.OrangeRed;
            }

            return percDifference;
        }

        private int SizeInfoWidth { get; set; }
        private int SizeInfoHeight { get; set; }

        private float InfoLastRate { get; set; } = -1;
        private float InfoLastZoom { get; set; } = -1;

        public void SetDestinationSizeInfo(int width, int height)
        {
            if (width == SizeInfoWidth && height == SizeInfoHeight)
                return;

            DestinationSizeInfo.Text = $"{width}x{height}";
            SizeInfoWidth = width;
            SizeInfoHeight = height;
        }

        // ToDo: Turn OnRender on or off.
        protected void OnRender(object sender, EventArgs e)
        {
            FrameCount++;
            // Debug.Print($"{FrameCount} {DateTime.Now.Second}.{DateTime.Now.Millisecond}");

            // Update preview if there is one waiting for us!
            if (NewScreenshot != null)
            {
                Preview.Source = NewScreenshot;
                NewScreenshot = null;
            }
            else if (!IsRecording && SelectedApp != null && SelectedApp.HWnd != IntPtr.Zero && FrameCount % 3 == 0)
            {
                Size size = Utils.GetSizeOfWindow(SelectedApp);
                int destWidth, destHeight;
                if (DestSize.Width == -1)
                    destWidth = size.Width;
                else
                    destWidth = DestSize.Width;

                if (DestSize.Height == -1)
                    destHeight = size.Height;
                else
                    destHeight = DestSize.Height;

                //ZoomInstruction zoomInstruction = ZoomInstruction.JustChillDude;
                NewScreenshot = ScreenCapture.SnapScreen(SelectedApp.HWnd, 0, 0, size.Width, size.Height, destWidth, destHeight, SettingsWrangler.GeneralSettings.Capture_SizeHandling, true, Globals.ZoomInstruction, CurrentZoom, 0, 0, null, SettingsWrangler.GeneralSettings.Capture_OnlyOnContentChange);

                SetPreviewSize(destWidth, destHeight);

                SetDestinationSizeInfo(destWidth, destHeight);

                //Debug.Print($"{size.Width},{size.Height} - {destWidth},{destHeight}");
                Preview.Source = NewScreenshot;
                NewScreenshot = null;
            }

            if (TargetRateChange > 0)
            {
                Rate += TargetRateChange;

                SetVisual(RateVisual, RateVisualBorder, PreviousRate, Rate, TargetRate);

                if (Rate > TargetRate)
                {
                    Rate = TargetRate;
                    TargetRateChange = 0;
                }
            }
            else if (TargetRateChange < 0)
            {
                Rate += TargetRateChange;
                if (Rate < TargetRate)
                {
                    Rate = TargetRate;
                    TargetRateChange = 0;
                }

                SetVisual(RateVisual, RateVisualBorder, PreviousRate, Rate, TargetRate);
            }


            if (TargetZoomChange > 0)
            {
                CurrentZoom += TargetZoomChange;

                SetVisual(ZoomVisual, ZoomVisualBorder, PreviousZoom, CurrentZoom, TargetZoom);

                if (CurrentZoom > TargetZoom)
                {
                    CurrentZoom = TargetZoom;
                    TargetZoomChange = 0;

                    Globals.ZoomRateOverride = false;
                }

                SetVisual(ZoomVisual, ZoomVisualBorder, PreviousZoom, CurrentZoom, TargetZoom);

                //        Debug.Print($"{FrameCount}: ZoomIn - PreviousZoom={PreviousZoom}, CurrentZoom={CurrentZoom}, TargetZoom={TargetZoom}");
            }
            else if (TargetZoomChange < 0)
            {
                CurrentZoom += TargetZoomChange;

                if (CurrentZoom < TargetZoom)
                {
                    CurrentZoom = TargetZoom;
                    TargetZoomChange = 0;
                    Globals.ZoomRateOverride = false;
                }

                SetVisual(ZoomVisual, ZoomVisualBorder, PreviousZoom, CurrentZoom, TargetZoom);

                // Debug.Print($"{FrameCount}: ZoomOut - PreviousZoom={PreviousZoom}, CurrentZoom={CurrentZoom}, TargetZoom={TargetZoom}");
            }

            //  DebugInfo.Text = $"Rate: {Rate:00.000}, Zoom: {CurrentZoom:0.000} - {DebugInfoExtra}";

            if (Rate != InfoLastRate)
            {
                RateInfo.Text = $"{Rate:0.0}x";
                InfoLastRate = Rate;
            }

            if (CurrentZoom != InfoLastZoom)
            {
                ZoomInfo.Text = $"{CurrentZoom:0.0}x";
                InfoLastZoom = CurrentZoom;
            }

            // We update UI text here, if required. Wierd way to do it, but updates to Info from background thread were causing lock-ups
            // when stopping videos - loosing the videos. Async updates to the UI thread could end up with e.g. recording info
            // overriding other info after the recording had stopped.
            // So we do any updates here. Also means if there are multiple updates from background thread in a frame, only the latest should show.

            // Simple delay - won't update till delay hit's zero (e.g. to stop instant updates overriding a message)
            FramesSinceLastInfo++;

            if (InfoTextUpdated)
            {
                if (!LowInfoPriority || FramesSinceLastInfo > 60)
                {
                    InfoIcon.Text = InfoIconText;
                    Info.Text = InfoText;
                    InfoTextUpdated = false;
                    //  AutoText = false;

                    if (!LowInfoPriority)
                        FramesSinceLastInfo = 0;
                }
            }

            if (InfoIconText?.Length == 0)
            {
                if (IsRecording)
                {
                    //   string rateInfoIcon;
                    if (Globals.ContentChanged)
                    {
                        if (InfoIcon.FontFamily != Globals.Font_WingDings)
                            InfoIcon.FontFamily = Globals.Font_WingDings;

                        int iconOffset = (FrameCount >> 3) % 12;     // 0->1000 -> 0->12
                        InfoIcon.Text = Convert.ToChar('·' + iconOffset).ToString();
                    }
                    else
                    {
                        // Show a pause
                        if (InfoIcon.FontFamily != Globals.Font_SegoeMDL2)
                            InfoIcon.FontFamily = Globals.Font_SegoeMDL2;

                        if (InfoIcon.Text != InfoIcon_Pause)
                            InfoIcon.Text = InfoIcon_Pause;
                    }
                }
            }
            else
            {
                if (InfoIcon.FontFamily != Globals.Font_WingDings)
                    InfoIcon.FontFamily = Globals.Font_WingDings;
            }

            //if (Globals.ContentChanged != LastContentChangedState)
            //{
            //    string rateInfoIcon;
            //    if (Globals.ContentChanged)
            //        RateInfoIcon.Text = "\xEC4A";
            //    else
            //        RateInfoIcon.Text = "\xE769";

            //    LastContentChangedState = Globals.ContentChanged;
            //}

            //// Auto text - not that pretty - should refactor this!
            //if (AutoText)
            //{
            //    string text;

            //    if (appInfo == null || appInfo.HWnd == IntPtr.Zero)
            //    {
            //        text = "No application/window selected...";
            //    }
            //    else
            //    {
            //        Size size = Utils.GetSizeOfWindow(appInfo);
            //        text = $"Selected target size: {size.Width}x{size.Height}";
            //    }

            //    if (text != Info.Text)
            //        SetInfoWithEffect(text);
            //}

            // Performance monitoring - consider <17ms for a snapshot as good, <33 as pretty poor, and above that as really bad
            // Smooth the performance stuff a bit so it's less jittery
            PerfAverage += Globals.Performance_LastScreenCaptureRate;

            if (FrameCount % 10 == 0)
            {
                double perf = PerfAverage * 0.1;  // Average over 10 frames

                double perc = SetVisual(UI_PerformanceVisual, UI_PerformanceVisualBorder, 0, perf, 33, false);

                // 0=Green -> 0.5=Yellow -> 1=Red
                double r = 0;
                double g = 0;
                if (perc < 0.5)
                {
                    r = (perc + perc);  // 0->1
                    g = 1.0;            // Always green!
                }
                else if (perc > 0.5)
                {
                    r = 1.0;           // Always red!
                    g = 1.0 - ((perc - 0.5) * 2.0);
                }

                UI_PerformanceVisual.Fill = new SolidColorBrush(Color.FromArgb(255, (byte)(255.0 * r), (byte)(255.0 * g), 0));

                PerfAverage = 0;
            }

            if (FrameCount % 60000 == 0)
            {
                GC.Collect();                       // If required.... first collect gets things onto the finalizer que
                GC.WaitForPendingFinalizers();    // Then you want for it to be ready
                GC.Collect();
            }
        }

        long PerfAverage;

        private const string InfoIcon_Pause = "\xE769";

        private bool LastContentChangedState { get; set; }

        private void PopulateAppWindows()
        {
            CursorWait();

            SetInfoText(InfoIcon_Hourglass, "Scanning capture targets...");

            List<AppInfo> windows = new List<AppInfo>();

            Process[] processList = Process.GetProcesses();
            AppInfo app;

            foreach (Process process in processList)
            {

                if (!String.IsNullOrEmpty(process.MainWindowTitle) && process.MainWindowHandle != IntPtr.Zero && NativeMethods.IsWindowVisible(process.MainWindowHandle))
                {
                    //Debug.Print((process.MainWindowTitle.Length < 32 ? process.MainWindowTitle : process.MainWindowTitle.Substring(32) + "..."));
                    app = new AppInfo
                    {
                        Title = (process.MainWindowTitle.Length < 48 ? process.MainWindowTitle : process.MainWindowTitle.Substring(0, 48) + "..."),
                        HWnd = process.MainWindowHandle
                    };

                    var size = Utils.GetSizeOfWindow(app);

                    // Screenshot of window!
                    app.Preview = ScreenCapture.SnapScreen(app.HWnd, 0, 0, size.Width, size.Height, 128, 128, SizeHandling.ScaleToFit, true);
                    windows.Add(app);
                }
            }

            windows.Sort((x, y) => x.Title.CompareTo(y.Title));
            // Add these in reverse order at the start so everything appears as it should do
            windows.Insert(0, new AppInfo() { Title = "Windows...", HWnd = IntPtr.Zero });
            // windows.Insert(0, new AppInfo() { Title = "", HWnd = IntPtr.Zero });

            IntPtr hWnd = NativeMethods.GetDesktopWindow();

            for (int i = 0; i < System.Windows.Forms.Screen.AllScreens.Length; i++)
            {
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens[i];

                IntRect rect = new IntRect();
                rect.Left = screen.Bounds.Left;
                rect.Right = screen.Bounds.Right;
                rect.Top = screen.Bounds.Top;
                rect.Bottom = screen.Bounds.Bottom;
                System.Drawing.Rectangle r;
                windows.Insert(0, new AppInfo() { Title = $"Display {i} - {screen.DeviceName}", HWnd = hWnd, Bounds = rect, BoundsSet = true });
            }

            windows.Insert(0, new AppInfo() { Title = "Entire Desktop", HWnd = hWnd });
            windows.Insert(0, new AppInfo() { Title = "Displays...", HWnd = IntPtr.Zero });
            windows.Insert(0, new AppInfo() { Title = "Define custom area", HWnd = hWnd });
            windows.Insert(0, new AppInfo() { Title = "Custom...", HWnd = IntPtr.Zero });

            UI_SelectedAppWindow.ItemsSource = windows;

            SetInfoText(InfoIcon_Smile, "Select capture target from drop down above");

            CursorRestore();
        }

        public bool InitCapturer(string filename) //, Visual visual)
        {
            //IntPtr hWnd;
            int audioSourceId = -1;

            Debug.Print("InitCapturer...");

            ////MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            ////foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
            ////{
            ////    Console.WriteLine($"{device.FriendlyName}, {device.State}");
            ////}

            // If no window is selected, we record the whole bally screen!
            //AppInfo appInfo = (AppInfo)SelectedAppWindow.SelectedItem;

            if (SelectedApp == null || SelectedApp.HWnd == IntPtr.Zero)
            {
                SetInfoWithEffect(InfoIcon_Sad, "Select capture target you want to record...");
                return false;
            }
            else
            {
                bool recordAudio = false;       // Only no audio :)

                FourCC encoder;
                if (Encoder_CaptureRaw.IsChecked ?? false)
                    encoder = SharpAvi.KnownFourCCs.Codecs.Uncompressed;
                else
                    encoder = SharpAvi.KnownFourCCs.Codecs.X264;

                InitSizeHandling();

                RecorderParams = new RecorderParams(filename, 30, encoder, 80, audioSourceId, true, recordAudio, 4, IncludeCursor, SelectedApp, DestSize);

                //VisCapturer = new Recorder(rp); // new Capturer();
                //VisCapturer.Init(filename); //, visual);
                //capturer.Start();

                return true;
            }
        }

        public void SetPreviewSize(double width, double height)
        {
            Preview.Width = width;
            Preview.Height = height;
        }

        private void InitSizeHandling()
        {
            string sizeDescription = Capture_Size.SelectedValue.ToString();
            SettingsWrangler.GeneralSettings.Capture_Size = sizeDescription;

            switch (sizeDescription)
            {
                case "Original size":
                case "Custom area":
                    DestSize.Width = -1; /// will be calculated from the source window
                    DestSize.Height = -1;
                    break;
                default:
                    DestSize = DimensionsFromSize(sizeDescription);
                    break;
            }

            SettingsWrangler.GeneralSettings.Capture_SizeHandling = ((CaptureSize)Capture_SizeHandling.SelectedItem).Type;


            //switch (((CaptureSize)Capture_SizeHandling.SelectedItem).Description)
            //{
            //    case "Crop to fit":
            //        Globals.Capture_SizeHandling = SizeHandling.CropToFit;
            //        break;
            //    case "Squash to fit":
            //        Globals.Capture_SizeHandling = SizeHandling.SquashToFit;
            //        break;
            //    case "Scale to fit":
            //        Globals.Capture_SizeHandling = SizeHandling.ScaleToFit;
            //        break;
            //}

            AppInfo app;
            if (SelectedApp == null || SelectedApp.HWnd == IntPtr.Zero)
                app = ThisApp;
            else
                app = SelectedApp;

            Size appSize = Utils.GetSizeOfWindow(app);

            if (DestSize.Width == -1)
                Preview.Width = appSize.Width;
            else
                Preview.Width = DestSize.Width;

            if (DestSize.Height == -1)
                Preview.Height = appSize.Height;
            else
                Preview.Height = DestSize.Height;
        }

        public void StartCapturer()
        {
            Debug.Print("StartCapturer...");

            IsRecording = true;
            Recorder = new Recorder(RecorderParams, this);
        }

        private void CursorWait()
        {
            PreviousCursor = this.Cursor;
            this.Cursor = System.Windows.Input.Cursors.Wait;
        }

        private void CursorRestore()
        {
            this.Cursor = PreviousCursor;
        }

        private void UI_Record_Click(object sender, RoutedEventArgs e)
        {
            CursorWait();

            SetInfoText(InfoIcon_Hourglass, "Initializing...");

            Rate = TargetRate;
            TargetRateChange = 0;
            InitRate(0);

            ScreenshotCounter = 0;
            ScreenshotFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), $"LapseOTron {DateTime.Now.ToString(@"yyyy-MM-dd hh-mm-ss")}"); //.avi");
            Filename = ScreenshotFilename + ".avi";

            if (InitCapturer(Filename))
            {
                RateChanged = false;
                RateChangeHandled = true;

                EnableDisableControl(Encoder_CaptureRaw, false);
                EnableDisableControl(Capture_Size, false, Capture_SizeL);
                SetLockedInfo(true);

                StartCapturer();

                SetInfoText(InfoIcon_Hourglass, "Recording...");

                UI_Record.Visibility = Visibility.Collapsed;
                UI_Screenshot.IsEnabled = true; // Visibility = Visibility.Visible;
                UI_Pause.Visibility = Visibility.Visible;
                //Stop.Visibility = Visibility.Visible;
                UI_Stop.IsEnabled = true;
                UI_Play.Visibility = Visibility.Collapsed;
                UI_SelectedAppWindow.IsEnabled = false;

                AllowInvoke = true;
            }

            CursorRestore();
        }

        private static void EnableDisableControl(Control control, bool enabled, TextBlock labelControl = null)
        {
            control.IsEnabled = enabled;
            Brush colour;

            if (enabled)
                colour = Brushes.Black;
            else
                colour = Brushes.Gray;

            if (labelControl == null)
                control.Foreground = colour;
            else
                labelControl.Foreground = colour;
        }

        public void StopCapturer()
        {
            Debug.Print("StopCapturer...");

            //using (StreamWriter sw = new StreamWriter(filename))
            {
                AllowInvoke = false;

                //sw.WriteLine($"Lapse-O-Tron Log @ {DateTime.Now}");
                //Recorder.SW = sw;
                if (IsRecording)
                {
                    //sw.WriteLine("IsRecording");

                    Recorder.Dispose();
                    //sw.WriteLine("Recorder.Dispose run");
                    //Recorder.SW = null;
                    Recorder = null;
                    //sw.WriteLine("Recorder=null run");

                    IsRecording = false;
                    Debug.Print("...stopped");
                    //sw.WriteLine("Ended");
                }
                //sw.Close();
            }
        }

        private void UI_Play_Click(object sender, RoutedEventArgs e)
        {
            UI_Play.Visibility = Visibility.Collapsed;
            UI_Pause.Visibility = Visibility.Visible;
            SetInfoText(InfoIcon_AutoSpin, "Recording...");
            Recorder.Resume();
        }

        private void UI_Pause_Click(object sender, RoutedEventArgs e)
        {
            UI_Play.Visibility = Visibility.Visible;
            UI_Pause.Visibility = Visibility.Collapsed;
            SetInfoText(InfoIcon_Smile, "Paused...");
            Recorder.Pause();
        }

        private void UI_Stop_Click(object sender, RoutedEventArgs e)
        {
            CursorWait();

            SetInfoText(InfoIcon_Hourglass, "Finishing capture...");
            StopCapturer();
            SetInfoText(InfoIcon_Smile, $"Saved as '{Path.GetFileName(Filename)}'");

            UI_Record.Visibility = Visibility.Visible;
            UI_Screenshot.IsEnabled = false; //.Visibility = Visibility.Collapsed;
            UI_Pause.Visibility = Visibility.Collapsed;
            UI_Stop.IsEnabled = false;
            UI_Play.Visibility = Visibility.Collapsed;

            EnableDisableControl(Encoder_CaptureRaw, true);
            EnableDisableControl(Capture_Size, true, Capture_SizeL);
            SetLockedInfo(false);

            UI_SelectedAppWindow.IsEnabled = true;

            CursorRestore();
        }

        private void AppWindows_DropDownOpened(object sender, EventArgs e)
        {
            PopulateAppWindows();
        }

        private void UI_Settings_Click(object sender, RoutedEventArgs e)
        {
            //     double preHeight = PreviewHolder.ActualHeight;

            double width = Options.Width + Options.Margin.Left;

            if (Options.Visibility == Visibility.Visible)
            {
                Options.Visibility = Visibility.Collapsed;
                double origWidth = this.Width;
                this.MinWidth -= width;
                this.Width = origWidth - width;
            }
            else
            {
                Options.Visibility = Visibility.Visible;
                double origWidth = this.Width;
                this.MinWidth += width;
                this.Width = origWidth + width;
            }

            this.UpdateLayout();

            //            double postHeight = PreviewHolder.ActualHeight;
            //           double difference = preHeight - postHeight;
            //       //    this.MinHeight += difference;
            //           this.Height += difference;
            //           if (this.Height < this.MinHeight)
            //               this.Height = this.MinHeight;
        }

        private void SelectedAppWindow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedApp = (AppInfo)UI_SelectedAppWindow.SelectedItem;
            if (SelectedApp == null || SelectedApp.HWnd == IntPtr.Zero)
            {
                SelectedApp = null;
                UI_SelectedAppWindow.SelectedItem = null;

                SetInfoWithEffect(InfoIcon_Sad, "No application/window selected...");
            }
            else
            {
                Size size = Utils.GetSizeOfWindow(SelectedApp);
                SetInfoWithEffect(InfoIcon_Smile, $"Selected capture target size: {size.Width}x{size.Height}");
            }
        }

        private void SaveScreenshot()
        {
            if (SelectedApp == null || SelectedApp.HWnd == IntPtr.Zero)
            {
                SetInfoWithEffect(InfoIcon_Sad, "Select capture target before taking screenshots...");
                return;
            }

            //if (Recorder == null)
            //{
            //    SetInfoWithEffect(InfoIcon_Sad, "Start recording before taking screenshots");

            //    return;
            //}

            CursorWait();

            string icon = (InfoIconText?.Length == 0 ? InfoIcon_Smile : InfoIcon_AutoSpin);

            SetInfoText(InfoIcon_Hourglass, "Saving screenshot...");

            if (IsRecording && Recorder != null)
            {
                ScreenCapture.SnapScreen(RecorderParams.HWnd, 0, 0, RecorderParams.SrcWidth, RecorderParams.SrcHeight, RecorderParams.DestWidth, RecorderParams.DestHeight, SettingsWrangler.GeneralSettings.Capture_SizeHandling, false, Globals.ZoomInstruction, CurrentZoom, 0, 0, null, SettingsWrangler.GeneralSettings.Capture_OnlyOnContentChange, $"{ScreenshotFilename}.{ScreenshotCounter++}.png");
            }
            //else
            //     // No recording - no screen positioning! We have to pretend we have recording set up. We use the preview recorder for this :)

            SetInfoWithEffect(icon, "Screenshot taken!");

            CursorRestore();
        }

        // ToDo: Link functions to hotkey structure definitions and call directly - no switch needed then
        // At the very least, register hotkey data so we can hash lookup the key+modifers as a hash to what to do
        public IntPtr HotkeyHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int key = (((int)lParam >> 16) & 0xffff);
                int modifier = ((int)lParam & 0xffff);
                int hotkeyId = lParam.ToInt32();

                //switch (wParam.ToInt32())
                //{
                //    case 1:
                //        int vkey = (((int)lParam >> 16) & 0xFFFF);
                //        if (vkey == 2)
                //        {
                //            //handle global hot key here...
                //        }
                //        handled = true;
                //        break;
                //}

                if (modifier == (int)KeyModifiers.Alt)
                {
                    Debug.Print(key.ToString());
                    switch ((System.Windows.Forms.Keys)key)
                    {
                        case System.Windows.Forms.Keys.F1:
                            UI_Rate.SelectedValue = "1x";
                            break;
                        case System.Windows.Forms.Keys.F2:
                            UI_Rate.SelectedValue = "2x";
                            break;
                        case System.Windows.Forms.Keys.F3:
                            UI_Rate.SelectedValue = "3x";
                            break;
                        case System.Windows.Forms.Keys.F4:
                            UI_Rate.SelectedValue = "4x";
                            break;
                        case System.Windows.Forms.Keys.F5:
                            UI_Rate.SelectedValue = "5x";
                            break;
                        case System.Windows.Forms.Keys.F6:
                            UI_Rate.SelectedValue = "10x";
                            break;
                        case System.Windows.Forms.Keys.F7:
                            UI_Rate.SelectedValue = "20x";
                            break;
                        case System.Windows.Forms.Keys.F8:
                            UI_Rate.SelectedValue = "30x";
                            break;
                        case System.Windows.Forms.Keys.F9:
                            UI_Rate.SelectedValue = "40x";
                            break;
                        case System.Windows.Forms.Keys.F10:
                            UI_Rate.SelectedValue = "50x";
                            break;
                        case System.Windows.Forms.Keys.F11:
                            UI_Rate.SelectedValue = "60x";
                            break;
                        case System.Windows.Forms.Keys.F12:
                            UI_Rate.SelectedValue = "120x";
                            break;
                        case System.Windows.Forms.Keys.Escape:
                            SaveScreenshot();
                            break;
                        case System.Windows.Forms.Keys.Z:
                            if (Capture_ZoomInToCursorPosition.IsChecked ?? false)
                                Globals.ZoomInstruction = ZoomInstruction.InitOffset;

                            ZoomIn();
                            break;
                        case System.Windows.Forms.Keys.X:
                            ZoomOut();
                            break;
                        case System.Windows.Forms.Keys.A:
                            if (Capture_ZoomInToCursorPosition.IsChecked ?? false)
                                Globals.ZoomInstruction = ZoomInstruction.InitOffset;

                            ZoomInInstantly();
                            break;
                        case System.Windows.Forms.Keys.S:
                            ZoomOutInstantly();
                            break;

                    }
                    //if (key == Keys.F1.GetHashCode() && )
                    //{
                    //    //if (this.WindowState == FormWindowState.Normal)
                    //    //    MinimiseWindows();
                    //    //else
                    //    //    RestoreWindows();
                    //    int i = 1;
                    //}
                }
            }
            return IntPtr.Zero;
        }

        private void UI_Screenshot_Click(object sender, RoutedEventArgs e)
        {
            SaveScreenshot();
        }

        private void InitRate(float changePeriod)
        {
            //if (RateChangePeriod == null)
            //    return;

            //if (RateChangePeriod.SelectedItem == null)
            //    return;

            //if (int.TryParse(((ComboBoxItem)Rate.SelectedItem).Tag.ToString(), out rate))
            //{
            //    PreviousSpeedRate = SpeedRate;
            //    SpeedRate = rate;
            //    SpeedRateChanged = true;
            //    SpeedRateChangeHandled = false;
            //}

            Rate rate = ((Rate)UI_Rate.SelectedItem);
            {
                PreviousRate = Rate;

                if (changePeriod == 0.0d)
                {
                    Rate = rate.Speed;
                    TargetRateChange = 0;

                    SetVisual(RateVisual, RateVisualBorder, 0, 1, 1); // Make sure it displays a full bar. It's possible this can be called part way through a current rate change
                }
                else
                {
                    TargetRate = rate.Speed;
                    TargetRateChange = (TargetRate - Rate) / (changePeriod * InterfaceFrameRate); // we will update the SpeedRate 60fps, as things are recorded, it will pick this up as it goes

                }
                // SpeedRate = rate.Speed;

                RateChanged = true;
                RateChangeHandled = false;
            }
        }

        private void UI_Rate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Initializing)
                return;

            if (UI_Rate.SelectedIndex < 2)
                UI_Rate.SelectedIndex = 2;

            Rate rate = ((Rate)UI_Rate.SelectedItem);
            string rateText = rate.Description;
            string icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Smile);
            SetInfoWithEffect(icon, $"Rate changed to {rateText}");

            float rateChangePeriod;
            if (IsRecording)
                rateChangePeriod = float.Parse(((ComboBoxItem)UI_RateChangePeriod.SelectedItem).Tag.ToString());
            else
                rateChangePeriod = 0;

            // If we aren't recording, change now!
            InitRate(rateChangePeriod);

            SettingsWrangler.GeneralSettings.Rate = rateText;
            SettingsWrangler.GeneralSettings.SettingsChanged = true;
        }

        private void UI_RateChangePeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Initializing)
                return;

            string icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Smile);
            string rateChangeText = ((ComboBoxItem)e.AddedItems[0]).Content.ToString();

            SettingsWrangler.GeneralSettings.RateChangePeriod = rateChangeText;
            SettingsWrangler.GeneralSettings.SettingsChanged = true;

            rateChangeText = rateChangeText.Replace("sec", "second", StringComparison.CurrentCulture);
            rateChangeText = rateChangeText.Replace("min", "minute", StringComparison.CurrentCulture);
            SetInfoWithEffect(icon, $"Rate change period changed to {rateChangeText}");

            SetRateChangeInfo();
        }

        private void UI_Zoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Initializing)
                return;

            string zoomText = (string)UI_Zoom.SelectedValue;

            string icon;

            if (zoomText.StartsWith("Custom"))
            {
                icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Sad);
                SetInfoWithEffect(icon, $"Custom zoom not yet implimented.");
                UI_Zoom.SelectedIndex = 7; // default to 5x            }
            }
            else
            {
                icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Smile);
                SetInfoWithEffect(icon, $"Zoom changed to {zoomText}");

                // Zoom is not instigated by changing the zoom level - only changes when zoom buttons are pressed
                // So we save it for later!
                SelectedZoom = float.Parse(((ComboBoxItem)UI_Zoom.SelectedItem).Tag.ToString());
                ZoomDescription = UI_Zoom.SelectedValue.ToString();

                SetZoomChangeInfo();

                SettingsWrangler.GeneralSettings.Zoom = zoomText;
                SettingsWrangler.GeneralSettings.SettingsChanged = true;
            }
        }

        private void UI_ZoomChangePeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Initializing)
                return;

            string icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Smile);
            string zoomChangeText = ((ComboBoxItem)e.AddedItems[0]).Content.ToString();
            SetInfoWithEffect(icon, $"Zoom change period changed to {zoomChangeText}");

            ZoomChangePeriod = float.Parse(((ComboBoxItem)UI_ZoomChangePeriod.SelectedItem).Tag.ToString());

            SetZoomChangeInfo();


            SettingsWrangler.GeneralSettings.ZoomChangePeriod = zoomChangeText;
            SettingsWrangler.GeneralSettings.SettingsChanged = true;
        }

        // Instantly change zoom if we aren't recording
        private float GetZoomChangePeriod()
        {
            //   if (IsRecording)
            return ZoomChangePeriod;
            //   else
            //       return 0;
        }

        private void InitZoom(float zoom, float changePeriod)
        {

            PreviousZoom = CurrentZoom;

            if (changePeriod == 0.0f)
            {
                CurrentZoom = zoom;
                TargetZoomChange = 0;
            }
            else
            {
                TargetZoom = zoom;
                TargetZoomChange = (TargetZoom - CurrentZoom) / (changePeriod * InterfaceFrameRate); // we will update the SpeedRate 60fps, as things are recorded, it will pick this up as it goes

                if (Capture_RealtimeZoomRate.IsChecked ?? false)
                {
                    Globals.ZoomRateOverride = true;
                    Recorder?.StopThread?.Set();
                }
            }
            // SpeedRate = rate.Speed;

            ZoomChanged = true;
            ZoomChangeHandled = false;

            // Debug.Print($"Zoom init: Zoom={CurrentZoom}, PreviousZoom={PreviousZoom}, TargetZoom={TargetZoom}, TargetZoomChange={TargetZoomChange}, ChangeResolution={changePeriod}*{InterfaceFrameRate}={changePeriod * InterfaceFrameRate}, ZoomRateOverride={Globals.ZoomRateOverride}");
        }

        private void ZoomIn()
        {
            string icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Smile);
            SetInfoWithEffect(icon, "Zooming into {ZoomDescription}");

            InitZoom(SelectedZoom, GetZoomChangePeriod());
        }

        private void ZoomInInstantly()
        {
            string icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Smile);
            SetInfoWithEffect(icon, "Zooming into {ZoomDescription} instantly");

            InitZoom(SelectedZoom, 0);
        }

        private void ZoomOut()
        {
            string icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Smile);
            SetInfoWithEffect(icon, "Zooming out to normal");

            InitZoom(1, GetZoomChangePeriod());
        }

        private void ZoomOutInstantly()
        {
            string icon = (InfoIconText?.Length == 0 ? InfoIcon_AutoSpin : InfoIcon_Smile);
            SetInfoWithEffect(icon, "Zooming out to normal instantly");

            InitZoom(1, 0);
        }

        private void UI_ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void UI_ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        /// <summary>
        /// Returns Width and Height from a string `WidthxHeight` (e.g. 800x600)
        /// </summary>
        /// <param name="Size"></param>
        /// <returns></returns>
        public static Size DimensionsFromSize(string Size)
        {
            string[] components = Size.Split('x');
            var size = new Size
            {
                Width = int.Parse(components[0]),
                Height = int.Parse(components[1])
            };

            return size;
        }

        private void Capture_SizeHandling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Initializing)
                return;

            InitSizeHandling();

            SettingsWrangler.GeneralSettings.SettingsChanged = true;
        }

        private void Capture_Size_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Initializing)
                return;

            InitSizeHandling();

            SettingsWrangler.GeneralSettings.SettingsChanged = true;
        }

        private void UI_ZoomInInstant_Click(object sender, RoutedEventArgs e)
        {
            ZoomInInstantly();
        }

        private void UI_ZoomOutInstant_Click(object sender, RoutedEventArgs e)
        {
            ZoomOutInstantly();
        }

        private void Options_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Initializing)
                return;

            if (Options.ActualHeight == 0)
                return;

            if (Options.Visibility != Visibility.Visible)
                return;

            double preHeight = Options.ActualHeight;
            this.UpdateLayout();
            double postHeight = Options.ActualHeight;

            double difference = postHeight - preHeight;
            //    this.MinHeight += difference;
            this.Height += difference;
            if (this.Height < this.MinHeight)
                this.Height = this.MinHeight;
        }

        private void Capture_IncludeCursor_Click(object sender, RoutedEventArgs e)
        {
            Capture_IncludeCursor_Changed();
        }

        private void Capture_IncludeCursor_Changed()
        {
            SettingsWrangler.GeneralSettings.Capture_IncludeCursor = Capture_IncludeCursor.IsChecked ?? false;
            SettingsWrangler.GeneralSettings.SettingsChanged = true;

            Set_UI_IncludeCursor();
        }

        private void UI_IncludeCursor_Click(object sender, RoutedEventArgs e)
        {
            Capture_IncludeCursor.IsChecked = !(Capture_IncludeCursor.IsChecked ?? false);

            Capture_IncludeCursor_Changed();
        }

        private void Set_UI_IncludeCursor()
        {
            if (Capture_IncludeCursor.IsChecked ?? false)
                UI_IncludeCursor.Foreground = Brushes.Black;
            else
                UI_IncludeCursor.Foreground = Brushes.LightGray;
        }

        private void Capture_IncludeHighlight_Click(object sender, RoutedEventArgs e)
        {
            Capture_IncludeHighlight_Changed();
        }

        private void Capture_IncludeHighlight_Changed()
        {
            SettingsWrangler.GeneralSettings.Capture_IncludeHighlight = Capture_IncludeHighlight.IsChecked ?? false;
            SettingsWrangler.GeneralSettings.SettingsChanged = true;

            Set_UI_IncludeHighlight();
        }

        private void UI_IncludeHighlight_Click(object sender, RoutedEventArgs e)
        {
            Capture_IncludeHighlight.IsChecked = !(Capture_IncludeHighlight.IsChecked ?? false);

            Capture_IncludeHighlight_Changed();
        }

        private void Set_UI_IncludeHighlight()
        {
            if (Capture_IncludeHighlight.IsChecked ?? false)
                UI_IncludeHighlight.Foreground = Brushes.Orange;
            else
                UI_IncludeHighlight.Foreground = Brushes.LightGray;
        }

        private void Capture_SkipDuplicateFrames_Click(object sender, RoutedEventArgs e)
        {
            Capture_SkipDuplicateFrames_Changed();
        }

        private void Capture_SkipDuplicateFrames_Changed()
        {
            SettingsWrangler.GeneralSettings.Capture_OnlyOnContentChange = Capture_SkipDuplicateFrames.IsChecked ?? false;
            SettingsWrangler.GeneralSettings.SettingsChanged = true;

            Set_UI_SkipDuplicateFrames();
        }

        private void UI_SkipDuplicateFrames_Click(object sender, RoutedEventArgs e)
        {
            Capture_SkipDuplicateFrames.IsChecked = !(Capture_SkipDuplicateFrames.IsChecked ?? false);

            Capture_SkipDuplicateFrames_Changed();
        }
        private void Set_UI_SkipDuplicateFrames()
        {
            if (Capture_SkipDuplicateFrames.IsChecked ?? false)
            {
                UI_SkipDuplicateFrames_Bottom.Foreground = Brushes.Gray;
                UI_SkipDuplicateFrames_Middle.Foreground = Brushes.Gray;
                UI_SkipDuplicateFrames_Top.Foreground = Brushes.Red;
            }
            else
            {
                UI_SkipDuplicateFrames_Bottom.Foreground = Brushes.LightGray;
                UI_SkipDuplicateFrames_Middle.Foreground = Brushes.LightGray;
                UI_SkipDuplicateFrames_Top.Foreground = Brushes.LightGray;
            }
        }

        private void Capture_ZoomInToCursorPosition_Click(object sender, RoutedEventArgs e)
        {
            Capture_ZoomInToCursorPosition_Changed();
        }

        private void Capture_ZoomInToCursorPosition_Changed()
        {
            SettingsWrangler.GeneralSettings.Capture_ZoomInToCursorPosition = Capture_ZoomInToCursorPosition.IsChecked ?? false;
            SettingsWrangler.GeneralSettings.SettingsChanged = true;

            if (SettingsWrangler.GeneralSettings.Capture_ZoomInToCursorPosition == false)
                Globals.ZoomInstruction = ZoomInstruction.ResetOffset;
            else
                MessageBox.Show("Position to zoom in on will be set to the current cursor position when zoom is triggered by a hot key", "Zoom position information", MessageBoxButton.OK, MessageBoxImage.Information);                

            Set_UI_ZoomInToCursorPosition();
        }

        private void UI_ZoomInToCursorPosition_Click(object sender, RoutedEventArgs e)
        {
            Capture_ZoomInToCursorPosition.IsChecked = !(Capture_ZoomInToCursorPosition.IsChecked ?? false);

            Capture_ZoomInToCursorPosition_Changed();
        }

        private void Set_UI_ZoomInToCursorPosition()
        {
            if (Capture_ZoomInToCursorPosition.IsChecked ?? false)
                UI_ZoomInToCursorPosition.Foreground = Brushes.Black;
            else
                UI_ZoomInToCursorPosition.Foreground = Brushes.LightGray;
        }

        private void Capture_HighQualityZoom_Click(object sender, RoutedEventArgs e)
        {
            Capture_HighQualityZoom_Changed();
        }

        private void Capture_HighQualityZoom_Changed()
        {
            SettingsWrangler.GeneralSettings.Capture_HighQualityZoom = Capture_HighQualityZoom.IsChecked ?? false;
            SettingsWrangler.GeneralSettings.SettingsChanged = true;

            Set_UI_HighQualityZoom();
        }

        private void UI_HighQualityZoom_Click(object sender, RoutedEventArgs e)
        {
            Capture_HighQualityZoom.IsChecked = !(Capture_HighQualityZoom.IsChecked ?? false);

            Capture_HighQualityZoom_Changed();
        }

        private void Set_UI_HighQualityZoom()
        {
            if (Capture_HighQualityZoom.IsChecked ?? false)
                UI_HighQualityZoom.Foreground = Brushes.Black;
            else
                UI_HighQualityZoom.Foreground = Brushes.LightGray;
        }

    }
}
