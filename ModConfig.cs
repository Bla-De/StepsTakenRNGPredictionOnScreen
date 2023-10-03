using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace StepsTakenOnScreen
{
    class ModConfig
    {
        public bool DisplaySteps { get; set; } = true;
        public bool DisplayLuck { get; set; } = true;
        public bool DisplayWeather { get; set; } = true;
        public bool DisplayGift { get; set; }
        public bool DisplayDish { get; set; }
        public bool DrawHud { get; set; } = true;
        public SButton ToggleHud { get; set; } = SButton.OemTilde;
        
        public int HorizontalOffset { get; set; }
        public int VerticalOffset { get; set; }
    }
}
