using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpAvi;
using SharpAvi.Codecs;
using SharpAvi.Output;

namespace LapseOTron
{
    public static class Utils
    {
        public static Size GetSizeOfWindow(AppInfo appInfo)
        {
            Size size;
            IntPtr hWnd = appInfo.HWnd;

            if (hWnd == IntPtr.Zero)
            {
                System.Windows.Media.Matrix toDevice;

                using (var source = new HwndSource(new HwndSourceParameters()))
                    toDevice = source.CompositionTarget.TransformToDevice;

                size.Width = (int)Math.Round(SystemParameters.PrimaryScreenWidth * toDevice.M11);
                size.Height = (int)Math.Round(SystemParameters.PrimaryScreenHeight * toDevice.M22);
            }
            else
            {
                //      var rect = new IntRect();
                //      User32.GetWindowRect(hWnd, ref rect);

                IntRect rect = appInfo.CurrentWindowRect(); // User32.GetWindowRect(hWnd, ref rect);

                size.Width = rect.Right - rect.Left;
                size.Height = rect.Bottom - rect.Top;

                // we HAVE to have an even width and height, otherwise the codec gives us a big phat middle finger
                if (size.Width % 2 == 1)
                    size.Width--;
                if (size.Height % 2 == 1)
                    size.Height--;
            }

            return size;
        }
    }

    public class RecorderParams
    // Used to Configure the Recorder
    {
        public IntPtr HWnd;

        string FileName { get; set; }
        public int FramesPerSecond { get; set; }
        public int Quality { get; set; }
        public int AudioSourceId { get; set; }
        public int AudioBitRate { get; set; }
        private FourCC Codec { get; set; }
        public bool EncodeAudio { get; set; }
        //public bool IncludeCursor { get; set; }

        public int SrcHeight { get; private set; }
        public int SrcWidth { get; private set; }
        public int DestHeight { get; private set; }
        public int DestWidth { get; private set; }

        ////static void InitLame()
        ////{
        ////    // Set LAME DLL path for MP3 encoder
        ////    Mp3AudioEncoderLame.SetLameDllLocation(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
        ////        string.Format("lameenc{0}.dll", Environment.Is64BitProcess ? "64" : "32")));
        ////}

        public RecorderParams(string filename, int frameRate, FourCC encoder, int quality,
            int audioSourceId, bool useStereo, bool encodeAudio, int audioQuality, bool includeCursor, AppInfo appInfo, Size destSize)
        {
            //Debug.Print("RecorderParams created");

            FileName = filename;
            FramesPerSecond = frameRate;
            Codec = encoder;
            Quality = quality;
            AudioSourceId = audioSourceId;
            EncodeAudio = encodeAudio;
            //AudioBitRate = Mp3AudioEncoderLame.SupportedBitRates.OrderBy(br => br).ElementAt(audioQuality);
            //IncludeCursor = includeCursor;
            this.HWnd = appInfo.HWnd;

            //if (hWnd == IntPtr.Zero)
            //{
            //    System.Windows.Media.Matrix toDevice;

            //    using (var source = new HwndSource(new HwndSourceParameters()))
            //        toDevice = source.CompositionTarget.TransformToDevice;

            //    Height = (int)Math.Round(SystemParameters.PrimaryScreenHeight * toDevice.M22);
            //    Width = (int)Math.Round(SystemParameters.PrimaryScreenWidth * toDevice.M11);
            //}
            //else
            //{
            //    //      var rect = new IntRect();
            //    //      User32.GetWindowRect(hWnd, ref rect);

            //    IntRect rect = appInfo.CurrentWindowRect(); // User32.GetWindowRect(hWnd, ref rect);

            //    Width = rect.Right - rect.Left;
            //    Height = rect.Bottom - rect.Top;

            //    // we HAVE to have an even width and height, otherwise the codec gives us a big phat middle finger
            //    if (Width % 2 == 1)
            //        Width--;
            //    if (Height % 2 == 1)
            //        Height--;
            //}

            Size size = Utils.GetSizeOfWindow(appInfo);
            SrcWidth = size.Width;
            SrcHeight = size.Height;

            if (destSize.Width == -1)
                DestWidth = SrcWidth;
            else
                DestWidth = destSize.Width;

            if (destSize.Height == -1)
                DestHeight = SrcHeight;
            else
                DestHeight = destSize.Height;

            ////WaveFormat = new WaveFormat(44100, 16, useStereo ? 2 : 1);
        }

        public AviWriter CreateAviWriter()
        {
            return new AviWriter(FileName)
            {
                FramesPerSecond = FramesPerSecond,
                EmitIndex1 = true,
            };
        }

        public IAviVideoStream CreateVideoStream(AviWriter writer)
        {
            // Select encoder type based on FOURCC of codec
            if (Codec == KnownFourCCs.Codecs.Uncompressed)
                return writer.AddUncompressedVideoStream(DestWidth, DestHeight);
            else if (Codec == KnownFourCCs.Codecs.MotionJpeg)
                return writer.AddMotionJpegVideoStream(DestWidth, DestHeight, Quality);
            else
            {
                return writer.AddMpeg4VideoStream(DestWidth, DestHeight, (double)writer.FramesPerSecond,
                    // It seems that all tested MPEG-4 VfW codecs ignore the quality affecting parameters passed through VfW API
                    // They only respect the settings from their own configuration dialogs, and Mpeg4VideoEncoder currently has no support for this
                    quality: Quality,
                    codec: Codec,
                    // Most of VfW codecs expect single-threaded use, so we wrap this encoder to special wrapper
                    // Thus all calls to the encoder (including its instantiation) will be invoked on a single thread although encoding (and writing) is performed asynchronously
                    forceSingleThreadedAccess: true);
            }
        }

        ////public IAviAudioStream CreateAudioStream(AviWriter writer)
        ////{
        ////    // Create encoding or simple stream based on settings
        ////    if (EncodeAudio)
        ////    {
        ////        // LAME DLL path is set in App.OnStartup()
        ////        return writer.AddMp3AudioStream(WaveFormat.Channels, WaveFormat.SampleRate, AudioBitRate);
        ////    }
        ////    else return writer.AddAudioStream(WaveFormat.Channels, WaveFormat.SampleRate, WaveFormat.BitsPerSample);
        ////}

        ////public WaveFormat WaveFormat { get; private set; }
    }

