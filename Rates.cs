using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LapseOTron
{
    public class Rate
    {
        public string Description { get; set; }
        public string Example1 { get; set; }
        public string Example2 { get; set; }
        public string Example3 { get; set; }

        public int Speed { get; set; }

        public int Multiplier
        {
            get { return -1; }
            set
            {
                double val = value;
                // e.g. 600 seconds / 20x = 30 seconds
                TimeSpan ts = new TimeSpan((long)(600.0d / val * TimeSpan.TicksPerSecond));
                Example1 = $"{ts.Minutes}:{ts.Seconds.ToString("00")}.{(ts.Milliseconds / 100)}";
                ts = new TimeSpan((long)(60.0d / val * TimeSpan.TicksPerSecond));
                if (ts.TotalMilliseconds > 100)
                    Example2 = $"{ts.Seconds}.{(ts.Milliseconds / 100)}";

                ts = new TimeSpan((long)(10.0d / val * TimeSpan.TicksPerSecond));
                if (ts.TotalMilliseconds > 100)
                    Example3 = $"{ts.Seconds}.{(ts.Milliseconds / 100)}";

                Speed = value;
            }
        }
    }

    public static class Rates
    {
        public static List<Rate> RateList = new List<Rate>
            (new[] {
                new Rate()
                {
                    Example1 = "10 min",
                    Example2 = "1 min",
                    Example3 = "10 sec"
                },
                new Rate()
                {
                    Example1 = "   ",
                    Example2 = "  ",
                    Example3 = "  "
                },
                new Rate() {
                    Description = "Real time",
                    Multiplier = 1,
                    Example2 = "60"
                },
                new Rate() {
                    Description = "2x",
                    Multiplier = 2
                },
                new Rate() {
                    Description = "3x",
                    Multiplier = 3
                },
                new Rate() {
                    Description = "4x",
                    Multiplier = 4
                },
                new Rate() {
                    Description = "5x",
                    Multiplier = 5
                },
                new Rate() {
                    Description = "6x",
                    Multiplier = 6
                },
                new Rate() {
                    Description = "7x",
                    Multiplier = 7
                },
                new Rate() {
                    Description = "8x",
                    Multiplier = 8
                },
                new Rate() {
                    Description = "9x",
                    Multiplier = 9
                },
                new Rate() {
                    Description = "10x",
                    Multiplier = 10
                },
                new Rate() {
                    Description = "15x",
                    Multiplier = 15
                },
                new Rate() {
                    Description = "20x",
                    Multiplier = 20
                },
                new Rate() {
                    Description = "25x",
                    Multiplier = 25
                },
                new Rate() {
                    Description = "30x",
                    Multiplier = 30
                },
                new Rate() {
                    Description = "40x",
                    Multiplier = 40
                },
                new Rate() {
                    Description = "50x",
                    Multiplier = 50
                },
                new Rate() {
                    Description = "60x",
                    Multiplier = 60
                },
                new Rate() {
                    Description = "100x",
                    Multiplier = 100
                },
                new Rate() {
                    Description = "120x",
                    Multiplier = 120
                },
                new Rate() {
                    Description = "200x",
                    Multiplier = 200
                },
                new Rate() {
                    Description = "300x",
                    Multiplier = 300
                },
                new Rate() {
                    Description = "400x",
                    Multiplier = 400
                },
                new Rate() {
                    Description = "500x",
                    Multiplier = 500
                },
                new Rate() {
                    Description = "600x",
                    Multiplier = 600
                },
                new Rate() {
                    Description = "750x",
                    Multiplier = 750
                },
                new Rate() {
                    Description = "1000x",
                    Multiplier = 1000
                },
            });
    }
}