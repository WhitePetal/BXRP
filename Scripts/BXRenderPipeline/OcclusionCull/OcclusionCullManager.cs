using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline.OcclusionCulling
{
    public class OcclusionCullManager
    {
        private static OcclusionCullManager _instance = new OcclusionCullManager();
		public static OcclusionCullManager instance => _instance;


    }
}
