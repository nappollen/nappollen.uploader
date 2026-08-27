using System;
using System.Threading;
using System.Threading.Tasks;
using BestHTTP;
using UnityEngine;
using VRC.Core;
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

        // En -batchmode -nographics, EditorApplication.update ne se déclenche jamais,
        // donc BestHTTP (utilisé par l'API VRChat) ne délivre jamais ses réponses tout seul.
        // On pompe donc BestHTTP + la file de jobs VRChat sur le main thread pendant TOUT le
        // build. La tâche de pompe s'entrelace avec les await du SDK (fetch user, GetWorld,
        // BuildAndUpload…) et fait répondre chaque appel HTTP.
        public override async Task Build()
        {
            using var cts = new CancellationTokenSource();
            var pump = PumpVrcApi(cts.Token);
            try
            {
                await BuildCore();
            }
            finally
            {
                cts.Cancel();
                try { await pump; }
                catch (Exception ex) { Output.Error(nameof(WorldBuilder), $"PumpVrcApi: {ex.Message}"); }
            }
        }

        private async Task BuildCore()
        {
            Output.Log(nameof(WorldBuilder), $"Instanting {nameof(VRCSdkControlPanel)}...");
            var window = ScriptableObject.CreateInstance<VRCSdkControlPanel>();

            Output.Log(nameof(WorldBuilder), $"Await 5secs...");
            await Task.Delay(5000);

            if (!VRCSdkControlPanel.TryGetBuilder(out IVRCSdkWorldBuilderApi builder))
            {
                Output.Error(nameof(WorldBuilder), "Unable to initialize VRCSdkControlPanel (timeout).");
                return;
            }

            var tcs = new TaskCompletionSource<bool>();

            void HandleUserError(ApiModelContainer<APIUser> container)
            {
                // Attention : sur le chemin d'erreur, le SDK passe un conteneur NULL
                // (InitialFetchCurrentUser fait `onError(c as ApiModelContainer<APIUser>)`
                // avec un ApiDictContainer -> null). Ne pas déréférencer sans garde.
                var error = container?.Error ?? "Unknown error (null response container)";
                var code = container?.Code ?? 0;
                Output.Error(nameof(WorldBuilder) + " User", $"Fetch failed: code={code}, {error}");
                tcs.TrySetException(new BuilderException($"SDK Error: {error}"));
            }

            void HandleUserSuccess(ApiModelContainer<APIUser> container)
            {
                tcs.TrySetResult(true);
            }

            try
            {
                Output.Log(nameof(WorldBuilder), "Make API in Online Mode...");
                API.SetOnlineMode(true);

                // En batch mode, active les logs de la catégorie "API" du SDK pour voir
                // le détail des requêtes/réponses (dont "NOT Authenticated: ...").
                if (Application.isBatchMode)
                    VRC.Core.Logger.EnableCategory("API");

                APIUser.InitialFetchCurrentUser(HandleUserSuccess, HandleUserError);

                // Attente active sécurisée avec timeout (ex: 15 secondes max)
                float timeout = 20f;
                float elapsed = 0f;
                while (!tcs.Task.IsCompleted && elapsed < timeout)
                {
                    UpdateDelegator.ManagedUpdate();
                    // En batchmode, EditorApplication.update ne tourne pas : on doit pomper
                    // BestHTTP manuellement pour que la réponse HTTP soit délivrée.
                    HTTPManager.OnUpdate();

                    await Task.Delay(100);
                    elapsed += 0.1f;
                }

                if (!tcs.Task.IsCompleted || !await tcs.Task)
                    throw new BuilderException("User fetch timed out or failed.");

                Output.Log(nameof(WorldBuilder), $"Fetched user '{APIUser.CurrentUser.displayName}' {APIUser.CurrentUser.id}...");
            }
            catch (Exception ex)
            {
                Output.Error(nameof(WorldBuilder) + " User", ex.Message);
                throw;
            }

            tcs = new TaskCompletionSource<bool>();

            void HandleBuildError(object sender, string e)
            {
                tcs.TrySetException(new BuilderException($"SDK Error: {e}"));
            }

            void HandleBuildFinish(object sender, string e)
            {
                Output.Log(nameof(WorldBuilder) + " Build", $"Finished: {e}");
            }

            void HandleUploadError(object sender, string e)
            {
                tcs.TrySetException(new BuilderException($"SDK Error: {e}"));
            }

            void HandleUploadSuccess(object sender, string e)
            {
                Output.Log(nameof(WorldBuilder) + " Upload", $"Success");
                tcs.TrySetResult(true);
            }

            void HandleUploadFinish(object sender, string e)
            {
                Output.Log(nameof(WorldBuilder) + " Upload", $"Finished: {e}");
                tcs.TrySetResult(true);
            }

            // Abonnement global
            builder.OnSdkBuildError += HandleBuildError;
            builder.OnSdkUploadError += HandleUploadError;
            builder.OnSdkBuildFinish += HandleBuildFinish;
            builder.OnSdkUploadFinish += HandleUploadFinish;
            builder.OnSdkUploadSuccess += HandleUploadSuccess;
            builder.OnSdkBuildProgress += HandleBuildProgress;
            builder.OnSdkUploadProgress += HandleUploadProgress;

            try
            {
                Output.Log(nameof(WorldBuilder), $"Builing and Upload...");

                var world = await VRCApi.GetWorld(pipe.blueprintId, true);
                Output.Log(nameof(WorldBuilder), $"Fetched world '{world.Name}' {world.ID}...");

                if (!builder.IsValidBuilder(out var message))
                    throw new BuilderException(message);

                await BuilderExtension.AddCopyrightAgreement(world.ID);
                await builder.BuildAndUpload(world);
                if (!await tcs.Task)
                    throw new BuilderException("Build and upload failed.");
            }
            catch (Exception ex)
            {
                Output.Error(nameof(WorldBuilder) + "Build/Upload", ex.Message);
                throw ex;
            }
            finally
            {
                builder.OnSdkBuildError -= HandleBuildError;
                builder.OnSdkBuildFinish -= HandleBuildFinish;
                builder.OnSdkUploadError -= HandleUploadError;
                builder.OnSdkUploadSuccess -= HandleUploadSuccess;
                builder.OnSdkUploadFinish -= HandleUploadFinish;
                builder.OnSdkBuildProgress -= HandleBuildProgress;
                builder.OnSdkUploadProgress -= HandleUploadProgress;
            }
        }

        // Remplacer l'attente par une fonction qui laisse tourner Unity
        private static async Task WaitUntilOrTimeout(Func<bool> condition, int timeoutMs)
        {
            int elapsed = 0;
            while (!condition() && elapsed < timeoutMs)
            {
                await Task.Delay(100);
                elapsed += 100;
            }
        }

        /// <summary>
        /// Pompe BestHTTP + la file de jobs VRChat sur le main thread.
        /// En -batchmode, EditorApplication.update ne se déclenche pas : sans ce pompage,
        /// l'API VRChat (qui utilise BestHTTP) n'appelle jamais ses callbacks de réponse,
        /// d'où les timeouts ("User fetch timed out or failed", etc.).
        /// La tâche s'entrelace avec les autres await sur le main thread (UnitySynchronizationContext).
        /// </summary>
        private static async Task PumpVrcApi(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    HTTPManager.OnUpdate();
                    UpdateDelegator.ManagedUpdate();
                }
                catch (Exception ex)
                {
                    Output.Error(nameof(WorldBuilder), $"PumpVrcApi: {ex.Message}");
                }
                await Task.Delay(50);
            }
        }

        private void HandleUploadProgress(object sender, (string status, float percentage) e)
            => Output.Log(nameof(WorldBuilder) + " Upload", $"{e.percentage}: {e.status}");

        private void HandleBuildProgress(object sender, string e)
            => Output.Log(nameof(WorldBuilder) + " Build", $"{e}");
    }
}