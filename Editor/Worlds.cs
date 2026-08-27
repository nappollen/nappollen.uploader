using System;
using System.Threading.Tasks;
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
        public override async Task Build()
        {
            var window = ScriptableObject.CreateInstance<VRCSdkControlPanel>();

            await Task.Delay(5000);

            if (!VRCSdkControlPanel.TryGetBuilder(out IVRCSdkWorldBuilderApi builder))
            {
                Output.Error(nameof(WorldBuilder), "Unable to initialize VRCSdkControlPanel (timeout).");
                return;
            }

            var tcs = new TaskCompletionSource<bool>();

            void HandleUserError(ApiModelContainer<APIUser> container)
            {
                tcs.TrySetException(new BuilderException($"SDK Error: {container.Error}"));
            }

            void HandleUserSuccess(ApiModelContainer<APIUser> container)
            {
                tcs.TrySetResult(true);
            }

            try
            {
                APIUser.InitialFetchCurrentUser(HandleUserSuccess, HandleUserError);
                if (!await tcs.Task)
                    throw new BuilderException("User fetch failed.");
                Output.Log(nameof(WorldBuilder), $"Fetched user '{APIUser.CurrentUser.displayName}' {APIUser.CurrentUser.id}...");
            }
            catch (Exception ex)
            {
                Output.Error(nameof(WorldBuilder) + " User", ex.Message);
                throw ex;
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

        private void HandleUploadProgress(object sender, (string status, float percentage) e)
            => Output.Log(nameof(WorldBuilder) + " Upload", $"{e.percentage}: {e.status}");

        private void HandleBuildProgress(object sender, string e)
            => Output.Log(nameof(WorldBuilder) + " Build", $"{e}");
    }
}