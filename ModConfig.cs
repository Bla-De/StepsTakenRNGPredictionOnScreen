using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepsTakenOnScreen
{
    class ModConfig
    {
        public bool DisplaySteps { get; set; } = true;
        public bool DisplayLuck { get; set; } = true;
        public bool DisplayWeather { get; set; } = true;
        public bool DisplayGift { get; set; }
        public bool DisplayDish { get; set; }

        public int HorizontalOffset { get; set; }
        public int VerticalOffset { get; set; }
    }
}
