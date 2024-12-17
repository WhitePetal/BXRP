using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IHasDependencies
    {
        void GetSourceAssetDependencies(AssetCollection assetCollection);
    }
}
