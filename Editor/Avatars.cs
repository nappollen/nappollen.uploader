using System.Threading.Tasks;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase;

namespace Nappollen.Uploader.Editor
{
    public static class AvatarBuilderExtension
    {
        public static bool TryMake(out AvatarBuilder builder)
        {
            var descriptor = Object.FindAnyObjectByType<VRC_AvatarDescriptor>(FindObjectsInactive.Exclude);
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

    public class AvatarBuilder : Builder
    {
        public VRC_AvatarDescriptor descriptor;
    }
}