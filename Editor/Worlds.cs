using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Editor;
using VRC.SDKBase;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.Api;
using Object = UnityEngine.Object;

namespace Nappollen.Uploader.Editor
{
    public static class WorldBuilderExtension
    {
        public static bool TryMake(out WorldBuilder builder)
        {
            var descriptor = Object.FindAnyObjectByType<VRC_SceneDescriptor>(FindObjectsInactive.Exclude);
            if (!BuilderExtension.TryFindPipeline(descriptor, out var pipe))
            {
                builder = null;
                return false;
            }
            builder = new()
            {
                pipe = pipe,
                descriptor = descriptor
            };
            return true;
        }
    }

    public class WorldBuilder : Builder
    {
        public VRC_SceneDescriptor descriptor;
        public override async Task Build()
        {
            var window = EditorWindow.GetWindowDontShow<VRCSdkControlPanel>();

            await Task.Delay(5000);

            if (!VRCSdkControlPanel.TryGetBuilder(out IVRCSdkWorldBuilderApi builder))
            {
                Debug.LogError("Impossible d'initialiser le VRCSdkControlPanel (timeout).");
                return;
            }

            var tcs = new TaskCompletionSource<bool>();

            // --- Événements de Build ---
            void HandleBuildError(object sender, string e)
            {
                tcs.TrySetException(new BuilderException($"Erreur de Build SDK : {e}"));
            }

            void HandleBuildFinish(object sender, string e)
            {
                Debug.Log($"Build terminé : {e}");
            }

            // --- Événements d'Upload ---
            void HandleUploadError(object sender, string e)
            {
                tcs.TrySetException(new BuilderException($"Erreur d'Upload SDK : {e}"));
            }

            void HandleUploadSuccess(object sender, string e)
            {
                Debug.Log($"Upload réussi : {e}");
                tcs.TrySetResult(true);
            }

            void HandleUploadFinish(object sender, string e)
            {
                Debug.Log($"Upload terminé : {e}");
                // Si Success n'est pas appelé, Finish fait foi de conclusion
                tcs.TrySetResult(true);
            }

            // Abonnement global
            builder.OnSdkBuildError += HandleBuildError;
            builder.OnSdkBuildFinish += HandleBuildFinish;
            builder.OnSdkUploadError += HandleUploadError;
            builder.OnSdkUploadSuccess += HandleUploadSuccess;
            builder.OnSdkUploadFinish += HandleUploadFinish;

            try
            {
                var world = await VRCApi.GetWorld(pipe.blueprintId, true);
                await BuilderExtension.AddCopyrightAgreement(world.ID);
                
                await builder.BuildAndUpload(world);
                bool success = await tcs.Task;
                Debug.Log($"Résultat global du Build & Upload : {success}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Échec de la procédure : {ex.Message}");
            }
            finally
            {
                // Nettoyage de l'ensemble des handlers
                builder.OnSdkBuildError -= HandleBuildError;
                builder.OnSdkBuildFinish -= HandleBuildFinish;
                builder.OnSdkUploadError -= HandleUploadError;
                builder.OnSdkUploadSuccess -= HandleUploadSuccess;
                builder.OnSdkUploadFinish -= HandleUploadFinish;
            }
        }
    }
}