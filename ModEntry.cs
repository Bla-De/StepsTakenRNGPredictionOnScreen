using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GenericModConfigMenu;
using xTile.Dimensions;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace StepsTakenOnScreen
{
  public class ModEntry : Mod
  {
    private ModConfig Config;
    private double dailyLuck;
    private int islandWeatherForTomorrow;
    private int weatherForTomorrow;
    private int dishOfTheDay;
    private int dishOfTheDayAmount;
    private string mailPerson;
    private int lastStepsTakenCalculation = -1;
    private int daysPlayedCalculation = -1;
    private int targetStepsCalculation = -1;
    private int targetDay = -1;
    private string labelStepsTaken;
    private string labelDailyLuck;
    private string labelIslandWeather;
    private string labelWeather;
    private string labelDish;
    private string labelGift;
    private string labelSearch;
    private string[] islandWeatherValues;
    private string[] weatherValues;
    private string[] dishValues;
    private string[] giftValues;
    private bool locationsChecked;
    private int extraCalls;
    private bool targetFound;
    private bool drawHud = true;
    private List<FlagSet> predictedWeather;
    private double predictedLuck;

    public override void Entry(IModHelper helper)
    {
      helper.Events.GameLoop.DayEnding += OnDayEnding;
      helper.Events.GameLoop.GameLaunched += OnGameLaunched;
      helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
      helper.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(this.OnButtonPressed);
      helper.Events.Input.ButtonReleased += new EventHandler<ButtonReleasedEventArgs>(this.OnButtonReleased);
      helper.Events.Display.RenderedHud += new EventHandler<RenderedHudEventArgs>(this.OnRenderedHud);
      helper.Events.GameLoop.DayStarted += new EventHandler<DayStartedEventArgs>(this.OnDayStarted);
      this.Config = this.Helper.ReadConfig<ModConfig>();
      this.labelStepsTaken = (this.Helper.Translation.Get("DisplaySteps"));
      this.labelDailyLuck = (this.Helper.Translation.Get("DisplayLuck"));
      this.labelIslandWeather = (this.Helper.Translation.Get("DisplayIslandWeather"));
      this.labelWeather = (this.Helper.Translation.Get("DisplayWeather"));
      this.labelGift = (this.Helper.Translation.Get("DisplayGift"));
      this.labelDish = (this.Helper.Translation.Get("DisplayDish"));
      this.labelSearch = (this.Helper.Translation.Get("DisplaySearch"));
      this.islandWeatherValues = this.Config.TargetWeather.Split(',');
      this.weatherValues = this.Config.TargetWeather.Split(',');
      this.dishValues = this.Config.TargetDish.Split(',');
      this.giftValues = this.Config.TargetGifter.Split(',');
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
      predictedWeather = new List<FlagSet>(); //re-initialize empty list when save loads, to prevent data leaking between saves and list bloating.
    }

    private void OnDayEnding(object sender, DayEndingEventArgs e) //grabs the predicted weather, the day it is predicted for, and the season. Stores it in a FlagSet for use on DayStart to make sure weather is set to match prediction.
    {
      Dictionary<int, string> intToSeason = new Dictionary<int, string>()
      {
        {1,"spring"},
        {2,"summer"},
        {3,"fall"},
        {4,"winter"}
      };
      Dictionary<string, int> seasonToInt = new Dictionary<string, int>()
      {
        { "spring", 1 },
        { "summer", 2 },
        { "fall", 3 },
        { "winter", 4 }
      };
      int currentDay = Game1.dayOfMonth;
      int targetDay;
      int currentSeason = seasonToInt[Game1.currentSeason];
      string targetSeason;
      if (currentDay + 1 > 28)
      {
        targetDay = currentDay + 1 - 28;
        if (currentSeason == 4)
        {
          targetSeason = intToSeason[1];
        }
        else
        {
          targetSeason = intToSeason[currentSeason + 1];
        }
      }
      else
      {
        targetDay = currentDay + 1;
        targetSeason = intToSeason[currentSeason];
      }
      FlagSet flagSet = new FlagSet(targetDay, targetSeason, weatherForTomorrow);
      predictedWeather.Add(flagSet);
      predictedLuck = dailyLuck;
    }
    public void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
      var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
      if (configMenu is null)
      {
        this.Monitor.Log("Not Registering Mod with API", (LogLevel) 2);
        return;
      }
      this.Monitor.Log("Registering Mod with API", (LogLevel) 2);
      configMenu.Register(
        mod: this.ModManifest,
        reset: () => this.Config = new ModConfig(),
        save: () =>
        {
          this.Helper.WriteConfig((this.Config));
          ReloadConfig();
        });

      configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => "Display Steps",
        tooltip: () => "Display current step count",
        getValue: () => this.Config.DisplaySteps,
        setValue: value => this.Config.DisplaySteps = value);
      configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => "Display Luck",
        tooltip: () => "Display predicted luck value",
        getValue: () => this.Config.DisplayLuck,
        setValue: value => this.Config.DisplayLuck = value);
      configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => "Display Island Weather",
        getValue: () => this.Config.DisplayIslandWeather,
        setValue: value => this.Config.DisplayIslandWeather = value);
      configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => "Display Weather",
        getValue: () => this.Config.DisplayWeather,
        setValue: value => this.Config.DisplayWeather = value);
      configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => "Display Gift",
        tooltip: () => "Display NPC sending gift in mail",
        getValue: () => this.Config.DisplayGift,
        setValue: value => this.Config.DisplayGift = value);
      configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => "Display Dish",
        tooltip: () => "Display the Dish of the Day",
        getValue: () => this.Config.DisplayDish,
        setValue: value => this.Config.DisplayDish = value);
      configMenu.AddNumberOption(
        mod:this.ModManifest,
        getValue: () => this.Config.HorizontalOffset,
        setValue: value => this.Config.HorizontalOffset = value,
        name: () => "Horizontal Offset",
        tooltip: () => "Sets horizontal position of UI, Integers only");
      configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => "Vertical Offset",
        tooltip: () => "Sets vertical position of UI, Integers only",
        getValue: () => this.Config.VerticalOffset,
        setValue: value => this.Config.VerticalOffset = value);
      configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => "Target Luck",
        tooltip: () => "Set target luck value. Values range from -0.1 to +0.1",
        getValue: () => (float)this.Config.TargetLuck,
        setValue: value => this.Config.TargetLuck = Math.Round(value,2));
      configMenu.AddTextOption(
        mod: this.ModManifest,
        name: () => "Target Island Weather",
        tooltip: () => "See documentation for valid weather type strings",
        getValue: () => this.Config.TargetIslandWeather,
        setValue: value => this.Config.TargetIslandWeather = value);
      configMenu.AddTextOption(
        mod: this.ModManifest,
        name: () => "Target Weather",
        tooltip: () => "See documentation for valid weather type strings",
        getValue: () => this.Config.TargetWeather,
        setValue: value => this.Config.TargetWeather = value);
      configMenu.AddTextOption(
        mod: this.ModManifest,
        name: () => "Target Gifter",
        tooltip: () => "Set name of desired gift sending NPC",
        getValue: () => this.Config.TargetGifter,
        setValue: value => this.Config.TargetGifter = value);
      configMenu.AddTextOption(
        mod: this.ModManifest,
        name: () => "Target Dish",
        tooltip: () => "Name of desired 'Dish of the Day'",
        getValue: () => this.Config.TargetDish,
        setValue: value => this.Config.TargetDish = value);
      configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => "Target Dish Amount",
        tooltip: () => "Desired number of 'Dish of the Day' available for sale",
        getValue: () => this.Config.TargetDishAmount,
        setValue: value => this.Config.TargetDishAmount = value);
      configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => "Target Steps Limit",
        tooltip: () =>
          "How many more steps the prediction will search through for your desired target combination, Integers only",
        getValue: () => this.Config.TargetStepsLimit,
        setValue: value => this.Config.TargetStepsLimit = value);
      configMenu.AddKeybind(
        mod: this.ModManifest,
        name: () => "Toggle UI",
        tooltip: () => "Set the key used to toggle the prediction UI on/off",
        getValue: () => this.Config.ToggleHud,
        setValue: value => this.Config.ToggleHud = value);
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
      //Monitor.Log($"predictedWeather Length: {predictedWeather.Count}",(LogLevel)2);
      this.locationsChecked = false;
      //this code will check if the 0th index of weatherPredictions is for the current Day. If it is, it will set the weather and the luck, then remove the 0th item from weatherPredictions
      if (predictedWeather.Count == 0)
      {
        return;
      }
      var currentSeason = Game1.currentSeason;
      var currentDay = Game1.dayOfMonth;
      if (predictedWeather[0].GetSeason() == currentSeason)
      {
        //Monitor.Log($"Season: {predictedWeather[0].GetSeason()}",(LogLevel)2);
        if (predictedWeather[0].GetDay() == currentDay)
        {
          //Monitor.Log($"Day: {predictedWeather[0].GetDay()} Weather: {predictedWeather[0].GetWeather()}",(LogLevel)2);
          Game1.weatherForTomorrow = predictedWeather[0].GetWeather();
          predictedWeather.RemoveAt(0);
        }
      }
      Game1.player.team.sharedDailyLuck.Value = predictedLuck; //set luck every day, since it is always predicted for very next day, hopefully works?
    }

    private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
    {
      
      if (!Context.IsWorldReady) 
      {
        return;
      }
      if (e.Button == Config.ToggleHud)
      {
        drawHud = !drawHud;
      }
      if (e.Button == SButton.F5)
      {
        ReloadConfig();
      }
    }

    private void ReloadConfig()
    {
      this.Config = this.Helper.ReadConfig<ModConfig>();
      this.Monitor.Log("Config reloaded", (LogLevel) 2);
      this.islandWeatherValues = this.Config.TargetWeather.Split(',');
      this.weatherValues = this.Config.TargetWeather.Split(',');
      this.dishValues = this.Config.TargetDish.Split(',');
      this.giftValues = this.Config.TargetGifter.Split(',');
      this.targetStepsCalculation = 0;
    }
    private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
    {
      if (!Context.IsWorldReady || !SButtonExtensions.IsActionButton(e.Button) && !SButtonExtensions.IsUseToolButton(e.Button))
      {return;}
      Vector2 grabTile = e.Cursor.GrabTile;
      Object objectAtTile = Game1.currentLocation.getObjectAtTile((int) grabTile.X, (int) grabTile.Y);
      if (objectAtTile == null || objectAtTile.minutesUntilReady <= 0)
      {return;}
      this.locationsChecked = false;
    }

    private void OnRenderedHud(object sender, RenderedHudEventArgs e)
    {
      SpriteBatch spriteBatch1 = Game1.spriteBatch;
      bool flag = false;
      if (!this.locationsChecked)
      {
        int extraCalls = this.extraCalls;
        this.CheckLocations();
        flag = this.extraCalls != extraCalls;
      }
      if ((this.Config.DisplayDish || this.Config.DisplayGift || this.Config.DisplayLuck || this.Config.DisplayWeather) && (flag || (long) this.lastStepsTakenCalculation != (long) Game1.stats.StepsTaken || (long) this.daysPlayedCalculation != (long) Game1.stats.DaysPlayed))
      {
        this.lastStepsTakenCalculation = (int) Game1.stats.StepsTaken;
        this.daysPlayedCalculation = (int) Game1.stats.DaysPlayed;
        this.CalculatePredictions(this.lastStepsTakenCalculation, out this.dailyLuck, out this.islandWeatherForTomorrow, out this.weatherForTomorrow, out this.dishOfTheDay, out this.dishOfTheDayAmount, out this.mailPerson);
      }
      string str1 = "";
      if (this.Config.DisplaySteps)
        str1 = this.InsertLine(str1, this.GetStepsTaken());
      if (this.Config.DisplayLuck)
        str1 = this.InsertLine(str1, this.GetLuck());
      if (this.Config.DisplayIslandWeather)
        str1 = this.InsertLine(str1, this.GetIslandWeather(this.islandWeatherForTomorrow));
      if (this.Config.DisplayWeather)
        str1 = this.InsertLine(str1, this.GetWeather(this.weatherForTomorrow));
      if (this.Config.DisplayDish)
        str1 = this.InsertLine(str1, this.GetDishOfTheDay());
      if (this.Config.DisplayGift)
        str1 = this.InsertLine(str1, this.GetMailPerson());
      if (this.Config.TargetLuck != -1.0 || this.Config.TargetWeather != "" || this.Config.TargetGifter != "" || this.Config.TargetDish != "")
      {
        if (flag || (long) Game1.stats.StepsTaken > (long) this.targetStepsCalculation || (long) Game1.stats.daysPlayed != (long) this.targetDay)
        {
          this.targetFound = false;
          this.targetDay = (int) Game1.stats.daysPlayed;
          for (int index = 0; index < this.Config.TargetStepsLimit; ++index)
          {
            this.targetStepsCalculation = index + (int) Game1.stats.StepsTaken;
            double dailyLuck;
            int islandWeather;
            int weather;
            int dishOfTheDay;
            int dishOfTheDayAmount;
            string mailPerson;
            this.CalculatePredictions(this.targetStepsCalculation, out dailyLuck, out islandWeather, out weather, out dishOfTheDay, out dishOfTheDayAmount, out mailPerson);
            if ((this.Config.TargetLuck == -1.0 || dailyLuck >= this.Config.TargetLuck) && (this.Config.TargetIslandWeather == "" || ((IEnumerable<string>) this.islandWeatherValues).Contains<string>(this.GetWeatherValue(islandWeather))) && (this.Config.TargetWeather == "" || ((IEnumerable<string>) this.weatherValues).Contains<string>(this.GetWeatherValue(weather))) && (this.Config.TargetDish == "" || ((IEnumerable<string>) this.dishValues).Contains<string>(this.GetDishOfTheDayValue(dishOfTheDay)) && dishOfTheDayAmount >= this.Config.TargetDishAmount) && (this.Config.TargetGifter == "" || ((IEnumerable<string>) this.giftValues).Contains<string>(mailPerson)))
            {
              this.targetFound = true;
              break;
            }
          }
        }
        string str2 = this.InsertLine(str1, "");
        str1 = this.InsertLine(!this.targetFound ? this.InsertLine(str2, "Criteria not met after searching to step count: " + this.targetStepsCalculation.ToString()) : this.InsertLine(str2, "Steps required to hit target: " + this.targetStepsCalculation.ToString()), "Criteria:");
        if (this.Config.TargetLuck != -1.0)
          str1 = this.InsertLine(str1, "Luck: " + this.Config.TargetLuck.ToString());
        if (this.Config.TargetIslandWeather != "")
          str1 = this.InsertLine(str1, "Island Weather: " + this.Config.TargetIslandWeather.ToString());
        if (this.Config.TargetWeather != "")
          str1 = this.InsertLine(str1, "Weather: " + this.Config.TargetWeather.ToString());
        if (this.Config.TargetDish != "")
          str1 = this.InsertLine(str1, "Dish: " + this.Config.TargetDish.ToString());
        if (this.Config.TargetGifter != "")
          str1 = this.InsertLine(str1, "Gifter: " + this.Config.TargetGifter.ToString());
      }
      if (str1 == "")
      {return;}
      SpriteBatch spriteBatch2 = spriteBatch1;
      string label = str1;
      Vector2 vector2 = new Vector2((float) this.Config.HorizontalOffset, (float) this.Config.VerticalOffset);
      ref Vector2 local = ref vector2;
      double width = Game1.viewport.Width;
      if (drawHud)
      {
        DrawHelper.DrawHoverBox(spriteBatch2, label, in local, (float) width);
      }
    }

    private string GetStepsTaken() => this.labelStepsTaken + ": " + Game1.stats.stepsTaken.ToString();

    private string GetLuck() => this.labelDailyLuck + ": " + this.dailyLuck.ToString();

    private string GetWeatherValue(int weatherValue)
    {
      string weatherValue1 = "";
      switch (weatherValue)
      {
        case 0:
          weatherValue1 = "Sunny";
          break;
        case 1:
          weatherValue1 = "Rain";
          break;
        case 2:
          weatherValue1 = "Debris";
          break;
        case 3:
          weatherValue1 = "Lightning";
          break;
        case 4:
          weatherValue1 = "Festival";
          break;
        case 5:
          weatherValue1 = "Snow";
          break;
        case 6:
          weatherValue1 = "Wedding";
          break;
      }
      return weatherValue1;
    }

    private string GetIslandWeather(int weatherValue) => this.labelIslandWeather + ": " + this.GetWeatherValue(weatherValue);

    private string GetWeather(int weatherValue) => this.labelWeather + ": " + this.GetWeatherValue(weatherValue);

    private string GetDishOfTheDay() => this.labelDish + ": " + this.GetDishOfTheDayValue(this.dishOfTheDay) + " (" + this.dishOfTheDayAmount.ToString() + ")";

    private string GetDishOfTheDayValue(int dish) => Game1.objectInformation[dish].Split('/')[4];

    private string GetMailPerson() => this.labelGift + ": " + this.mailPerson;

    private void CheckLocations()
    {
      if (this.locationsChecked)
        return;
      this.locationsChecked = true;
      this.extraCalls = 0;
      int minutesUntilMorning = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay);
      foreach (GameLocation location in (IEnumerable<GameLocation>) Game1.locations)
      {
        OverlaidDictionary.PairsCollection pairs = location.objects.Pairs;
        foreach (KeyValuePair<Vector2, Object> keyValuePair in pairs)
        {
          Object @object = keyValuePair.Value;
          if (@object.heldObject.Value != null && !@object.name.Contains("Table") &&  @object.bigCraftable || ((Item) @object).parentSheetIndex != 165 && (!@object.name.Equals("Bee House") || location.IsOutdoors) &&  @object.minutesUntilReady - minutesUntilMorning > 0)
            ++this.extraCalls;
        }
      }
    }

    private void CalculatePredictions(
      int steps,
      out double dailyLuck,
      out int islandWeather,
      out int weather,
      out int dishOfTheDay,
      out int dishOfTheDayAmount,
      out string mailPerson)
    {
      this.CheckLocations();
      Random random = new Random((int) Game1.uniqueIDForThisGame / 100 + (int) Game1.stats.DaysPlayed * 10 + 1 + steps);
      for (int index = 0; index < Game1.dayOfMonth; ++index)
        random.Next();
      dishOfTheDay = random.Next(194, 240);
      while (((IEnumerable<int>) Utility.getForbiddenDishesOfTheDay()).Contains<int>(dishOfTheDay))
        dishOfTheDay = random.Next(194, 240);
      dishOfTheDayAmount = random.Next(1, 4 + (random.NextDouble() < 0.08 ? 10 : 0));
      random.NextDouble();
      for (int index = 0; index < this.extraCalls; ++index)
        random.Next();
      mailPerson = "";
      if ( Game1.player.friendshipData.Count() > 0)
      {
        string key = ((IEnumerable<string>)  Game1.player.friendshipData.Keys).ElementAt<string>(random.Next(( Game1.player.friendshipData.Keys).Count<string>()));
        if (random.NextDouble() < (double) ((Game1.player.friendshipData)[key].Points / 250) * 0.1 && (Game1.player.spouse == null || !Game1.player.spouse.Equals(key)) && ((ContentManager) Game1.content).Load<Dictionary<string, string>>("Data\\mail").ContainsKey(key))
          mailPerson = key;
      }
      random.NextDouble();
      dailyLuck = (double) random.Next(-100, 101) / 1000.0;
      islandWeather = random.NextDouble() < 0.24 ? 1 : 0;
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
      int num1 = 1;
      switch (Game1.currentSeason)
      {
        case "spring":
          num1 = 1;
          break;
        case "summer":
          num1 = 2;
          break;
        case "fall":
          num1 = 3;
          break;
        case "winter":
          num1 = 4;
          break;
      }
      int num2 = Game1.dayOfMonth + 1;
      if (num2 == 29)
      {
        num2 = 1;
        ++num1;
        if (num1 == 5)
          num1 = 1;
      }
      double num3 = num1 != 2 ? (num1 != 4 ? 0.183 : 0.63) : (num2 > 1 ? 0.12 + (double) num2 * 0.003 : 0.0);
      if (random.NextDouble() < num3)
      {
        weather = 1;
        if (num1 == 2 && random.NextDouble() < 0.85 || num1 != 4 && random.NextDouble() < 0.25 && num2 > 2 && Game1.stats.DaysPlayed > 26U)
          weather = 3;
        if (num1 == 4)
          weather = 5;
      }
      else
        weather = Game1.stats.DaysPlayed <= 1U || (num1 != 1 || random.NextDouble() >= 0.2) && (num1 != 3 || random.NextDouble() >= 0.6) ? 0 : 2;
      if (Utility.isFestivalDay(num2 + 1, Game1.currentSeason))
        weather = 4;
      if (Game1.stats.DaysPlayed == 1U)
        weather = 1;
      if (Game1.stats.DaysPlayed == 2U)
        weather = 0;
      if (num1 == 2 && num2 % 13 == 12)
        weather = 3;
      if (num2 != 28)
        return;
      weather = 0;
    }

    private string InsertLine(string str, string newStr) => str == "" ? newStr : str + "\r\n" + newStr;
  }
}