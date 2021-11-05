namespace Rainometer
{
    // ReSharper disable UnassignedReadonlyField
    public readonly struct WeatherData
    {
        public readonly struct WeatherNode
        {
            public readonly string Description;
            public readonly string Icon;
        }

        public readonly struct MainNode
        {
            public readonly double Temp;
        }

        public readonly WeatherNode[] Weather;
        public readonly MainNode Main;
    }
}