    public class Recorder : IDisposable
    {
        #region Fields
        AviWriter Writer { get; set; }
        RecorderParams RecorderParams { get; set; }
        IAviVideoStream VideoStream { get; set; }
        //IAviAudioStream audioStream;
        ////WaveInEvent audioSource;
        Thread ScreenThread { get; set; }

        //the difference between AutoResetEvent and ManualResetEvent is that as soon as you call.Set, all blocked threads will be released and will bypass WaitOne immediately.AutoResetEvent on the other hand will unblock exactly one thread and then Reset itself -  will bypass WaitOne immediately until you call .Reset()*
        public ManualResetEvent StopThread { get; set; } = new ManualResetEvent(false);
        private AutoResetEvent VideoFrameWritten { get; set; } = new AutoResetEvent(false);
        private AutoResetEvent AudioBlockWritten { get; set; } = new AutoResetEvent(false);
        public bool IsPaused { get; set; } = false;
        //        byte[] Buffer;
        int BufferMilliseconds;
        int FramesPerSecond { get; set; }

        bool StopNow { get; set; } = false;

        //public StreamWriter SW { get; set; }

        MainWindow MainParent { get; set; }

        private int FrameNumber { get; set; }
        //private int msCount;

        #endregion

        //public Preview(RecorderParams Params, MainWindow mainParent)
        //{

        //}
        private object ThreadLock { get; set; } = new object();

