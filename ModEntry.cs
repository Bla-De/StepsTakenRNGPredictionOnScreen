using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace StepsTakenOnScreen
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;
        private double dailyLuck;
        private int weatherForTomorrow;
        private int dishOfTheDay;
        private int dishOfTheDayAmount;
        private string mailPerson;

        private int lastStepsTakenCalculation = -1;
        private int daysPlayedCalculation = -1;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;

            this.Config = this.Helper.ReadConfig<ModConfig>();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // Refresh Config
            if (e.Button == SButton.F5)
            {
                this.Config = this.Helper.ReadConfig<ModConfig>();
                this.Monitor.Log("Config reloaded", LogLevel.Info);
            }
        }
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // get location info

            SpriteBatch spriteBatch = Game1.spriteBatch;

            // Calculate predictions if required
            if (Config.DisplayDish || Config.DisplayGift || Config.DisplayLuck || Config.DisplayWeather)
            {
                CalculatePredictions();
            }

            string str = "";

            if (Config.DisplaySteps)
            {
                str = InsertLine(str, GetStepsTaken());
            }
           
            if (Config.DisplayLuck)
            {
                str = InsertLine(str, GetLuck());
            }

            if (Config.DisplayWeather)
            {
                str = InsertLine(str, GetWeather());
            }

            if (Config.DisplayDish)
            {
                str = InsertLine(str, GetDishOfTheDay());
            }

            if (Config.DisplayGift)
            {
                str = InsertLine(str, GetMailPerson());
            }

            if (str != "")
                DrawHelper.DrawHoverBox(spriteBatch, str, new Vector2(Config.HorizontalOffset,Config.VerticalOffset), Game1.viewport.Width);

        }

        private string GetStepsTaken()
        {
            return "StepsTaken: " + Game1.stats.stepsTaken.ToString();
        }

        private string GetLuck()
        {
            return "Luck Tomorrow: " + dailyLuck.ToString();
        }
        private string GetWeather()
        {
            string weather = "";
            switch (weatherForTomorrow)
            {
                case 0:
                    weather = "Sunny";
                    break;
                case 1:
                    weather = "Rain";
                    break;
                case 2:
                    weather = "Debris";
                    break;
                case 3:
                    weather = "Lightning";
                    break;
                case 4:
                    weather = "Festival";
                    break;
                case 5:
                    weather = "Snow";
                    break;
                case 6:
                    weather = "Wedding";
                    break;
            }
            return "Weather Aftermorrow: " + weather;
        }
        private string GetDishOfTheDay()
        {
            return "Dish of the Day: " + Game1.objectInformation[dishOfTheDay].Split('/')[4] + " (" + dishOfTheDayAmount.ToString() + ")";
        }

        private string GetMailPerson()
        {
            return "Mail Gift: " + mailPerson;
        }

        private void CalculatePredictions()
        {
            // Only do calculations if necessary
            if (lastStepsTakenCalculation == Game1.stats.StepsTaken && daysPlayedCalculation == Game1.stats.DaysPlayed)
                return;

            lastStepsTakenCalculation = (int)Game1.stats.StepsTaken;
            daysPlayedCalculation = (int)Game1.stats.DaysPlayed;

            // Simulate new day logic
            Random random = new Random((int)Game1.uniqueIDForThisGame / 100 + (int)Game1.stats.DaysPlayed * 10 + 1 + (int)Game1.stats.StepsTaken);

            // Day of month not yet incremented
            for (int index = 0; index < Game1.dayOfMonth; ++index)
                random.Next();
            dishOfTheDay = random.Next(194, 240);
            while ((Utility.getForbiddenDishesOfTheDay()).Contains<int>(dishOfTheDay))
                dishOfTheDay = random.Next(194, 240);
            dishOfTheDayAmount = random.Next(1, 4 + (random.NextDouble() < 0.08 ? 10 : 0));
            // Object constructor
            random.NextDouble();
            //Rarecrow Society
            random.NextDouble();

            mailPerson = "";
            if (Game1.player.friendshipData.Count() > 0)
            {
                string key = Game1.player.friendshipData.Keys.ElementAt<string>(random.Next(Game1.player.friendshipData.Keys.Count<string>()));
                if (random.NextDouble() < (double)(Game1.player.friendshipData[key].Points / 250) * 0.1 && (Game1.player.spouse == null || !Game1.player.spouse.Equals(key)) && Game1.content.Load<Dictionary<string, string>>("Data\\mail").ContainsKey(key))
                    mailPerson = key;
            }

            dailyLuck = random.Next(-100, 101) / 1000.0;

            // Debris weather has extra random uses
            if (Game1.weatherForTomorrow == 2)
            {
                int num = random.Next(16, 64);
                for (int index = 0; index < num; ++index)
                {
                    random.NextDouble();
                    random.NextDouble();
                    random.NextDouble();
                    random.NextDouble();
                    random.NextDouble();
                    random.NextDouble();
                }
            }

            // Need incremented date for next calculations
            int season = 1;
            switch (Game1.currentSeason)
            {
                case "spring":
                    season = 1;
                    break;
                case "summer":
                    season = 2;
                    break;
                case "fall":
                    season = 3;
                    break;
                case "winter":
                    season = 4;
                    break;
            }

            int dayOfMonth = Game1.dayOfMonth + 1;
            if (dayOfMonth == 29)
            {
                dayOfMonth = 1;
                season++;
                if (season == 5)
                    season = 1;
            }

            double chanceToRainTomorrow = season != 2 ? (season != 4 ? 0.183 : 0.63) : (dayOfMonth > 1 ? 0.12 + (double)dayOfMonth * (3.0 / 1000.0) : 0.0);
            if (random.NextDouble() < chanceToRainTomorrow)
            {
                weatherForTomorrow = 1;
                if (season == 2 && random.NextDouble() < 0.85 || season != 4 && random.NextDouble() < 0.25 && (dayOfMonth > 2 && Game1.stats.DaysPlayed > 26U))
                    weatherForTomorrow = 3;
                if (season == 4)
                    weatherForTomorrow = 5;
            }
            else
                weatherForTomorrow = Game1.stats.DaysPlayed <= 1U || (season != 1 || random.NextDouble() >= 0.2) && (season != 3 || random.NextDouble() >= 0.6) ? 0 : 2;
            if (Utility.isFestivalDay(dayOfMonth + 1, Game1.currentSeason))
                weatherForTomorrow = 4;
            if (Game1.stats.DaysPlayed == 1U)
                weatherForTomorrow = 1;

            // Simulate checks for weather at the start of the next day (literals adjusted from game code)
            if (Game1.stats.DaysPlayed == 2U)
                weatherForTomorrow = 0;
            if (season == 2 && dayOfMonth % 13 == 12)
                weatherForTomorrow = 3;

            if (dayOfMonth == 28)
                weatherForTomorrow = 0;

        }

        private string InsertLine(string str, string newStr)
        {
            if (str == "")
            {
                return newStr;
            }

            return str + "\r\n" + newStr;
        }
    }
}
