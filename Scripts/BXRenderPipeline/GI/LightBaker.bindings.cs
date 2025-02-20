#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BXRenderPipeline.LightTransport;
using UnityEngine;

namespace BXRenderPipelineEditor.LightBaking
{
    internal static partial class LightBaker
    {
        public enum ResultType : uint
        {
            Success = 0,
            Cancelled,
            JobFailed,
            OutOfMemory,
            InvalidInput,
            LowLevelAPIFailure,
            FailedCreatingJobQueue,
            IOFailed,
            ConnectedToBaker,
            Undefined
        }

        public struct Result
        {
            public ResultType type;
            public string message;
            public override string ToString()
            {
                if (message.Length == 0)
                    return $"Result type: '{type}'";
                return $"Result type: '{type}', message: '{message}'";
            }

            public IProbeIntegrator.Result ConvertToIProbeIntegratorResult()
            {
                IProbeIntegrator.Result result = new()
                {
                    type = (IProbeIntegrator.ResultType)type,
                    message = message
                };
                return result;
            }
        }

        public enum Backend
        {
            CPU = 0,
            GPU = 1,
            UnityComputeGPU = 2
        }

        public enum TransmissionChannels
        {
            Red = 0,
            Alpha = 1,
            AlphaCutout = 2,
            RGB = 3,
            None = 4
        }

        public enum TransmissionType
        {
            Opacity = 0,
            Transparency = 1,
            None = 2
        }

        public enum MeshType
        {
            Terrain = 0,
            MeshRenderer = 1
        }

        public enum MixedLightingMode
        {
            IndirectOnly = 0,
            Subtractive = 1,
            Shadowmask = 2,
        }

        public enum LightmapBakeMode
        {
            NonDirectional = 0,
            CombineDirectional = 1
        }

        [Flags]
        public enum ProbeRequestOutputType : uint
        {
            RadianceDirect = 1 << 0,
            RadianceIndirect = 1 << 1,
            Validity = 1 << 2,
            MixedLightOcclusion = 1 << 3,
            LightProbeOcclusion = 1 << 4,
            EnvironmentOcclusion = 1 << 5,
            Depth = 1 << 6,
            All = 0xFFFFFFFF
        }

        public struct ProbeRequest
        {
            public ProbeRequestOutputType outputTypeMask;
            public ulong positionOffset;
            public ulong positionLength;
            public float pushoff;
            public string outputFolderPath;

            // Environment occlusion
            public ulong integrationRadiusOffset;
            public uint environmentOcclusionSampleCount;
            public bool ignoreDirectEnvironment;
            public bool ignoreIndirectEnvironment;
        }

        [Flags]
        public enum LightmapRequestOutputType : uint
        {
            IrradianceIndirect = 1 << 0,
            IrradianceDirect = 1 << 1,
            IrradianceEnvironment = 1 << 2,
            Occupancy = 1 << 3,
            Validity = 1 << 4,
            DirectionalityIndirect = 1 << 5,
            DirectionalityDirect = 1 << 6,
            AmbientOcclusion = 1 << 7,
            Shadowmask = 1 << 8,
            Normal = 1 << 9,
            ChartIndex = 1 << 10,
            OverlapPixelIndex = 1 << 11,
            All = 0xFFFFFFFF
        }

        public enum TillingMode : byte
        {
            // Assuming a 4k lightmap (16M texels), the tiling will yield the following chunk sizes:
            None = 0,                 // 4k * 4k =    16M texels
            Quarter = 1,              // 2k * 2k =     4M texels
            Sixteenth = 2,            // 1k * 1k =     1M texels
            Sixtyfourth = 3,          // 512 * 512 = 262k texels
            TwoHundredFiftySixth = 4, // 256 * 256 = 262k texels
            Max = TwoHundredFiftySixth,
            Error = 5                // Error. We do't want to go lower (GPU occupancy will start to be a problem for samller atlas sizes)
        }

        public struct LightmapRequest
        {
            public LightmapRequestOutputType outputTypeMask;
            public uint lightmapOffset;
            public uint lightmapCount;
            public TillingMode tillingMode;
            public string outputFolderPath;
            public float pushoff;
        }

        public struct Resolution
        {
            public uint width;
            public uint height;

            public Resolution(uint widthIn, uint heightIn)
            {
                width = widthIn;
                height = heightIn;
            }
        }

