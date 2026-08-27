using System;
using System.IO;

namespace Nappollen.Uploader.Editor
{
    public static class EnvManager
    {
        public static string Get(string key, string defaultValue = null)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(value))
                return value;

            value = ReadKeyFromDotEnv(key);
            if (!string.IsNullOrEmpty(value))
                return value;

            return defaultValue;
        }

        public static bool Has(string key) 
            => !string.IsNullOrEmpty(Get(key));

        private static string ReadKeyFromDotEnv(string key)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (!File.Exists(path))
                return null;

            foreach (var line in File.ReadAllLines(path))
                if (TryExtractValue(line, key, out string value))
                    return value;

            return null;
        }

        private static bool TryExtractValue(string line, string targetKey, out string value)
        {
            value = null;
            string trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                return false;

            int separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
                return false;

            string currentKey = trimmed.Substring(0, separatorIndex).Trim();
            if (currentKey != targetKey)
                return false;

            string rawValue = trimmed.Substring(separatorIndex + 1).Trim();
            value = CleanQuotes(rawValue);
            return true;
        }

        private static string CleanQuotes(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            bool isDoubleQuoted = value.StartsWith("\"") && value.EndsWith("\"");
            bool isSingleQuoted = value.StartsWith("'") && value.EndsWith("'");

            if ((isDoubleQuoted || isSingleQuoted) && value.Length >= 2)
                return value.Substring(1, value.Length - 2);

            return value;
        }
    }
}