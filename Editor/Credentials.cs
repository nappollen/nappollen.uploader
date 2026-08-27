using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace Nappollen.Uploader.Editor
{
    public static class Credentials
    {
        public static string FILENAME = ".env.exported";

        public static string Token
            => EnvManager.Get("VRCHAT_TOKEN")
            ?? throw new InvalidDataException("Missing VRCHAT_AUTHTOKEN");

        public static string Provider
            => EnvManager.Get("VRCHAT_PROVIDER")
            ?? "vrchat";

        public static string ProviderId
            => EnvManager.Get("VRCHAT_PROVIDER_ID")
            ?? null;

        public static string TwoFactorToken
            => EnvManager.Get("VRCHAT_2FA_TOKEN")
            ?? throw new InvalidDataException("Missing VRCHAT_2FA_TOKEN");

        public static string Human
            => EnvManager.Get("VRCHAT_HUMAN")
            ?? null;

        public static void Import()
        {
            if (!ApiCredentials.IsLoaded())
                ApiCredentials.Load();
            ApiCredentials.Set(
                Human,
                ProviderId,
                Provider,
                Token,
                TwoFactorToken
            );
            Debug.Log($"Credentials imported (Logged as {Human ?? "auto"}).");
        }

        public static void Export(string path)
        {
            if (!ApiCredentials.IsLoaded())
                ApiCredentials.Load();
            (string, string)[] kvs = {
                ("VRCHAT_TOKEN", ApiCredentials.GetAuthToken()),
                ("VRCHAT_PROVIDER", ApiCredentials.GetAuthTokenProvider()),
                ("VRCHAT_PROVIDER_ID", ApiCredentials.GetAuthTokenProviderUserId()),
                ("VRCHAT_2FA_TOKEN", ApiCredentials.GetTwoFactorAuthToken()),
                ("VRCHAT_HUMAN", ApiCredentials.GetHumanName()),
            };
            var sb = new StringBuilder();
            foreach (var kv in kvs)
                sb.AppendFormat("{0}={1}\n", kv.Item1, kv.Item2);
            var dir = Path.GetDirectoryName(path);
            EnsureDirectoryExists(dir);
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        [MenuItem("Tools/Uploader/Export Credentials")]
        private static void ExportCredentials()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), FILENAME);
            if (File.Exists(path) && !EditorUtility.DisplayDialog(
                "Export Credentials",
                $"Do you want to replace the old {FILENAME} ?",
                "Yes, replace",
                "Cancel"
            ))
            {
                Debug.LogWarning($"You're cancelled exporting credentials.");
                return;
            }
            Export(path);
            Debug.Log($"Exported at {path}.");
            if (EditorUtility.DisplayDialog(
                "Export Credentials",
                $"Credentials exported at {path}, open it ?",
                "Open",
                "Ok"
            ))
                EditorUtility.OpenWithDefaultApp(path);
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path) || Directory.Exists(path))
                return;
            Directory.CreateDirectory(path);
        }
    }
}