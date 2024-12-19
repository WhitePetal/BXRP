using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BXGeometryGraph
{
    public struct PreviewProperty
    {
        public string name { get; set; }
        public PropertyType propType { get; private set; }

        public PreviewProperty(PropertyType type) : this()
        {
            propType = type;
        }

        const string k_SetErrorMessage = "Cannot set a {0} property on a PreviewProperty with type {1}.";
        const string k_GetErrorMessage = "Cannot get a {0} property on a PreviewProperty with type {1}.";

        [StructLayout(LayoutKind.Explicit)]
        struct Data
        {
            [FieldOffset(0)]
            public Color colorValue;
            [FieldOffset(0)]
            public Texture textureValue;
            [FieldOffset(0)]
            public Cubemap cubemapValue;
            [FieldOffset(0)]
            public Gradient gradientValue;
            [FieldOffset(0)]
            public Vector4 vector4Value;
            [FieldOffset(0)]
            public float floatValue;
            [FieldOffset(0)]
            public bool booleanValue;
        }

        private Data m_Data;

        public Color colorValue
        {
            get
            {
                if(propType != PropertyType.Color)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Color, propType));
                return m_Data.colorValue;
            }
            set
            {
                if(propType != PropertyType.Color)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Color, propType));
                m_Data.colorValue = value;
            }
        }

        public Texture textureValue
        {
            get
            {
                if (propType != PropertyType.Texture2D && propType != PropertyType.Texture2DArray && propType != PropertyType.Texture3D)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Texture2D, propType));
                return m_Data.textureValue;
            }
            set
            {
                if (propType != PropertyType.Texture2D && propType != PropertyType.Texture2DArray && propType != PropertyType.Texture3D)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Texture2D, propType));
                m_Data.textureValue = value;
            }
        }

        public Cubemap cubemapValue
        {
            get
            {
                if (propType != PropertyType.Cubemap)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Cubemap, propType));
                return m_Data.cubemapValue;
            }
            set
            {
                if (propType != PropertyType.Cubemap)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Cubemap, propType));
                m_Data.cubemapValue = value;
            }
        }

        public Gradient gradientValue
        {
            get
            {
                if (propType != PropertyType.Gradient)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Gradient, propType));
                return m_Data.gradientValue;
            }
            set
            {
                if (propType != PropertyType.Gradient)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Gradient, propType));
                m_Data.gradientValue = value;
            }
        }

        public Vector4 vector4Value
        {
            get
            {
                if (propType != PropertyType.Vector2 && propType != PropertyType.Vector3 && propType != PropertyType.Vector4)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Vector4, propType));
                return m_Data.vector4Value;
            }
            set
            {
                if (propType != PropertyType.Vector2 && propType != PropertyType.Vector3 && propType != PropertyType.Vector4
                    && propType != PropertyType.Matrix2 && propType != PropertyType.Matrix3 && propType != PropertyType.Matrix4)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Vector4, propType));
                m_Data.vector4Value = value;
            }
        }

        public float floatValue
        {
            get
            {
                if (propType != PropertyType.Float)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Float, propType));
                return m_Data.floatValue;
            }
            set
            {
                if (propType != PropertyType.Float)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Float, propType));
                m_Data.floatValue = value;
            }
        }

        public bool booleanValue
        {
            get
            {
                if (propType != PropertyType.Boolean)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Boolean, propType));
                return m_Data.booleanValue;
            }
            set
            {
                if (propType != PropertyType.Boolean)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Boolean, propType));
                m_Data.booleanValue = value;
            }
        }

        public void SetValueOnMaterialPropertyBlock(MaterialPropertyBlock mat)
        {
            // TODO
        }
    }
}