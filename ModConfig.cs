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

        public bool DisplayIslandWeather { get; set; } = true;

        public bool DisplayWeather { get; set; } = true;

        public bool DisplayGift { get; set; }

        public bool DisplayDish { get; set; }

        public int HorizontalOffset { get; set; }

        public int VerticalOffset { get; set; }

        public double TargetLuck { get; set; } = -1.0;

        public string TargetIslandWeather { get; set; } = "";

        public string TargetWeather { get; set; } = "";

        public string TargetGifter { get; set; } = "";

        public string TargetDish { get; set; } = "";

        public int TargetDishAmount { get; set; }

        public int TargetStepsLimit { get; set; } = 1000;

        public SButton ToggleHud { get; set; } = SButton.OemTilde;
        
    }
}