        public struct TextureData
        {
            public Resolution resolution;
            public Vector4[] data;

            public TextureData(Resolution resolutionIn)
            {
                resolution = resolutionIn;
                data = new Vector4[resolution.width * resolution.height];
            }
        }

        public struct TextureTransform
        {
            public Vector2 scale;
            public Vector2 offset;
        }

        public struct TextureProperties
        {
            public TextureWrapMode wrapModeU;
            public TextureWrapMode wrapModeV;
            public FilterMode filterMode;
            public TextureTransform textureST;
        }

        public struct CookieData
        {
            public Resolution resolution;
            public uint pixelStride;
            public uint slices;
            public bool repeat;
            public byte[] data;

            public CookieData(Resolution resolutionIn, uint pixelStrideIn, uint slicesIn, bool repeatIn)
            {
                resolution = resolutionIn;
                pixelStride = pixelStrideIn;
                slices = slicesIn;
                repeat = repeatIn;
                data = new byte[resolution.width * resolution.height * slices * pixelStride];
            }
        }

        public struct Instance
        {
            public MeshType meshType;
            public int meshIndex; // index into BakeInput::m_MeshData, -1 for Terrain
            public int terrainIndex; // index into BakeInput::m_TerrainData, -1 for MeshRenderer
            public Matrix4x4 transform;
            public bool castShadows;
            public bool receiveShadows;
            public bool oddNegativeScale;
            public int lodGroup;
            public byte lodMask;
            public int[] submeshMaterialIndices;
        }

        public struct Terrain
        {
            public uint heightMapIndex; // index into BakeInput::m_HeightmapData
            public int terrainHoleIndex; // index into BakeInput::m_TerrainHoleData -1 means no hole data
            public float outputResolution;
            public Vector3 heightmapScale;
            public Vector4 uvBounds;
        }

        public struct Material
        {
            public bool doubleSideGI;
            public TransmissionChannels transmissionChannels;
            public TransmissionType transmissionType;
        }

        public enum LightType : byte
        {
            Directional = 0,
            Point = 1,
            Spot = 2,
            Rectangle = 3,
            Disc = 4,
            SpotPyramidShape = 5,
            SpotBoxShape = 6
        }

        public enum FalloffType : byte
        {
            InverseSquared = 0,
            InverseSquaredNoRangeAttenuation = 1,
            Linear = 2,
            Legacy = 3
        }

        public enum AngularFalloffType : byte
        {
            LUT = 0,
            AnalyticAndInnerAngle = 1
        }

        public enum LightMode : byte
        {
            Realtime = 0,
            Mixed = 1,
            Baked = 2
        }

        public struct Light
        {
            public Vector3 color;
            public Vector3 indirectColor;
            public Quaternion orientation;
            public Vector3 position;
            public float range;
            public int cookieTextureIndex;
            public float cookieScale;
            public float coneAngle;
            public float innerConeAngle;
            public float shape0;
            public float shape1;
            public LightType type;
            public LightMode mode;
            public FalloffType falloff;
            public AngularFalloffType angularFalloff;
            public bool castsShadows;
            public int shadowMaskChannel;
        }

        public struct SampleCount
        {
            public uint directSampleCount;
            public uint indirectSampleCount;
            public uint environmentSampleCount;
        }

