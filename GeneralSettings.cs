using System;
using System.Collections.ObjectModel;
using LapseOTron.Attributes;
using System.Runtime.Serialization;
using System.Reflection;
using System.Linq;
using LapseOTron.Settings;

namespace LapseOTron.Settings
{
    [System.Serializable]
    [ExternalFilename("LapseOTron.config")]
    public class GeneralSettings : ISettings
    {
        [IgnoreDataMember]
        public bool SettingsChanged { get; set; } = false;

        private string _SettingsFilename { get; set; }
        [IgnoreDataMember]
        public string SettingsFilename
        {
            get
            {
                if (string.IsNullOrEmpty(_SettingsFilename))
                    _SettingsFilename = typeof(GeneralSettings).GetCustomAttributes<ExternalFilename>().First().Filename;

                return _SettingsFilename;
            }
            set { _SettingsFilename = value; }
        }

        [IgnoreDataMember]
        public bool Debug_LogToFile { get; set; } = false;

        public string Rate { get; set; } = "10x";
        public string RateChangePeriod { get; set; } = "30 secs";

        public string Zoom { get; set; } = "5x";
        public string ZoomChangePeriod { get; set; } = "2 secs";

        public bool Capture_IncludeCursor { get; set; } = true;
        public bool Capture_IncludeHighlight { get; set; } = false;
        public SizeHandling Capture_SizeHandling { get; set; } = SizeHandling.CropToFit;
        public string Capture_Size { get; set; } = "1920x1080";
        public bool Capture_OnlyOnContentChange { get; set; } = false;
        public bool Capture_ZoomInToCursorPosition { get; set; } = false;
        public bool Capture_HighQualityZoom { get; set; } = false;
    }
}
