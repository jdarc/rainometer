using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Rainometer
{
    internal static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private const string KeyName = "HKEY_CURRENT_USER\\SOFTWARE\\Zentient\\Rainometer";
        private const string BaseQueryUrl = "https://api.openweathermap.org/data/2.5/weather";
        private const string BaseIconUrl = "https://openweathermap.org/img/wn";

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

            async void Refresh(object _)
            {
                var json = (await Curl.GetStringAsync(uri)).FromJson<WeatherData>();
                var icon = CropImage(await DownloadIcon(json));
                
                notifyIcon.Text = GenerateDescription(json);
                notifyIcon.Icon?.Dispose();
                notifyIcon.Icon = ConvertToIcon(icon);
            }

            var timer = new System.Threading.Timer(Refresh, null, 0, 60000);

            Application.ApplicationExit += (_, _) =>
            {
                timer.Dispose();
                notifyIcon.Dispose();
            };

            Application.Run();
        }

        private static async Task<Image> DownloadIcon(WeatherData json)
        {
            var iconData = await Curl.GetByteArrayAsync($"{BaseIconUrl}/{json.Weather[0].Icon}@2x.png");
            using var iconStream = new MemoryStream(iconData);
            return Image.FromStream(iconStream);
        }

        private static Bitmap CropImage(Image original)
        {
            var processed = new Bitmap(80, 80);
            var rect = new RectangleF(10.0f, 10.0f, 80.0f, 80.0f);
            using var g = Graphics.FromImage(processed);
            g.DrawImage(original, 0.0f, 0.0f, rect, GraphicsUnit.Pixel);
            return processed;
        }

        private static string GenerateDescription(WeatherData json)
        {
            var textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
            var desc = textInfo.ToTitleCase(json.Weather[0].Description);
            return $"Feels like {json.Main.Temp} °C. {desc}.";
        }

        private static Icon ConvertToIcon(Bitmap bitmap)
        {
            var handle = bitmap.GetHicon();
            var resource = Icon.FromHandle(handle);
            var icon = (Icon) resource.Clone();
            resource.Dispose();
            DestroyIcon(handle);
            return icon;
        }
    }
}