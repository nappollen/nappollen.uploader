using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Nappollen.Uploader.Editor
{
    public static class Output
    {
        public static void Log(string tag, string message)
        {
            string formatted = $"{tag}: {message}";
            Debug.Log(Format(formatted));
            SendToDiscord($"`LOG` {formatted}");
        }

        public static void Warning(string tag, string message)
        {
            string formatted = $"{tag}: {message}";
            Debug.LogWarning(Format(formatted));
            SendToDiscord($"`WAR` __{formatted}__");
        }

        public static void Error(string tag, string message)
        {
            string formatted = $"{tag}: {message}";
            Debug.LogError(Format(formatted));
            SendToDiscord($"`ERR` **{formatted}**");
        }

        private static void SendToDiscord(string content)
        {
            string webhookUrl = EnvManager.Get("OUTPUT_WEBHOOK");
            if (string.IsNullOrEmpty(webhookUrl))
                return;

            string jsonBody = $"{{\"content\": \"{EscapeJson(content)}\"}}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            var request = new UnityWebRequest(webhookUrl, "POST")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning(Format($"Failed to send Discord webhook: {request.error}"));
                request.Dispose();
            };
        }

        private static string Format(string str)
            => $"[Nappollen Uploader] {str}";

        private static string EscapeJson(string str)
            => str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r");
    }
}