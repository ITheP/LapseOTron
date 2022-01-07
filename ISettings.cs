using System;
using System.Collections.Generic;
using System.Text;

namespace LapseOTron.Settings
{
    public interface ISettings
    {
        public bool SettingsChanged { get; set; }
        public string SettingsFilename { get; set; }
    }
}
