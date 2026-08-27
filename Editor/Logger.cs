using UnityEngine;

namespace Nappollen.Uploader.Editor
{
    public static class Output
    {
        public static void Log(string tag, string message)
            => Debug.Log($"[Nappollen Uploader] {tag}: {message}");

        public static void Warning(string tag, string message)
            => Debug.LogWarning($"[Nappollen Uploader] {tag}: {message}");

        public static void Error(string tag, string message)
            => Debug.LogError($"[Nappollen Uploader] {tag}: {message}");
    }
}