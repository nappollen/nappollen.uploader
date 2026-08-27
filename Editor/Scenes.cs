using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;

namespace Nappollen.Uploader.Editor
{
    public static class Scenes
    {
        public static string ScenePath
            => EnvManager.Get("SCENE_PATH", "Assets/OPENME.unity");
            
        [MenuItem("Tools/Uploader/Open Scene")]
        public static void Open()
        {
            var path = ScenePath;

            var active = SceneManager.GetActiveScene();
            if (active.isLoaded && active.path == path)
            {
                Debug.Log($"Scene already open: {path}.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new Exception($"The scene '{path}' is not valid.");

            Debug.Log($"Scene opened: {path}.");
        }
    }
}