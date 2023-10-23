using StardewValley;
namespace StepsTakenOnScreen
{
    //weather key:
    //0 = Sunny
    //1 = Rain
    //2 = Debris (windy)
    //3 = lightning (storm)
    //4 = Festival
    //5 = Snow
    //6 = Wedding
    public class FlagSet 
    {
        private int _dayOfMonth { get;}
        private string _targetSeason { get;}
        private int _predictedWeatherNextDay { get;}

        public FlagSet(int targetDay, string targetSeason, int predictedWeather)
        {
            _dayOfMonth = targetDay;
            _targetSeason = targetSeason;
            _predictedWeatherNextDay = predictedWeather;
        }
        public int GetDay()
        {
            return _dayOfMonth;
        }

        public string GetSeason()
        {
            return _targetSeason;
        }

        public int GetWeather()
        {
            return _predictedWeatherNextDay;
        }
    }
}