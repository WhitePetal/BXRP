using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace BXRenderPipeline.LightTransport
{
    public interface IWorld : IDisposable
    {
        // Functionality like AddInstance/RemoveInstance will be added in the future.
    }

    public class WintermuteWorld : IWorld
    {
        private IntegrationContext integrationContext;

        public void Dispose()
        {
        }

        public IntegrationContext GetIntegrationContext()
        {
            return integrationContext;
        }

        public void SetIntegrationContext(IntegrationContext context)
        {
            integrationContext = context;
        }
    }

    public class RadeonRaysWorld : IWorld
    {
        private IntegrationContext integrationContext;

        public void Dispose()
        {
        }

        public IntegrationContext GetIntegrationContext()
        {
            return integrationContext;
        }

        public void SetIntegrationContext(IntegrationContext context)
        {
            integrationContext = context;
        }
    }
}
