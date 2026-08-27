using System;
using UnityEditor;
using UnityEngine;

namespace Nappollen.Uploader.Editor
{
    public static class BatchBuildRunner
    {
        public static async void RunBuild()
        {
            Debug.Log("[BatchMode] Lancement du processus de build VRCSdk...");

            try
            {
                // Appel direct si UploaderEditor et Build() sont statiques
                await UploaderEditor.Build();

                Debug.Log("[BatchMode] Build et Upload terminés avec succès.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BatchMode] Échec critique du build : {ex.Message}");
                EditorApplication.Exit(1);
            }
        }
    }
}