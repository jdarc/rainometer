using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Rainometer
{
    public static class Weather
    {
        private const string KeyName = "HKEY_CURRENT_USER\\SOFTWARE\\Zentient\\Rainometer";
        private const string BaseQueryUrl = "https://api.openweathermap.org/data/2.5/weather";

        private static readonly Icon ErrorIcon = new(typeof(Weather), "Error.ico");
        private static readonly Icon MissingIcon = new(typeof(Weather), "Missing.ico");
        private static readonly Dictionary<string, Icon> WeatherIcons = LoadWeatherIcons();

        private static readonly HttpClient Curl = new();
        private static readonly TextInfo TextInfo = Thread.CurrentThread.CurrentCulture.TextInfo;

        public static async Task Check(NotifyIcon target)
        {
            string desc;
            Icon icon;

            try
            {
                var cityId = Registry.GetValue(KeyName, "CityId", "2950159");
                var units = Registry.GetValue(KeyName, "Units", "metric");
                var apiKey = Registry.GetValue(KeyName, "ApiKey", "");
                var uri = $"{BaseQueryUrl}?id={cityId}&units={units}&APPID={apiKey}";
                var data = (await Curl.GetStringAsync(uri)).FromJson<WeatherData>();
                desc = $"{TextInfo.ToTitleCase(data.Weather[0].Description)}. {(int) data.Main.Temp}°C";
                icon = WeatherIcons[data.Weather[0].Icon];
            }
            catch (Exception ex)
            {
                desc = ex.Message;
                icon = ErrorIcon;
            }

            target.Text = desc;
            target.Icon?.Dispose();
            target.Icon = (Icon) icon.Clone();
        }

        private static Dictionary<string, Icon> LoadWeatherIcons()
        {
            var icons = new Dictionary<string, Icon>();

            var assembly = Assembly.GetExecutingAssembly();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(".png", true, CultureInfo.InvariantCulture))
                {
                    using var stream = assembly.GetManifestResourceStream(name);
                    var key = name.Replace(".png", "").Split('.').Last().Trim().ToLower();
                    icons.Add(key, stream != null ? ImageStreamToIcon(stream) : MissingIcon);
                }
            }

            return icons;
        }

        private static Icon ImageStreamToIcon(Stream stream)
        {
            return Icon.FromHandle(new Bitmap(Image.FromStream(stream)).GetHicon());
        }
    }
}
