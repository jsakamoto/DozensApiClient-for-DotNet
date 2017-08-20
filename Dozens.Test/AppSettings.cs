using System;
using System.IO;
using Newtonsoft.Json;

namespace DozensAPI.Test
{
    public class AppSettings
    {
        public string DozensId { get; set; }

        public string APIKey { get; set; }

        public bool UseMock { get; set; }

        public static AppSettings Load()
        {
            var baseDir = Directory.GetCurrentDirectory();
            var appSettings = new AppSettings();

            foreach (var fname in new[] { "application.json", "application.user.json" })
            {
                var path = Path.Combine(baseDir, fname);
                var json = File.ReadAllText(path);
                JsonConvert.PopulateObject(json, appSettings);
            }

            return appSettings;
        }
    }
}