        public Recorder(RecorderParams Params, MainWindow mainParent)
        {
            MainParent = mainParent;

            Debug.Print("Recorder Created");

            // Set LAME DLL path for MP3 encoder
            //Mp3AudioEncoderLame.SetLameDllLocation(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), string.Format("lib\\libmp3lame{0}.dll", Environment.Is64BitProcess ? "64" : "32")));

            this.RecorderParams = Params;

            //            Buffer = new byte[Params.SrcWidth * Params.SrcHeight * 4];

            // Create AVI writer and specify FPS
            Writer = Params.CreateAviWriter();

            // Create video stream
            VideoStream = Params.CreateVideoStream(Writer);
            // Set only name. Other properties were when creating stream, 
            // either explicitly by arguments or implicitly by the encoder used
            VideoStream.Name = "Lapse-O-Tron";

            FramesPerSecond = (int)Writer.FramesPerSecond;
            BufferMilliseconds = (int)Math.Ceiling(1000 / Writer.FramesPerSecond);

            ////if (Params.AudioSourceId != -1)
            ////{
            ////    try
            ////    {
            ////        var waveFormat = Params.WaveFormat;

            ////        audioStream = Params.CreateAudioStream(writer);
            ////        audioStream.Name = "Audio";

            ////        audioSource = new WaveInEvent
            ////        {
            ////            DeviceNumber = Params.AudioSourceId,
            ////            WaveFormat = waveFormat,
            ////            // Buffer size to store duration of 1 frame
            ////            BufferMilliseconds = bufferMilliseconds,
            ////            NumberOfBuffers = 3,
            ////        };

            ////        audioSource.DataAvailable += AudioDataAvailable;
            ////    }
            ////    catch (Exception e)
            ////    {
            ////        Debug.Print("Error initializing audio source - {0}", e.Message);
            ////    }
            ////}
            ///

            // DONT LIKET HIS HERE! MOVE IT
            // Adjust preview so it is effectivley the same size as our destination bitmap (viewport will scale things accordingly)
            MainParent.SetPreviewSize((double)RecorderParams.DestWidth, (double)RecorderParams.DestHeight);

            ScreenThread = new Thread(RecordScreen)
            {
                Name = typeof(Recorder).Name + ".RecordScreen",
                IsBackground = true
            };

            FrameNumber = 0;
            //msCount = 0;
            ////if (audioSource != null)
            ////{
            ////    videoFrameWritten.Set();
            ////    audioBlockWritten.Reset();
            ////    audioSource.StartRecording();
            ////}
            ScreenThread.Start();
        }

        //public void Stop()
        //{
        //    StopNow = true;
        //}

        public void Dispose()
        {
            StopNow = true;

            if (IsPaused)
            {
                //SW.WriteLine("Resuming");
                Resume();
                //SW.WriteLine("Resumed");
            }

            //SW.Flush();
            //SW.WriteLine("Stopping Thread");
            StopThread.Set();
            //SW.WriteLine("Stopped");
            //SW.Flush();

            //Task.Run(() => screenThread.Join()).Wait();
            ScreenThread.Join();

            //SW.Flush();
            //SW.WriteLine("Joined");

            ////if (audioSource != null)
            ////{
            ////    audioSource.StopRecording();
            ////    audioSource.DataAvailable -= AudioDataAvailable;
            ////}

            // Close writer: the remaining data is written to a file and file is closed
            Writer.Close();
            //SW.WriteLine("Closed");

            StopThread.Close();
            //SW.WriteLine("stopThread closed");
        }

        public void Pause()
        {
            if (!IsPaused)
            {
                //ScreenThread.Suspend();

                ////if (audioSource != null) audioSource.StopRecording();

                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsPaused)
            {
                //ScreenThread.Resume();

                ////if (audioSource != null)
                ////{
                ////    videoFrameWritten.Set();
                ////    audioBlockWritten.Reset();
                ////    audioSource.StartRecording();
                ////}

                IsPaused = false;

                lock (ThreadLock)
                {
                    Monitor.Pulse(ThreadLock);
                }
            }
        }

