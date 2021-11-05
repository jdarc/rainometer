using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Rainometer
{
    internal static class Program
    {
        private const string KeyName = "HKEY_CURRENT_USER\\SOFTWARE\\Zentient\\Rainometer";
        private const string BaseQueryUrl = "https://api.openweathermap.org/data/2.5/weather";

        private static readonly TextInfo TextInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
        private static readonly HttpClient Curl = new();

        private static void Main()
        {
            var notifyIcon = new NotifyIcon();
            notifyIcon.Icon = null;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenu = new ContextMenu(new[] {new MenuItem("Exit", (_, _) => Application.Exit())});

            var cityId = Registry.GetValue(KeyName, "CityId", "");
            var units = Registry.GetValue(KeyName, "Units", "metric");
            var apiKey = Registry.GetValue(KeyName, "ApiKey", "");
            var uri = $"{BaseQueryUrl}?id={cityId}&units={units}&APPID={apiKey}";

            var error = new Icon(typeof(Program), "Error.ico");
            var weatherIcons = LoadWeatherIcons();

            async void Refresh(object _)
            {
                string desc;
                Icon icon;
                try
                {
                    var data = (await Curl.GetStringAsync(uri)).FromJson<WeatherData>();
                    desc = $"{TextInfo.ToTitleCase(data.Weather[0].Description)}. {(int) data.Main.Temp}°C";
                    icon = weatherIcons[data.Weather[0].Icon];
                }
                catch (Exception ex)
                {
                    desc = ex.Message;
                    icon = error;
                }

                notifyIcon.Text = desc;
                notifyIcon.Icon?.Dispose();
                notifyIcon.Icon = icon;
            }

            var timer = new System.Threading.Timer(Refresh, null, 0, 60000);

            Application.ApplicationExit += (_, _) =>
            {
                timer.Dispose();
                notifyIcon.Dispose();
            };

            Application.Run();
        }

        private static Dictionary<string, Icon> LoadWeatherIcons()
        {
            var missing = new Icon(typeof(Program), "Missing.ico");
            var icons = new Dictionary<string, Icon>();

            var assembly = Assembly.GetExecutingAssembly();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(".png", true, CultureInfo.InvariantCulture))
                {
                    using var stream = assembly.GetManifestResourceStream(name);
                    var key = name.Replace(".png", "").Split('.').Last().Trim().ToLower();
                    icons.Add(key, stream != null ? ImageStreamToIcon(stream) : missing);
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
