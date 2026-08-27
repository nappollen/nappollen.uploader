using System;
using UnityEditor;
using UnityEngine;

namespace Nappollen.Uploader.Editor
{
    public static class BatchBuildRunner
    {
        public static async void RunBuild()
        {
            Output.Log(nameof(BatchBuildRunner), "Starting the VRCSdk build process...");

            try
            {
                // Appel direct si UploaderEditor et Build() sont statiques
                await UploaderEditor.Build();

                Output.Log(nameof(BatchBuildRunner), "Build and upload completed successfully.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Output.Error(nameof(BatchBuildRunner), $"Critical build failure: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }
    }
}