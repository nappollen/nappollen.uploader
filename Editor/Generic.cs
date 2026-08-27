using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase;
using VRC.SDKBase.Editor.Api;

namespace Nappollen.Uploader.Editor
{
    public static class BuilderExtension
    {
        public static readonly string AgreementText = "By clicking OK, I certify that I have the necessary rights to upload this content and that it will not infringe on any third-party legal or intellectual property rights.";

        public static bool TryFindPipeline(MonoBehaviour descriptor, out PipelineManager pipe)
        {
            if (!descriptor)
            {
                pipe = null;
                return false;
            }

            pipe = descriptor.GetComponent<PipelineManager>();
            if (!pipe)
            {
                pipe = null;
                return false;
            }

            pipe.blueprintId = EnvManager.Get("SCENE_BLUEPRINT", pipe.blueprintId);
            if (string.IsNullOrEmpty(pipe.blueprintId))
            {
                pipe = null;
                return false;
            }

            return true;
        }

        public static async Task AddCopyrightAgreement(string blueprint)
        {
            const string key = "VRCSdkControlPanel.CopyrightAgreement.ContentList";
            var keyText = SessionState.GetString(key, "");
            var list = string.IsNullOrEmpty(keyText) 
                ? new List<string>() 
                : SessionState.GetString(key, "")
                    .Split(';')
                    .ToList();
            if (list.Contains(blueprint)) return;
            list.Add(blueprint);
            SessionState.SetString(key, string.Join(";", list));
            await VRCApi.ContentUploadConsent(new VRCAgreement
            {
                AgreementCode = "content.copyright.owned",
                AgreementFulltext = AgreementText,
                ContentId = blueprint,
                Version = 1,
            });
        }
    }
}