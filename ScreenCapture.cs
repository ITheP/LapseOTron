#define DEBUG_SHOW_TIMINGS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LapseOTron;

namespace LapseOTron
{
    public enum ZoomInstruction
    {
        /// <summary>
        /// Do nothing
        /// </summary>
        JustChillDude = 0,
        /// <summary>
        /// Uses current offset of mouse from centre of window as where we zoom into (bordering at the edge of window if is outside)
        /// </summary>
        InitOffset = 1,
        /// <summary>
        ///  Resets offset to nothing
        /// </summary>
        ResetOffset = 2
    }

    public static class ScreenCapture
    {

        private static SolidBrush Highlight_FillBrush { get; set; } = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((byte)0x77, (byte)0xFF, (byte)0x99, (byte)0x99));

        private static SolidBrush Highlight_Brush { get; set; } = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((byte)0xCC, (byte)0xFF, (byte)0x99, (byte)0x99));
        private static System.Drawing.Pen Highlight_Pen { get; set; } = new System.Drawing.Pen(Highlight_Brush, 5.0F);

        private static int ContentChange_SrcLastWidth { get; set; }
        private static int ContentChange_SrcLastHeight { get; set; }

        private static int ContentChange_DestLastWidth { get; set; }
        private static int ContentChange_DestLastHeight { get; set; }

        private static byte[] ContentChange_PreviousFrameBuffer { get; set; }

        private static Bitmap BitmapFromScreen { get; set; }
        private static Graphics BitmapGraphics { get; set; }
        // FOR SOME REASON pushing everything through a graphics buffer = faster/less overhead!
        private static BufferedGraphics GraphicsBuffer { get; set; }

        private static float zoomCenterX { get; set; }
        private static float zoomCenterY { get; set; }
        private static int RawScreenBitmap_Width { get; set; }
        private static int RawScreenBitmap_Height { get; set; }
        private static IntPtr RawScreenBitmap { get; set; } = IntPtr.Zero;
        private static IntPtr GeneralHWnd { get; set; } = IntPtr.Zero;
        private static IntPtr GeneralhDC { get; set; }
        private static IntPtr GeneralhMemDC { get; set; }

        /// <summary>

        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        /// 

        /// <summary>
        /// Want to make sure any bitmaps used by the below don't hang around.
        /// If they are not required - dispose/de-reference them.
        /// If they are used, dont forget to dispose/de-reference them!
        /// We only return what is required. Screenshots are pretty much always wanted, so that is the default return type.
        /// We hold onto bitmaps, context, etc. between calls to reuse and try and avoid continuous bitmap recreation overhead.
        /// WARNING - any calculations around passing in offsets or cropped source sizes should account for returned window sizes that may be smaller than anticipated.
        /// e.g. a 1920x1080 window, sized perfectly for video of that size, if cropped to remove a border, may return a smaller than 1920x1080 bitmap. If this is then scaled to fit,
        /// a 1920x1080 resulting video, quality may drop.
        /// </summary>
        /// <param name="hWnd">hWnd of window we are capturing</param>
        /// <param name="srcOffsetX">Offset into window (e.g. to remove borders)</param>
        /// <param name="srcOffsetY">Offset into window (e.g. to remove borders)</param>
        /// <param name="srcWidth">Width of source window - may be smaller than actual size, e.g. to help remove borders (along with the srcOffsetX setting)</param>
        /// <param name="srcHeight">Height of source window - may be smaller than actual size, e.g. to help remove borders (along with the srcOffsetY setting)</param>
        /// <param name="destWidth">Destination width</param>
        /// <param name="destHeight">Destination height</param>
        /// <param name="sizeHandling">How to clip/squash/etc. the source image into the destination</param>
        /// <param name="returnScreenshot"></param>
        /// <param name="zoomInstruction"></param>
        /// <param name="zoom"></param>
        /// <param name="zoomX"></param>
        /// <param name="zoomY"></param>
        /// <param name="buffer">Buffer to stick resulting data into. Should be destination size in size. If no buffer is given, then this will just return a screenshot bitmap (e.g. for preview purposes)</param>
        /// <returns></returns>

        /// <param name="checkIfContentChanged"></param><param name="filename"></param>
        public static BitmapSource SnapScreen(IntPtr hWnd, int srcOffsetX, int srcOffsetY, int srcWidth, int srcHeight, int destWidth, int destHeight, SizeHandling sizeHandling, bool returnScreenshot, ZoomInstruction zoomInstruction = ZoomInstruction.JustChillDude, float zoom = 1.0F, int zoomX = 0, int zoomY = 0, byte[] buffer = null, bool checkIfContentChanged = false, string filename = null)
        {
            //  if (GeneralhDC == IntPtr.Zero)
            //  {
            GeneralhDC = NativeMethods.GetWindowDC(GeneralHWnd);
            GeneralhMemDC = NativeMethods.CreateCompatibleDC(GeneralhDC);
            //  }

            #region Include Cursor
            ////IntPtr hIcon = IntPtr.Zero;

            // We also check to see (if required) if the current frame is different to the last frame
            bool contentChanged = false;
            //bool checkIfContentChanged = SettingsWrangler.GeneralSettings.Capture_OnlyOnContentChange;

            var rect = new IntRect();
            NativeMethods.GetWindowRect(hWnd, ref rect);
            int x = rect.Left + srcOffsetX;
            int y = rect.Top + srcOffsetY;

            if (checkIfContentChanged && buffer != null)
            {
                if (ContentChange_SrcLastWidth != srcWidth || ContentChange_SrcLastHeight != srcHeight || ContentChange_DestLastWidth != destWidth || ContentChange_DestLastHeight != destHeight)
                {
                    contentChanged = true;
                    ContentChange_SrcLastWidth = srcWidth;
                    ContentChange_SrcLastHeight = srcHeight;
                    ContentChange_DestLastWidth = destWidth;
                    ContentChange_DestLastHeight = destHeight;
                }
                else if (buffer != null && ContentChange_PreviousFrameBuffer != null && buffer.Length != ContentChange_PreviousFrameBuffer.Length)
                    contentChanged = false;
            }

            ////int CursorX = 0, CursorY = 0;

            //IntPtr RawScreenBitmap = NativeMethods.CreateCompatibleBitmap(hDC, srcWidth, srcHeight);

            // Create raw bitmap if missing or size has changed
            // Note that created bitmap does not rely on the hDC hanging around to be re-usable, as long as future uses use a compatible format
            if (RawScreenBitmap == IntPtr.Zero)
            {
                RawScreenBitmap = NativeMethods.CreateCompatibleBitmap(GeneralhDC, srcWidth, srcHeight);
            }
            else if (RawScreenBitmap_Width != srcWidth || RawScreenBitmap_Height != srcHeight)
            {
                NativeMethods.DeleteObject(RawScreenBitmap);
                RawScreenBitmap = NativeMethods.CreateCompatibleBitmap(GeneralhDC, srcWidth, srcHeight);
                RawScreenBitmap_Width = srcWidth;
                RawScreenBitmap_Height = srcHeight;
            }

            // ToDo: Throw exception - should never happen
            if (RawScreenBitmap == IntPtr.Zero)
            {
                return null;
            }

            IntPtr hOld = NativeMethods.SelectObject(GeneralhMemDC, RawScreenBitmap);

            NativeMethods.BitBlt(GeneralhMemDC, 0, 0, srcWidth, srcHeight, GeneralhDC, x, y, PatBltType.SrcCopy);

            NativeMethods.SelectObject(GeneralhMemDC, hOld);

            // Can also use the CursorX and Y to draw e.g. highlighting (e.g. a shrinking circle)

            BitmapSource screenshotImage = null;

            // int destWidth = RecorderParams.DestWidth;
            // int destHeight = RecorderParams.DestHeight;

            // float srcWidth = RecorderParams.SrcWidth;
            // float srcHeight = RecorderParams.SrcHeight;

            float drawWidth = destWidth;
            float drawHeight = destHeight;

            float offsetX = 0;
            float offsetY = 0;

            float scaleX = zoom; // 1.0F;
            float scaleY = zoom;    // 1.0F;

            //Debug.Print("Before....");
            //Debug.Print($"Src   : {srcWidth},{srcHeight}");
            //Debug.Print($"Dest  : {destWidth},{destHeight}");
            //Debug.Print($"Draw  : {drawWidth},{drawHeight}");
            //Debug.Print($"Offset: {offsetX},{offsetY}");
            //Debug.Print($"Scale : {scaleX},{scaleY}");

            switch (sizeHandling)
            {
                case SizeHandling.CropToFit:
                    // Crop to fit...
                    // E.g. Src Height is higher than the dest height we....
                    // (Src - Dest) / 2 as an offset

                    // Center horizontally
                    offsetX = (destWidth - srcWidth) * 0.5F;
                    offsetY = (destHeight - srcHeight) * 0.5F;

                    drawWidth = srcWidth;
                    drawHeight = srcHeight;

                    break;

                //case SizeHandling.SquashToFit:
                //    // Squash to fit...
                //    // Default! We don't do anything fancy, just let it do its thing
                //
                //    break;

                case SizeHandling.ScaleToFit:
                    // Scale to fit
                    // We scale things down to make them fit while retaining the correct ratio
                    // How much we have to scale by may be different in the X and Y, so we end up using whatever is greater
                    // E.g. Src width 200 is greater than Dest width of 100. 100/200 = 0.5 scale
                    float xScale = drawWidth / srcWidth;
                    float yScale = drawHeight / srcHeight;

                    // If we need to scale UP, then e.g. src width 200 into a dest width of 400 = 400/200 = 2.0 scale
                    // We go with the smallest scaling factor as the one we need

                    scaleX = (xScale < yScale ? xScale : yScale);
                    scaleY = scaleX;

                    // Update destination size

                    // If it's 1, then fab - exact match.

                    drawWidth = srcWidth * scaleX;
                    drawHeight = srcHeight * scaleY;

                    // Set offsets accordingly to centre
                    offsetX = (destWidth - drawWidth) * 0.5F;
                    offsetY = (destHeight - drawHeight) * 0.5F;

                    break;
            }

            //    Debug.Print("After....");
            //    Debug.Print($"Src   : {srcWidth},{srcHeight}");
            //    Debug.Print($"Dest  : {destWidth},{destHeight}");
            //    Debug.Print($"Draw  : {drawWidth},{drawHeight}");
            //    Debug.Print($"Offset: {offsetX},{offsetY}");
            //    Debug.Print($"Scale : {scaleX},{scaleY}\n");

            // ToDo: Retain aspect ratio
            // ToDo: Size to fit

            // Info from http://www.itgo.me/a/x1025659752280739323/how-to-capture-screen-to-be-video-using-c-sharp-net (didn't quite work, needed fixing, plus some bad GDI leaks!)

            IntPtr hIcon = IntPtr.Zero;
            Bitmap cursorBmp = null; // Icon.FromHandle(hIcon).ToBitmap();
            int cursorX = 0;
            int cursorY = 0;

            if (SettingsWrangler.GeneralSettings.Capture_IncludeCursor || SettingsWrangler.GeneralSettings.Capture_IncludeHighlight || zoomInstruction == ZoomInstruction.InitOffset)
            {
                CursorInfo cursorInfo = new CursorInfo() { cbSize = Marshal.SizeOf(typeof(CursorInfo)) };

                if (NativeMethods.GetCursorInfo(out cursorInfo))
                {
                    if (cursorInfo.flags == NativeMethods.CURSOR_SHOWING)
                    {
                        hIcon = NativeMethods.CopyIcon(cursorInfo.hCursor);

                        if (hIcon != IntPtr.Zero)
                        {
                            IconInfo iconInfo;

                            if (NativeMethods.GetIconInfo(hIcon, out iconInfo))
                            {
                                // Don't need the color bitmap
                                if (iconInfo.hbmColor != IntPtr.Zero)
                                    NativeMethods.DeleteObject(iconInfo.hbmColor);

                                // Don't need the mask!
                                if (iconInfo.hbmMask != IntPtr.Zero)
                                    NativeMethods.DeleteObject(iconInfo.hbmMask);

                                cursorX = cursorInfo.ptScreenPos.x - ((int)iconInfo.xHotspot) - x;
                                cursorY = cursorInfo.ptScreenPos.y - ((int)iconInfo.yHotspot) - y;

                                // Find the center point of our image
                                float centerX = destWidth / 2;      // e.g. @100
                                float centerY = destHeight / 2;

                                // get difference with cursor
                                float diffX = cursorX - centerX;    // e.g. @75 = -25 away
                                float diffY = cursorY - centerY;

                                if (zoomInstruction == ZoomInstruction.InitOffset)
                                {
                                    int tmpX = (cursorX < 0 ? 0 : cursorX);
                                    if (tmpX > srcWidth)
                                        tmpX = srcWidth;

                                    tmpX -= (srcWidth / 2);

                                    int tmpY = (cursorY < 0 ? 0 : cursorY);
                                    if (tmpY > srcHeight)
                                        tmpY = srcHeight;

                                    tmpY -= (srcHeight / 2);

                                    zoomCenterX = tmpX;
                                    zoomCenterY = tmpY;

                                    Globals.ZoomInstruction = ZoomInstruction.JustChillDude;
                                }

                                // and adjust for zoom level
                                float zoomedX = diffX * zoom;                      // e.g. 25 away * 2 = -50 away
                                float zoomedY = diffY * zoom;

                                cursorX = (int)(cursorX - diffX + zoomedX);
                                cursorY = (int)(cursorY - diffY + zoomedY);

                                //Type[] cargt = new[] { typeof(IntPtr), typeof(bool) };
                                //ConstructorInfo ci = typeof(Icon).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, cargt, null);
                                //object[] cargs = new[] { (object)bitmap.GetHicon(), true };
                                //Icon icon = (Icon)ci.Invoke(cargs);
                                //return icon;

                                // uiFlags: 0 - Count of GDI objects - pens, brushes, fonts, palettes, regions, device contexts, bitmaps, etc.
                                // uiFlags: 1 - Count of USER objects
                                // GDI objects: 
                                // USER objects: accelerator tables, cursors, icons, menus, windows, etc.
                                //     Debug.Print($"1: {NativeMethods.GetGuiResources(Process.GetCurrentProcess().Handle, 0)}");

                                // https://codingsight.com/gdi-leak-handling/

                                if (SettingsWrangler.GeneralSettings.Capture_IncludeCursor)
                                {
                                    // GDI LEAK
                                    // Creation of the cursorBmp is causing GDI Objects to be created, but NOT released
                                    // event if manually attempting to destroy the icon, and dispose of things
                                    // Possible windows bugs? Forum posts suggesting fixes to this problem don't appear to be working.

                                    // TEMPORARILY DISABLED
                                    //using (Icon icon = Icon.FromHandle(hIcon))
                                    //{
                                    //    cursorBmp = icon.ToBitmap();    // Icon.FromHandle(hIcon).ToBitmap();
                                    //                                    //        NativeMethods.DestroyIcon(icon.Handle);
                                    //    NativeMethods.DestroyIcon(icon.Handle);
                                    //}

                                    //// Extra attempts to test disposal
                                    ////     icon.Dispose();
                                    ////       cursorBmp.Dispose();
                                    ////       cursorBmp = null;
                                }

                                NativeMethods.DestroyIcon(hIcon);
                            }
                        }
                    }
                }
            }

            if (zoomInstruction == ZoomInstruction.ResetOffset)
            {
                zoomCenterX = 0;
                zoomCenterY = 0;
            }
            #endregion

            // If our image is squished to fit etc. then cursor zoom center point will be moved!

            // Globals.MainWindow.DebugInfoExtra = $"Cursor: {cursorX},{cursorY}";

            Stopwatch sw = new Stopwatch();
            sw.Start();

#if (DEBUG_SHOW_TIMINGS)
            StringBuilder sb = new StringBuilder();
            long last = 0;
            long latest = 0;
#endif

            if (BitmapFromScreen == null)
            {
                BitmapFromScreen = new Bitmap(destWidth, destHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                BitmapGraphics = Graphics.FromImage(BitmapFromScreen);
                GraphicsBuffer = BufferedGraphicsManager.Current.Allocate(BitmapGraphics, new Rectangle(0, 0, destWidth, destHeight));
            }
            else if (BitmapFromScreen.Width != destWidth || BitmapFromScreen.Height != destHeight)
            {
                GraphicsBuffer.Dispose();
                BitmapGraphics.Dispose();
                BitmapFromScreen.Dispose();
                BitmapFromScreen = new Bitmap(destWidth, destHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                BitmapGraphics = Graphics.FromImage(BitmapFromScreen);
                GraphicsBuffer = BufferedGraphicsManager.Current.Allocate(BitmapGraphics, new Rectangle(0, 0, destWidth, destHeight));
            }
            //       using (Bitmap bitmapFromScreen = new Bitmap(destWidth, destHeight))
            //       {
            //            using (var graphics = Graphics.FromImage(bitmapFromScreen))
            //           {
            // THINGS MAY NEED TO BE SCALED UP/DOWN etc. AT THIS POINT. Don't forget to offset as required!

            //      if (DateTime.Now.Second % 2 == 0)
            //      {
            //zoom = (DateTime.Now.Second % 5) + 1;

            //      BitmapGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; //Bilinear?

            offsetX -= ((drawWidth * zoom) - drawWidth) / 2;
            offsetX += (1.0F - zoom) * zoomCenterX;
            offsetY -= ((drawHeight * zoom) - drawHeight) / 2;
            offsetY += (1.0F - zoom) * zoomCenterY;

            drawWidth *= zoom;
            drawHeight *= zoom;
            //      }


            // Debug.Print($"offsets: {offsetX},{offsetY} cursor: {cursorX},{cursorY} scales: {scaleX},{scaleY} drawsize: {drawWidth},{drawHeight}");

            // last = sw.ElapsedMilliseconds;
            // Copy the raw data on screen into our Bitmap

            var g = GraphicsBuffer.Graphics;

            // If we aren't filling the area, we might want to blank any left over bits to remove previous gfx that might linger around
            if (offsetX > 0 || (offsetX + drawWidth) < destWidth)
            {
                // Left
                g.FillRectangle(System.Drawing.Brushes.Black, new System.Drawing.Rectangle(0, 0, (int)(offsetX), (int)destHeight));
                // right
                //g.FillRectangle(System.Drawing.Brushes.Black, new System.Drawing.Rectangle((int)(offsetX + drawWidth), 0, (int)offsetX, (int)destHeight));
            }

            if (offsetX + drawWidth < destWidth)
            {
                // Right
                // e.g. offsetX -200 + destWidth 500 = totalDrawSize 300
                int offsetTmp = (int)(offsetX + drawWidth);
                g.FillRectangle(System.Drawing.Brushes.Black, new System.Drawing.Rectangle((int)(offsetTmp), 0, (int)(destWidth - offsetTmp), (int)destHeight));
            }

            if (offsetY > 0)
            {
                // Top
                g.FillRectangle(System.Drawing.Brushes.Black, new System.Drawing.Rectangle((int)offsetX, 0, (int)drawWidth, (int)offsetY));

                // Bottom
                //g.FillRectangle(System.Drawing.Brushes.Black, new System.Drawing.Rectangle((int)offsetX, (int)(offsetY + drawHeight), (int)drawWidth, (int)offsetY));
            }

            if (offsetY + drawHeight < destHeight)
            {
                // Bottom
                int offsetTmp = (int)(offsetY + drawHeight);
                g.FillRectangle(System.Drawing.Brushes.Black, new System.Drawing.Rectangle((int)offsetX, (int)(offsetTmp), (int)drawWidth, (int)(destHeight - offsetTmp)));
            }

#if (DEBUG_SHOW_TIMINGS)
            latest = sw.ElapsedMilliseconds;
            sb.Append($"Rectangles: {latest - last} @ {latest}, ");
#endif

            //last = sw.ElapsedMilliseconds;

            if (SettingsWrangler.GeneralSettings.Capture_HighQualityZoom)
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;        // Bilinear = slower, Faster -> .HighQualityBilinear;
            else
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

            g.DrawImage(Image.FromHbitmap(RawScreenBitmap), offsetX, offsetY, drawWidth, drawHeight);

            // ToDo: right here we can look at scaling, moving around, etc. capture area.
            // ToDo: Right here we can add overlays/logos/etc. rendered on top.
            // ToDo: have a live area and a `writing to file` area preview?

            g.Flush();

            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

            //// THIS NEEDS OPTION FOR SIZING AND OFFSETTING AND SCALING TOO
            //if (hIcon != IntPtr.Zero)
            if (SettingsWrangler.GeneralSettings.Capture_IncludeCursor && cursorBmp != null)
            {
                g.DrawImage(cursorBmp, cursorX, cursorY, cursorBmp.Width, cursorBmp.Height);
                cursorBmp.Dispose();
            }

            g.Flush();

#if (DEBUG_SHOW_TIMINGS)
            latest = sw.ElapsedMilliseconds;
            sb.Append($"Capture: {latest - last} @ {latest}, ");
#endif

            if (SettingsWrangler.GeneralSettings.Capture_IncludeHighlight)
            {
                //last = sw.ElapsedMilliseconds;

                //float centerX = 100;
                //float centerY = 100;
                float radius = 25;
                float diameter = radius + radius;

                g.FillEllipse(Highlight_FillBrush, cursorX - radius, cursorY - radius, diameter, diameter);

                g.DrawEllipse(Highlight_Pen, cursorX - radius, cursorY - radius, diameter, diameter);

                g.Flush();

#if (DEBUG_SHOW_TIMINGS)
                latest = sw.ElapsedMilliseconds;
                sb.Append($"Highlight: {latest - last} @ {latest}, ");
#endif

                //Highlight_FillBrush.Dispose();
                //Highlight_Brush.Dispose();
                //Highlight_Pen.Dispose();
            }

            //last = sw.ElapsedMilliseconds;

            GraphicsBuffer.Render(BitmapGraphics);

#if (DEBUG_SHOW_TIMINGS)
            latest = sw.ElapsedMilliseconds;
            sb.Append($"GraphicsBufferRender: {latest - last} @ {latest}, ");
#endif

            Rectangle rectangle = new Rectangle(0, 0, destWidth, destHeight);

            // Buffer data for video encoder - rgb data
            if (buffer != null)
            {
                //last = sw.ElapsedMilliseconds;

                // Copies bitmap data into raw buffer
                var videoBitmapData = BitmapFromScreen.LockBits(rectangle, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                Marshal.Copy(videoBitmapData.Scan0, buffer, 0, buffer.Length);
                BitmapFromScreen.UnlockBits(videoBitmapData);

#if (DEBUG_SHOW_TIMINGS)
                latest = sw.ElapsedMilliseconds;
                sb.Append($"BufferCopy: {latest - last} @ {latest}, ");
#endif

                if (checkIfContentChanged && contentChanged == false && ContentChange_PreviousFrameBuffer != null)
                // ToDo: Check performance of this
                // Looks like there are faster ways of doing this...
                // Can we atleast cast byte[] to int[] or longint[] and do 64bit rather than 8bit at a time comparisons?

                // Change to screen content is more likely to be around the centerish of the screen, so top left to bottom right comparion is probably not the optimal way to do this.
                // ToDo: Different checking pattern of what we are checking!

                // Some variants given below, along with a bit of performance info
                {
                    // ReadOnlySpan<> made no difference
                    //Debug.Print($"Buffer length: {buffer.Length}");
                    //Stopwatch sw = Stopwatch.StartNew();

                    // Fastest
                    Span<byte> span1 = MemoryMarshal.Cast<byte, byte>(buffer);
                    Span<byte> span2 = MemoryMarshal.Cast<byte, byte>(ContentChange_PreviousFrameBuffer);
                    contentChanged = !span1.SequenceEqual(span2);

                    //sw.Restart();

                    // 2nd fastest
                    //Span<int> span1 = MemoryMarshal.Cast<byte, int>(buffer);
                    //Span<int> span2 = MemoryMarshal.Cast<byte, int>(ContentChange_PreviousFrameBuffer);
                    //contentChanged = !span1.SequenceEqual(span2);

                    //Debug.Print($"Span time int: {sw.Elapsed}");
                    //sw.Restart();

                    // 3rd fastest
                    //Span<long> span164 = MemoryMarshal.Cast<byte, long>(buffer);
                    //Span<long> span264 = MemoryMarshal.Cast<byte, long>(ContentChange_PreviousFrameBuffer);
                    //contentChanged = !span164.SequenceEqual(span264);

                    //Debug.Print($"Span time long: {sw.Elapsed}");
                    //sw.Restart();

                    // 4th fastest
                    //Parallel.ForEach(buffer,
                    //    (i, s, x) =>
                    //    {
                    //        if (buffer[x] != ContentChange_PreviousFrameBuffer[x])
                    //        {
                    //            contentChanged = true;
                    //            s.Stop();
                    //        }
                    //    });

                    //Debug.Print($"For time Parallel: {sw.Elapsed}");
                    //sw.Restart();

                    // 5th fastest - or should we say slowest :)
                    //for (int i = 0; i < buffer.Length; i++)
                    //{
                    //    if (buffer[i] != ContentChange_PreviousFrameBuffer[i])
                    //    {
                    //        contentChanged = true;
                    //        break;
                    //    }
                    //}

                    //Debug.Print($"Basic for time: {sw.Elapsed}\n");


                }
                else
                {
                    contentChanged = true;
                }
            }

            // Save to file, if wanted!
            // Screenshot should be same size as video - NOT resized - incase it is required in video editing!
            // ToDo: Run this on a different thread so it doesn't block the video/cause possible jerks/etc.
            if (filename != null)
            {
                try
                {
                    //last = sw.ElapsedMilliseconds;
                    BitmapFromScreen.Save(filename, ImageFormat.Png);

#if (DEBUG_SHOW_TIMINGS)
                    latest = sw.ElapsedMilliseconds;
                    sb.Append($"Save: {latest - last} @ {latest}, ");
#endif
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error saving screenshot: {ex.Message}");
                }
            }

            // Screenshot data - slightly different, argb rather than rgb
            // ToDo: Scale here to destination size?

            // ToDo: No need to return a screenshot if no content changed and we are just filling buffers for videos. Performance savings!
            if (returnScreenshot)
            {
                //last = sw.ElapsedMilliseconds;
                var screenBitmapData = BitmapFromScreen.LockBits(rectangle, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                int size = (rectangle.Width * rectangle.Height) * 4;

// ToDo: CAN WE OVERWRITE DATA HERE RATHER THAN MAKING NEW ONE?
           
                screenshotImage = BitmapSource.Create(BitmapFromScreen.Width, BitmapFromScreen.Height, BitmapFromScreen.HorizontalResolution, BitmapFromScreen.VerticalResolution, PixelFormats.Bgra32, null, screenBitmapData.Scan0, size, screenBitmapData.Stride);
                BitmapFromScreen.UnlockBits(screenBitmapData);
                screenshotImage.Freeze(); // Make friendly across threads. We won't be editing it any more.
#if (DEBUG_SHOW_TIMINGS)
                latest = sw.ElapsedMilliseconds;
                sb.Append($"BitmapFromScreen: {latest - last} @ {latest}, ");
#endif
            }
            // }
            //        }

            //     NativeMethods.DeleteObject(RawScreenBitmap);
            NativeMethods.DeleteDC(GeneralhMemDC);
            NativeMethods.ReleaseDC(GeneralHWnd, GeneralhDC);
            //Cleanup();

            ContentChange_PreviousFrameBuffer = buffer;
            Globals.ContentChanged = contentChanged || !checkIfContentChanged;       // Always true if we aren't checking!

#if (DEBUG_SHOW_TIMINGS)
            latest = sw.ElapsedMilliseconds;
            sb.Append($"Total: {latest}");
#endif

            Globals.Performance_LastScreenCaptureRate = sw.ElapsedMilliseconds; ;

#if (DEBUG_SHOW_TIMINGS)
            Debug.Print(sb.ToString());
#endif

            return screenshotImage;
        }

        private static void Cleanup()
        {
            if (RawScreenBitmap != IntPtr.Zero)
                NativeMethods.DeleteObject(RawScreenBitmap);

            //    NativeMethods.DeleteDC(GeneralhMemDC);
            //    NativeMethods.ReleaseDC(GeneralHWnd, GeneralhDC);
        }
    }
}
