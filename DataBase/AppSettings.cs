using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PServer_v2.DataBase
{
    public class AppSettings
    {
        public DatabaseConfig Database { get; set; }
        public EnvironmentConfig Environment { get; set; }
        public AdminConfig Admin { get; set; }

        public static AppSettings Load()
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(settingsPath))
            {
                settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            }

            string dataDirectory = GetDataDirectory();

            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath, Encoding.UTF8);
                var settings = ParseJson(json);
                
                if (string.IsNullOrEmpty(settings.Database.UserDatabasePath))
                {
                    settings.Database.UserDatabasePath = Path.Combine(dataDirectory, "PServer.db");
                }
                if (string.IsNullOrEmpty(settings.Database.GameDatabasePath))
                {
                    settings.Database.GameDatabasePath = Path.Combine(dataDirectory, "WonderlandPServer.s3db");
                }
                
                return settings;
            }

            return new AppSettings
            {
                Database = new DatabaseConfig
                {
                    UserDatabasePath = Path.Combine(dataDirectory, "PServer.db"),
                    GameDatabasePath = Path.Combine(dataDirectory, "WonderlandPServer.s3db")
                },
                Environment = new EnvironmentConfig { IsDevelopment = false },
                Admin = new AdminConfig
                {
                    Username = "admin",
                    Password = "admin",
                    CharacterName = "admin",
                    GMLevel = 255
                }
            };
        }

        private static string GetDataDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = baseDir;
            
            for (int i = 0; i < 5; i++)
            {
                string testPath = Path.Combine(projectRoot, "pServer", "data");
                if (Directory.Exists(testPath))
                {
                    return testPath;
                }
                projectRoot = Path.GetDirectoryName(projectRoot);
                if (string.IsNullOrEmpty(projectRoot)) break;
            }
            
            string defaultPath = Path.Combine(baseDir, "pServer", "data");
            if (!Directory.Exists(defaultPath))
            {
                Directory.CreateDirectory(defaultPath);
            }
            return defaultPath;
        }

        private static AppSettings ParseJson(string json)
        {
            var settings = new AppSettings
            {
                Database = new DatabaseConfig(),
                Environment = new EnvironmentConfig(),
                Admin = new AdminConfig()
            };

            string dataDir = GetDataDirectory();
            settings.Database.UserDatabasePath = ExtractString(json, "UserDatabasePath", "");
            settings.Database.GameDatabasePath = ExtractString(json, "GameDatabasePath", "");
            
            if (string.IsNullOrEmpty(settings.Database.UserDatabasePath))
            {
                settings.Database.UserDatabasePath = Path.Combine(dataDir, "PServer.db");
            }
            if (string.IsNullOrEmpty(settings.Database.GameDatabasePath))
            {
                settings.Database.GameDatabasePath = Path.Combine(dataDir, "WonderlandPServer.s3db");
            }
            settings.Environment.IsDevelopment = ExtractBool(json, "IsDevelopment", false);
            settings.Admin.Username = ExtractString(json, "Username", "admin");
            settings.Admin.Password = ExtractString(json, "Password", "admin");
            settings.Admin.CharacterName = ExtractString(json, "CharacterName", "admin");
            settings.Admin.GMLevel = ExtractInt(json, "GMLevel", 255);

            return settings;
        }

        private static string ExtractString(string json, string key, string defaultValue)
        {
            var pattern = "\"" + key + "\"\\s*:\\s*\"([^\"]+)\"";
            var match = Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : defaultValue;
        }

        private static bool ExtractBool(string json, string key, bool defaultValue)
        {
            var pattern = "\"" + key + "\"\\s*:\\s*(true|false)";
            var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.ToLower() == "true";
            }
            return defaultValue;
        }

        private static int ExtractInt(string json, string key, int defaultValue)
        {
            var pattern = "\"" + key + "\"\\s*:\\s*(\\d+)";
            var match = Regex.Match(json, pattern);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int value))
            {
                return value;
            }
            return defaultValue;
        }
    }

    public class DatabaseConfig
    {
        public string UserDatabasePath { get; set; }
        public string GameDatabasePath { get; set; }
    }

    public class EnvironmentConfig
    {
        public bool IsDevelopment { get; set; }
    }

    public class AdminConfig
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string CharacterName { get; set; }
        public int GMLevel { get; set; }
    }
}

