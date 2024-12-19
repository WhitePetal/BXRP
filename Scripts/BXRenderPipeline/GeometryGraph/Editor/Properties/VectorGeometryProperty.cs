using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    public abstract class VectorGeometryProperty : AbstractGeometryProperty<Vector4>
    {
        internal override bool isExposable => true;
        internal override bool isRenamable => true;
        internal virtual int vectorDimension => 4;

        internal override string GetHLSLVariableName(bool isSubgraphProperty, GenerationMode mode)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            if (decl == HLSLDeclaration.HybridPerInstance)
                return $"UNITY_ACCESS_HYBRID_INSTANCED_PROP({referenceName}, {concretePrecision.ToGeometryString()}{vectorDimension})";
            else
                return base.GetHLSLVariableName(isSubgraphProperty, mode);
        }

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{referenceName}(\"{displayName}\", Vector) = ({NodeUtils.FloatToShaderValueShaderLabSafe(value.x)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.y)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.z)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.w)})";
        }

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return $"{concreteGeometryValueType.ToGeometryString(precisionString)} {referenceName}";
        }
    }
}