        void RecordScreen()
        {
            // measure time intervals in fractions of ms - a 30fps stream will need 33.33333' ms's per frame.
            double frameInterval = (1.0d / (double)Writer.FramesPerSecond) * 1000.0d; // TimeSpan.FromSeconds(1 / (double)writer.FramesPerSecond);

            // 2 frame buffers, where we can iterate through and check for differences
            var frameBufferA = new byte[RecorderParams.DestWidth * RecorderParams.DestHeight * 4];
            var frameBufferB = new byte[RecorderParams.DestWidth * RecorderParams.DestHeight * 4];
            byte[] frameBufferTmp;

            Task videoWriteTask = null;
            //var isFirstFrame = true;
            ////var timeTillNextFrame = TimeSpan.Zero;
            ////double previousTime = 0.0d;
            double currentTime = 0.0d;
            double expectedTime = 0.0;
            double timeTillNextFrame = 0;
            long lastGC = 0;
            var stopwatch = new Stopwatch();
            var RateChangeWatch = new Stopwatch();
            // var zoomWatch = new Stopwatch();
            TimeSpan approxTotalTime = new TimeSpan(0);
            TimeSpan totalTime = new TimeSpan(0);
            long frameInTicks = TimeSpan.TicksPerMillisecond * 30;         // 1/60fps = 30ms per frame

            stopwatch.Start();
            RateChangeWatch.Start();
            // zoomWatch.Start();

            StringBuilder timeInfo = new StringBuilder();

            MainParent.SetInfoTextIconToSpinner();
            MainParent.SetDestinationSizeInfo(RecorderParams.DestWidth, RecorderParams.DestHeight);

            while (!StopNow) //!StopThread.WaitOne((int)timeTillNextFrame))
            {
                Debug.Print($"{timeTillNextFrame}");
                StopThread.Reset();
                StopThread.WaitOne((int)timeTillNextFrame);

                if (!StopNow)   // might have been set while waiting
                {
                    if (IsPaused)
                    {
                        lock (ThreadLock)
                        {
                            Monitor.Wait(ThreadLock);
                        }
                    }

                    //currentTime =  // = DateTime.Now;

                    // Time wise, the point of the screenshot is when the screen is captured, so how long it takes to write to the video is not important, just as long as it does it before the next frame comes along!

                    // swap frame buffers, for when we want to do comparisons
                    frameBufferTmp = frameBufferA;
                    frameBufferA = frameBufferB;
                    frameBufferB = frameBufferTmp;

                    //ZoomInstruction zoomInstruction = ZoomInstruction.JustChillDude;
                    BitmapSource newScreenshotImage = ScreenCapture.SnapScreen(RecorderParams.HWnd, 0, 0, RecorderParams.SrcWidth, RecorderParams.SrcHeight, RecorderParams.DestWidth, RecorderParams.DestHeight, SettingsWrangler.GeneralSettings.Capture_SizeHandling, true, Globals.ZoomInstruction, MainParent.CurrentZoom, 0, 0, frameBufferA, SettingsWrangler.GeneralSettings.Capture_OnlyOnContentChange);

                    if (Globals.ContentChanged)
                    {
                        // Only write if content has changed (which will be always if we aren't checking)

                        //ImageSourceConverter c = new ImageSourceConverter();
                        //ImageSource imgSrc = (ImageSource)c.ConvertFrom(newBitmap);
                        //MainParent.Preview.Source = imgSrc;

                        // Wait for the previous frame is written
                        //if (!isFirstFrame)
                        //{
                        videoWriteTask?.Wait();
                        VideoFrameWritten.Set();
                        //}

                        // write frames of video till we catch up with current time required (yes there could be some duplicates, but bad luck)
                        //while (currentTime >= expectedTime)
                        //{
                        // Start asynchronous (encoding and) writing of the new frame
                        videoWriteTask = VideoStream.WriteFrameAsync((FrameNumber % FramesPerSecond) == 0, frameBufferA, 0, frameBufferA.Length);

                        ////currentTime -= frameInterval; // close in on expected time.
                    }

                    int rate;
                    if (Globals.ZoomRateOverride)
                        rate = 1;
                    else
                        rate = (int)MainParent.Rate;

                    expectedTime += frameInterval * rate; // <---- * multiplier

                    //// wait a moment for things to get written too
                    //////if (currentTime > expectedTime)
                    //////{
                    //videoWriteTask?.Wait();
                    //videoFrameWritten.Set();
                    //////}
                    //}

                    ////if (audioStream != null)
                    ////{
                    ////    var signalled = WaitHandle.WaitAny(new WaitHandle[] { audioBlockWritten, stopThread });
                    ////    if (signalled == 1) break;
                    ////}

                    MainParent.NewScreenshot = newScreenshotImage;

                    //newBitmap.Dispose();
                    //newBitmap = null;

                    timeInfo.Clear();

                    TimeSpan span = stopwatch.Elapsed;
                    int elapsedHours = span.Hours;
                    int elapsedMinutes = span.Minutes;
                    int elapsedSeconds = span.Seconds;
                    int elapsedMS = span.Milliseconds / 10;

                    bool showHours = (elapsedHours > 0);
                    bool showMinutes = (elapsedMinutes > 0);

                    timeInfo.Append("Recording ");

                    if (showHours)
                        timeInfo.Append($"{elapsedHours:00}:");

                    if (showMinutes)
                        timeInfo.Append($"{elapsedMinutes:00}:");

                    timeInfo.Append($"{elapsedSeconds:00}.{elapsedMS:00}");

                    string extra;
                    int extraHours;
                    int extraMinutes;
                    int extraSeconds;
                    int extraMS;

                    if (MainParent.RateChanged)
                    {
                        if (!MainParent.RateChangeHandled)
                        {
                            // Init if just changed

                            // We want to add the previous chunk of time we have been running, at the previous speed rate
                            MainParent.RateChangeHandled = true;

                            int prevRate = (int)MainParent.PreviousRate;
                            approxTotalTime = approxTotalTime.Add(new TimeSpan((prevRate == 0 ? 0 : RateChangeWatch.Elapsed.Ticks / prevRate)));
                            RateChangeWatch.Restart();
                        }
                        //extra = "@ varying speed rates";

                        // current timespan at current rate
                        span = new TimeSpan(RateChangeWatch.Elapsed.Ticks / rate);

                        if (Globals.ContentChanged) // Only add if we aren't ignoring
                                                    // added to previous total time
                            span = span.Add(approxTotalTime);

                        extraHours = span.Hours;
                        extraMinutes = span.Minutes;
                        extraSeconds = span.Seconds;
                        extraMS = span.Milliseconds / 10;

                        timeInfo.Append(" ⇨ approx. ");
                    }
                    else
                    {
                        span = new TimeSpan((rate == 0 ? 0 : stopwatch.Elapsed.Ticks / rate));

                        extraHours = span.Hours;
                        extraMinutes = span.Minutes;
                        extraSeconds = span.Seconds;
                        extraMS = span.Milliseconds / 10;

                        // Below line is currently being errored
                        // ToDo: Revery when it formats properly
                        timeInfo.Append(" ⇨ ");
                    }

                    if (showHours)
                        timeInfo.Append($"{extraHours:00}:");

                    if (showMinutes)
                        timeInfo.Append($"{extraMinutes:00}:");

                    timeInfo.Append($"{extraSeconds:00}.{extraMS:00}"); // " / ");

                    //float zoom = MainParent.Zoom;
                    //float zoomChangePeriod = MainParent.ZoomChangePeriod;

                    //// We handle the zoom differently to the change rate....
                    //if (MainParent.ZoomChanged)
                    //{
                    //    // Some from MainParent.PreviousZoom to MainParent.Zoom
                    //    if (!MainParent.ZoomChangeHandled)
                    //    {
                    //        // Init if just changed
                    //        MainParent.ZoomChangeHandled = true;

                    //        float prevZoom = MainParent.PreviousZoom;
                    //        zoomWatch.Restart();
                    //    }

                    //    // current timespan at current rate
                    //    span = new TimeSpan(zoomWatch.Elapsed.Ticks / zoomChangePeriod);

                    //}

                    //totalTime = totalTime.Add(new TimeSpan(frameInTicks));
                    //
                    //int totalHours = totalTime.Hours;
                    //int totalMinutes = totalTime.Minutes;
                    //int totalSeconds = totalTime.Seconds;
                    //int totalMS = totalTime.Milliseconds / 10;
                    //
                    //if (showHours)
                    //    timeInfo.Append($"{totalHours:00}:");
                    //
                    //if (showMinutes)
                    //    timeInfo.Append($"{totalMinutes:00}:");
                    //
                    //timeInfo.Append($"{totalSeconds:00}.{totalMS:00}");
                    Debug.Print(Globals.ContentChanged.ToString());

                    MainParent.SetInfoText(timeInfo.ToString(), true);

                    // We have now have written frame(s) up to the current time required.
                    // We now want to wait a few MS until the next frame should kick in
                    long ms = stopwatch.ElapsedMilliseconds;
                    timeTillNextFrame = expectedTime - ms; // (expectedTime + frameInterval) - currentTime;
                    if (timeTillNextFrame < 0)
                        // This could occure, e.g. starting up, and its taking longer than 1 frame to actually write the stuff
                        timeTillNextFrame = 0;

                    //if (ms - lastGC > 1000)
                    //{
                    //    GC.Collect();                       // If required.... first collect gets things onto the finalizer que
                    //    GC.WaitForPendingFinalizers();    // Then you want for it to be ready
                    //    GC.Collect();
                    //    lastGC = ms;
                    //}


                    ////timeTillNextFrame = timestamp + frameInterval - DateTime.Now;
                    ////if (timeTillNextFrame < TimeSpan.Zero) timeTillNextFrame = TimeSpan.Zero;
                    //isFirstFrame = false;
                }
            }

            //// Wait for the last frame is written
            //if (!isFirstFrame) videoWriteTask.Wait();
            videoWriteTask?.Wait();
        }
    }
}