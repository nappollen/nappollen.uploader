using System;
using System.Threading.Tasks;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase;

namespace Nappollen.Uploader.Editor
{
    public class Builder
    {
        public PipelineManager pipe;

        public virtual Task Build()
            => Task.FromException(new NotImplementedException());
    }
}