        public struct LightingSettings
        {
            public SampleCount lightmapSampleCounts;
            public SampleCount probeSampleCounts;
            public uint minBounces;
            public uint maxBounces;
            public LightmapBakeMode lightmapBakeMode;
            public MixedLightingMode mixedLightingMode;
            public bool aoEnabled;
            public float aoDistance;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class ExternalProcessConnection : IDisposable
        {
            private IntPtr _ptr;
            private readonly bool _ownsPtr;

            public ExternalProcessConnection()
            {
                _ptr = Internal_Create();
                _ownsPtr = true;
            }

            public ExternalProcessConnection(IntPtr ptr)
            {
                _ptr = ptr;
                _ownsPtr = false;
            }

            public bool Connect(int bakePortNumber)
            {
                return Internal_Connect(bakePortNumber);
            }

            ~ExternalProcessConnection()
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
                if (_ownsPtr && _ptr != IntPtr.Zero)
                {
                    Internal_Destroy(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }

            //[NativeMethod(IsThreadSafe = true)]
            static extern void Internal_Destroy(IntPtr ptr);
            //[NativeMethod(IsThreadSafe = true)]
            static extern IntPtr Internal_Create();
            extern bool Internal_Connect(int bakePortNumber);

            internal static class BindingsMarshaller
            {
                public static IntPtr ConvertToNative(ExternalProcessConnection connection) => connection._ptr;
            }
        }

        public class BakeInput : IDisposable
        {
            private IntPtr _ptr;
            private readonly bool _ownsPtr;

            public BakeInput()
            {
                _ptr = Internal_Create();
                _ownsPtr = true;
            }

            public BakeInput(IntPtr ptr)
            {
                _ptr = ptr;
                _ownsPtr = false;
            }

            public void Dispose()
            {
                Destroy();
                GC.SuppressFinalize(this);
            }

            private void Destroy()
            {
                if(_ownsPtr && _ptr != IntPtr.Zero)
                {
                    Internal_Destroy(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }

            static extern IntPtr Internal_Create();
            static extern void Internal_Destroy(IntPtr ptr);

            public extern ulong GetByteSize();

            public extern uint albedoTextureCount { get; }
            extern TextureData Internal_GetAlbedoTextureData(uint index);

            public TextureData GetAlbedoTextureData(uint index)
            {
                if (index >= albedoTextureCount)
                    throw new ArgumentException($"index must be between 0 and {albedoTextureCount - 1}, but was {index}");
                return Internal_GetAlbedoTextureData(index);
            }

            public Texture2D GetAlbedoTexture(uint index)
            {
                if (index >= albedoTextureCount)
                    throw new ArgumentException($"index must be between 0 and {albedoTextureCount - 1}, but was {index}");
                TextureData textureData = Internal_GetAlbedoTextureData(index);
                Texture2D tex = new Texture2D((int)textureData.resolution.width, (int)textureData.resolution.height, TextureFormat.RGBAFloat, false);
                tex.SetPixelData(textureData.data, 0);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                return tex;
            }

            public extern uint emissiveTextureCount { get; }
            extern TextureData Internal_GetEmissiveTextureData(uint index);

            public TextureData GetEmissiveTextureData(uint index)
            {
                if (index >= emissiveTextureCount)
                    throw new ArgumentException($"index must be between 0 and {emissiveTextureCount - 1}, but was {index}");
                return Internal_GetEmissiveTextureData(index);
            }

            public Texture2D GetEmissiveTexture(uint index)
            {
                if (index >= emissiveTextureCount)
                    throw new ArgumentException($"index must be between 0 and {emissiveTextureCount - 1}, but was {index}");
                TextureData textureData = Internal_GetEmissiveTextureData(index);
                Texture2D tex = new Texture2D((int)textureData.resolution.width, (int)textureData.resolution.height, TextureFormat.RGBAFloat, false);
                tex.SetPixelData(textureData.data, 0);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                return tex;
            }

            extern LightingSettings Internal_GetLightingSettings();
            public LightingSettings GetLightingSettings()
            {
                return Internal_GetLightingSettings();
            }

            extern void Internal_SetLightingSettings(LightingSettings lightingSettings);
            public void SetLightingSettings(LightingSettings lightingSettings)
            {
                Internal_SetLightingSettings(lightingSettings);
            }

            public extern uint instanceCount { get; }
            extern Instance Internal_Instance(uint index);
            public Instance instance(uint index)
            {
                if (index >= instanceCount)
                    throw new ArgumentException($"index must be between 0 and {instanceCount - 1}, but was {index}");
                Instance instance = Internal_Instance(index);
                return instance;
            }
            extern void Internal_SetInstance(uint index, Instance instance);
            public void Instance(uint index, Instance instance)
            {
                if (index >= instanceCount)
                    throw new ArgumentException($"index must be between 0 and {instanceCount - 1}, but was {index}");
                Internal_SetInstance(index, instance);
            }

            public extern uint terrainCount { get; }
            extern Terrain Internal_GetTerrain(uint index);
            public Terrain GetTerrain(uint index)
            {
                if (index >= terrainCount)
                    throw new ArgumentException($"index must be between 0 and {terrainCount - 1}, but was {index}");
                Terrain terrain = Internal_GetTerrain(index);
                return terrain;
            }

            public extern Vector2[] GetUV1VertexData(uint meshIndex);

            public extern uint meshCount { get; }
        }
    }
}
#endif