using LapseOTron.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LapseOTron
{
    public class CaptureSize
    {
        public string Description { get; set; }
        public BitmapImage Example { get; set; }
        public SizeHandling Type { get; set; }

    }

    public static class CaptureSizes
    {
        public static ObservableCollection<CaptureSize> CaptureSizeList = new ObservableCollection<CaptureSize>
            (new[] {
                new CaptureSize()
                {
                    Description = "Crop to fit",
                    Example = new BitmapImage(new Uri("./Images/Capture_SizeHandling_CropToFit.png", UriKind.RelativeOrAbsolute)),   // Resources.Capture_SizeHandling_CropToFit,
                    Type = SizeHandling.CropToFit
                },
                new CaptureSize()
                {
                    Description = "Squash to fit",
                    Example = new BitmapImage(new Uri("./Images/Capture_SizeHandling_SquashToFit.png", UriKind.RelativeOrAbsolute)),   //Resources.Capture_SizeHandling_SquashToFit,
                    Type = SizeHandling.SquashToFit
                },
                new CaptureSize() {
                    Description = "Scale to fit",
                    Example = new BitmapImage(new Uri("./Images/Capture_SizeHandling_ScaleToFit.png", UriKind.RelativeOrAbsolute)),   // Resources.Capture_SizeHandling_ScaleToFit,
                    Type = SizeHandling.ScaleToFit
                }
            });
    }
}