using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline.LightTransport
{
    public class IntegrationContext : IDisposable
    {
        ~IntegrationContext()
        {
            Destroy();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        private void Destroy()
        {

        }
    }
}
