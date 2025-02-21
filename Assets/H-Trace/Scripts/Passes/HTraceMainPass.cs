using System;
using H_Trace.Scripts.Globals;
using H_Trace.Scripts.Structs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

namespace H_Trace.Scripts.Passes
{
    internal class HTraceMainPass : CustomPass
    {   
        private const string HRenderAO_SHADER_NAME = "HRenderAO";
        private const string HTracingScreenSpace_SHADER_NAME = "HTracingScreenSpace";
        private const string HTracingWorldSpace_SHADER_NAME = "HTracingWorldSpace";
        private const string HRadianceCache_SHADER_NAME = "HRadianceCache";
        private const string HTemporalReprojection_SHADER_NAME = "HTemporalReprojection";
        private const string HSpatialPrepass_SHADER_NAME = "HSpatialPrepass";
        private const string HProbeAmbientOcclusion_SHADER_NAME = "HProbeAmbientOcclusion";
        private const string HFilterOctahedral_SHADER_NAME = "HReSTIR";
        private const string HCopy_SHADER_NAME = "HCopy";
        private const string HRayGeneration_SHADER_NAME = "HRayGeneration";
        private const string VoxelVisualization_SHADER_NAME = "VoxelVisualization";
        private const string HReservoirValidation_SHADER_NAME = "HReservoirValidation";
        private const string HDepthPyramid_SHADER_NAME = "HDepthPyramid";
        private const string HDebugPassthrough_SHADER_NAME = "HDebugPassthrough";
        private const string HInterpolation_SHADER_NAME = "HInterpolation";
        private const string HTemporalDenoiser_SHADER_NAME = "HTemporalDenoiser";

        #region PUBLIC PROPERTY SETTINGS -------------------------->

        [SerializeField]
        private GeneralData GeneralData;
        [SerializeField]
        private DebugData DebugData;
        [SerializeField]
        private ScreenSpaceLightingData ScreenSpaceLightingData;
        [SerializeField]
        private VoxelizationData VoxelizationData;

        private VoxelizationRuntimeData VoxelizationRuntimeData;
        
        #endregion

        // Local variables
        private int HashStorageSize = 512000 * 2;
        private int HashUpdateFraction = 10;
        private int HashUpdateFrameIndex = 0;
        private int HFrameIndex = 0;
       
        private Vector3 PrevVoxelCameraPos = new Vector3(0, 0, 0);
        private Vector2Int PrevScreenResolution = new Vector2Int(0, 0);
        private Vector2Int DepthPyramidResolution = new Vector2Int(16, 16);
        private Matrix4x4 DepthMatrixPrev = new Matrix4x4();
        private Vector3 cameraPosistionPrev = new Vector3();

        private int  PersistentHistorySamples = 4;
        private int  _probeSize = 6;
        private int  _octahedralSize = 4;
        private int  _startFrameCounter = 0;
        private bool _firstFrame = true;
        private bool _initialized;
        private bool _prevDirectionalOcclusion;
        private bool _prevDebugModeEnabled;

        
        // Computes
        private ComputeShader VoxelVisualization = null;
        private ComputeShader HReservoirValidation = null;
        private ComputeShader HTracingScreenSpace = null;
        private ComputeShader HTracingWorldSpace = null;
        private ComputeShader HRadianceCache = null;
        private ComputeShader HTemporalReprojection = null;
        private ComputeShader HReSTIR = null;
        private ComputeShader HCopy = null;
        private ComputeShader HSpatialPrepass = null;
        private ComputeShader HProbeAmbientOcclusion = null;
        private ComputeShader HProbeAtlasAccumulation = null;
        private ComputeShader HRayGeneration = null;
        private ComputeShader HRenderAO = null;
        private ComputeShader HDepthPyramid = null;
        private ComputeShader HPrefilterTemporal = null;
        private ComputeShader HPrefilterSpatial = null;
        private ComputeShader HDebugPassthrough = null;
        private ComputeShader HInterpolation = null;
        private ComputeShader HTemporalDenoiser = null;


        // Materials, shaders, compute buffers
        Material MotionVectorsMaterial;
        Material VoxelVisualizationMaterial;
            
        // Indirection dispatch buffers
        ComputeBuffer RayCounter;
        ComputeBuffer RayCounterWS;
        ComputeBuffer IndirectArgumentsSS;
        ComputeBuffer IndirectArgumentsWS;
        ComputeBuffer IndirectArgumentsOV;
        ComputeBuffer IndirectArgumentsSF;
        ComputeBuffer IndirectCoordsSS;
        ComputeBuffer IndirectCoordsWS;
        ComputeBuffer IndirectCoordsOV;
        ComputeBuffer IndirectCoordsSF;
        
        // Spatial offsets buffers
        ComputeBuffer PointDistributionBuffer;
        ComputeBuffer SpatialOffsetsBuffer;
        
        // Hash buffers
        ComputeBuffer HashBuffer_Key;
        ComputeBuffer HashBuffer_Payload;
        ComputeBuffer HashBuffer_Counter;
        ComputeBuffer HashBuffer_Radiance;
        ComputeBuffer HashBuffer_Position;
        
        
        #region RT HADNLES ------------------------------------>
                
        // SSAO RT
        RTHandle ProbeSSAO;
        RTHandle NormalDepthHalf;
        RTHandle BentNormalsAO;
        RTHandle BentNormalsAO_Interpolated;
        RTHandle BentNormalsAO_History;
        RTHandle BentNormalsAO_Accumulated;
        RTHandle BentNormalsAO_Samplecount;
        RTHandle BentNormalsAO_SamplecountHistory;
            
        // TRACING RT
        RTHandle VoxelPayload;
        RTHandle RayDirections;
        RTHandle HitRadiance;
        RTHandle HitDistanceScreenSpace;
        RTHandle HitDistanceWorldSpace;
        RTHandle HitCoordScreenSpace;
        
        // PROBE AO RT
        RTHandle ProbeAmbientOcclusion;
        RTHandle ProbeAmbientOcclusion_History;
        RTHandle ProbeAmbientOcclusion_Filtered;
            
        // GBUFFER RT
        RTHandle CustomCameraMotionVectors;
        RTHandle GeometryNormal;
        RTHandle NormalDepth_History;
        RTHandle ProbeNormalDepth;
        RTHandle ProbeNormalDepth_History;
        RTHandle ProbeWorldPosNormal_History;
        RTHandle ProbeNormalDepth_Intermediate;
        RTHandle ProbeDiffuse;
        
        RTHandle DepthPyramid;
        RTHandle DepthIntermediate_Pyramid;
        
        // REPROJECTION RT
        RTHandle HistoryIndirection;
        RTHandle ReprojectionCoord;
        RTHandle PersistentReprojectionCoord;
        RTHandle ReprojectionWeights;
        RTHandle PersistentReprojectionWeights;
        
        // SPATIAL PREPASS RT
        RTHandle SpatialOffsetsPacked;
        RTHandle SpatialWeightsPacked;
            
        // RESERVOIR RT
        RTHandle ReservoirAtlas;
        RTHandle ReservoirAtlas_History;
        RTHandle ReservoirAtlasRadianceData_A;
        RTHandle ReservoirAtlasRadianceData_B;
        RTHandle ReservoirAtlasRadianceData_C;
        RTHandle ReservoirAtlasRayData_A;
        RTHandle ReservoirAtlasRayData_B;
        RTHandle ReservoirAtlasRayData_C;
            
        // SHADOW GUIDANCE MASK RT
        RTHandle ShadowGuidanceMask;
        RTHandle ShadowGuidanceMask_Accumulated;
        RTHandle ShadowGuidanceMask_Filtered;
        RTHandle ShadowGuidanceMask_History;
        RTHandle ShadowGuidanceMask_CheckerboardHistory;
        RTHandle ShadowGuidanceMask_Samplecount;
        RTHandle ShadowGuidanceMask_SamplecountHistory;
            
        // INTERPOLATION RT
        RTHandle PackedSH_A;
        RTHandle PackedSH_B;
        RTHandle Radiance_Interpolated;
        
        // TEMPORAL DENOISER RT
        RTHandle RadianceAccumulated;
        RTHandle RadianceAccumulated_History;
        RTHandle LuminanceDelta;
        RTHandle LuminanceDelta_History;
        
        // DEBUG RT
        RTHandle VoxelVisualizationRayDirections;
        RTHandle DebugOutput;
        
        RTHandle RadianceCacheFiltered;
        
        #endregion

        
        #region MATERIAL & RESOURCE LOAD --------------------->

            private void ResourcesLoad()
            {
                VoxelVisualization      = HExtensions.LoadComputeShader(VoxelVisualization_SHADER_NAME);
                HReservoirValidation    = HExtensions.LoadComputeShader(HReservoirValidation_SHADER_NAME);
                HTracingScreenSpace     = HExtensions.LoadComputeShader(HTracingScreenSpace_SHADER_NAME);
                HTracingWorldSpace      = HExtensions.LoadComputeShader(HTracingWorldSpace_SHADER_NAME);
                HRadianceCache          = HExtensions.LoadComputeShader(HRadianceCache_SHADER_NAME);
                HTemporalReprojection   = HExtensions.LoadComputeShader(HTemporalReprojection_SHADER_NAME);
                HSpatialPrepass         = HExtensions.LoadComputeShader(HSpatialPrepass_SHADER_NAME);
                HProbeAmbientOcclusion  = HExtensions.LoadComputeShader(HProbeAmbientOcclusion_SHADER_NAME);
                HReSTIR                 = HExtensions.LoadComputeShader(HFilterOctahedral_SHADER_NAME);
                HCopy                   = HExtensions.LoadComputeShader(HCopy_SHADER_NAME);
                HRayGeneration          = HExtensions.LoadComputeShader(HRayGeneration_SHADER_NAME);
                HRenderAO               = HExtensions.LoadComputeShader(HRenderAO_SHADER_NAME);
                HDepthPyramid           = HExtensions.LoadComputeShader(HDepthPyramid_SHADER_NAME);
                HDebugPassthrough       = HExtensions.LoadComputeShader(HDebugPassthrough_SHADER_NAME);
                HInterpolation          = HExtensions.LoadComputeShader(HInterpolation_SHADER_NAME);
                HTemporalDenoiser       = HExtensions.LoadComputeShader(HTemporalDenoiser_SHADER_NAME);
            }
            
            private void MaterialSetup()
            {
                MotionVectorsMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/HDRP/CameraMotionVectors"));
                VoxelVisualizationMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("HTrace/VoxelVisualization"));
            }

        #endregion

        
        #region TEXTURE AND BUFFER ALLOCATION --------------------------->

        private void AllocateMainRT(bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(VoxelPayload);
                HExtensions.HRelease(RayDirections);
                HExtensions.HRelease(HitRadiance);
                HExtensions.HRelease(HitDistanceScreenSpace);
                HExtensions.HRelease(HitDistanceWorldSpace);
                HExtensions.HRelease(HitCoordScreenSpace);
                
                HExtensions.HRelease(ProbeAmbientOcclusion);
                HExtensions.HRelease(ProbeAmbientOcclusion_History);
                HExtensions.HRelease(ProbeAmbientOcclusion_Filtered);
                
                HExtensions.HRelease(CustomCameraMotionVectors);
                HExtensions.HRelease(GeometryNormal);
                HExtensions.HRelease(NormalDepth_History);
                HExtensions.HRelease(ProbeNormalDepth);
                HExtensions.HRelease(ProbeNormalDepth_History);
                HExtensions.HRelease(ProbeWorldPosNormal_History);
                HExtensions.HRelease(ProbeNormalDepth_Intermediate);
                HExtensions.HRelease(ProbeDiffuse);
                
                HExtensions.HRelease(HistoryIndirection);
                HExtensions.HRelease(ReprojectionWeights);
                HExtensions.HRelease(PersistentReprojectionWeights);
                HExtensions.HRelease(ReprojectionCoord);
                HExtensions.HRelease(PersistentReprojectionCoord);
                
                HExtensions.HRelease(SpatialOffsetsPacked);
                HExtensions.HRelease(SpatialWeightsPacked);
                
                HExtensions.HRelease(ReservoirAtlas);
                HExtensions.HRelease(ReservoirAtlas_History);
                HExtensions.HRelease(ReservoirAtlasRadianceData_A);
                HExtensions.HRelease(ReservoirAtlasRadianceData_B);
                HExtensions.HRelease(ReservoirAtlasRadianceData_C);
                HExtensions.HRelease(ReservoirAtlasRayData_A);
                HExtensions.HRelease(ReservoirAtlasRayData_B);
                HExtensions.HRelease(ReservoirAtlasRayData_C);
                
                HExtensions.HRelease(ShadowGuidanceMask);
                HExtensions.HRelease(ShadowGuidanceMask_Accumulated);
                HExtensions.HRelease(ShadowGuidanceMask_Filtered);
                HExtensions.HRelease(ShadowGuidanceMask_History);
                HExtensions.HRelease(ShadowGuidanceMask_CheckerboardHistory);
                HExtensions.HRelease(ShadowGuidanceMask_Samplecount);
                HExtensions.HRelease(ShadowGuidanceMask_SamplecountHistory);
                
                HExtensions.HRelease(PackedSH_A);
                HExtensions.HRelease(PackedSH_B);
                HExtensions.HRelease(Radiance_Interpolated);
                
                HExtensions.HRelease(RadianceAccumulated);
                HExtensions.HRelease(RadianceAccumulated_History);
                HExtensions.HRelease(LuminanceDelta);
                HExtensions.HRelease(LuminanceDelta_History);

                HExtensions.HRelease(RadianceCacheFiltered);
                
                HExtensions.HRelease(RayCounter);
                HExtensions.HRelease(RayCounterWS);
                HExtensions.HRelease(IndirectArgumentsSS);
                HExtensions.HRelease(IndirectArgumentsWS);
                HExtensions.HRelease(IndirectArgumentsOV);
                HExtensions.HRelease(IndirectArgumentsSF);

                HExtensions.HRelease(PointDistributionBuffer);
                HExtensions.HRelease(SpatialOffsetsBuffer);
                
                HExtensions.HRelease(HashBuffer_Key);
                HExtensions.HRelease(HashBuffer_Payload);
                HExtensions.HRelease(HashBuffer_Counter);
                HExtensions.HRelease(HashBuffer_Radiance);
                HExtensions.HRelease(HashBuffer_Position);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();

            Vector2 FullRes       = Vector2.one;
            Vector2 HalfRes       = Vector2.one / 2;
            Vector2 ProbeRes      = Vector2.one / _probeSize;
            Vector2 ProbeAtlasRes = Vector2.one / (float)_probeSize * (float)_octahedralSize;

            // -------------------------------------- BUFFERS -------------------------------------- //
                
            // Indirection dispatch buffers
            RayCounter          = new ComputeBuffer(10 * TextureXR.slices, sizeof(uint));
            RayCounterWS        = new ComputeBuffer(10 * TextureXR.slices, sizeof(uint)); 
            IndirectArgumentsSS = new ComputeBuffer(3 * TextureXR.slices, sizeof(uint), ComputeBufferType.IndirectArguments);
            IndirectArgumentsWS = new ComputeBuffer(3 * TextureXR.slices, sizeof(uint), ComputeBufferType.IndirectArguments);
            IndirectArgumentsOV = new ComputeBuffer(3 * TextureXR.slices, sizeof(uint), ComputeBufferType.IndirectArguments);
            IndirectArgumentsSF = new ComputeBuffer(3 * TextureXR.slices, sizeof(uint), ComputeBufferType.IndirectArguments);
            
            // Spatial offsets buffers
            PointDistributionBuffer = new ComputeBuffer(TextureXR.slices * 32 * 4, 2 * sizeof(float));
            SpatialOffsetsBuffer    = new ComputeBuffer(9 * 9, 2 * sizeof(int));
            
            // Hash buffers
            HashBuffer_Key      = new ComputeBuffer(HashStorageSize, 1 * sizeof(uint));
            HashBuffer_Payload  = new ComputeBuffer(HashStorageSize / HashUpdateFraction, 2 * sizeof(uint)); 
            HashBuffer_Counter  = new ComputeBuffer(HashStorageSize, 1 * sizeof(uint));
            HashBuffer_Radiance = new ComputeBuffer(HashStorageSize, 4 * sizeof(uint)); 
            HashBuffer_Position = new ComputeBuffer(HashStorageSize, 4 * sizeof(uint));
            
            
            // -------------------------------------- TRACING RT -------------------------------------- //
            VoxelPayload = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_VoxelPayload", enableRandomWrite: true);
       
            RayDirections = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "_RayDirections", enableRandomWrite: true);  
            
            HitDistanceScreenSpace = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_UInt, name: "_HitDistanceScreenSpace", enableRandomWrite: true);
            
            HitDistanceWorldSpace = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_HitDistanceWorldSpace", enableRandomWrite: true);

            HitRadiance = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_HitRadiance", enableRandomWrite: true);
            
            HitCoordScreenSpace = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension, useDynamicScale: true,
                colorFormat: GraphicsFormat.R16G16_UInt, name: "_HitCoordScreenSpace", enableRandomWrite: true);
            
            
            // -------------------------------------- PROBE AO RT -------------------------------------- //
            
            ProbeAmbientOcclusion = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_UInt, name: "_ProbeAmbientOcclusion", enableRandomWrite: true);
            
            ProbeAmbientOcclusion_History = RTHandles.Alloc(ProbeRes, TextureXR.slices * PersistentHistorySamples, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_UInt, name: "_ProbeAmbientOcclusion_History", enableRandomWrite: true);
            
            ProbeAmbientOcclusion_Filtered = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ProbeAmbientOcclusion_Filtered", enableRandomWrite: true);

            
            // -------------------------------------- GBUFFER RT -------------------------------------- //
            
            CustomCameraMotionVectors = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16_SFloat, name: "_CustomCameraMotionVectors", enableRandomWrite: true);

            GeometryNormal = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_GeometryNormal", enableRandomWrite: true);

            NormalDepth_History = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_NormalDepth_History", enableRandomWrite: true);

            ProbeNormalDepth = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ProbeNormalDepth", enableRandomWrite: true);
            
            ProbeNormalDepth_History = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ProbeNormalDepth_History", enableRandomWrite: true);
            
            ProbeWorldPosNormal_History = RTHandles.Alloc(ProbeRes, TextureXR.slices * PersistentHistorySamples, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_ProbeWorldPosNormal_History", enableRandomWrite: true);

            ProbeNormalDepth_Intermediate = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ProbeNormalDepth_Intermediate", enableRandomWrite: true);
        
            ProbeDiffuse = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "_ProbeDiffuse", enableRandomWrite: true);

                
            // -------------------------------------- REPROJECTION RT -------------------------------------- //
            
            HistoryIndirection = RTHandles.Alloc(ProbeRes, TextureXR.slices * PersistentHistorySamples, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16_UInt, name: "_HistoryIndirection", enableRandomWrite: true);

            ReprojectionWeights = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "_ReprojectionWeights", enableRandomWrite: true);
                
            PersistentReprojectionWeights = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "_PersistentReprojectionWeights", enableRandomWrite: true);

            ReprojectionCoord = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16_UInt, name: "_ReprojectionCoord", enableRandomWrite: true);

            PersistentReprojectionCoord = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16_UInt, name: "_PersistentReprojectionCoord", enableRandomWrite: true);

            
            // -------------------------------------- SPATIAL PREPASS RT -------------------------------------- //
            
            SpatialOffsetsPacked = RTHandles.Alloc(ProbeRes, TextureXR.slices * 4, dimension: TextureXR.dimension, useDynamicScale: true,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_SpatialOffsetsPacked", enableRandomWrite: true);
            
            SpatialWeightsPacked = RTHandles.Alloc(ProbeRes, TextureXR.slices * 4, dimension: TextureXR.dimension, useDynamicScale: true,
                colorFormat: GraphicsFormat.R16G16B16A16_UInt, name: "_SpatialWeightsPacked", enableRandomWrite: true);


            // -------------------------------------- RESERVOIR RT -------------------------------------- //
            
            ReservoirAtlas = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_ReservoirAtlas", enableRandomWrite: true);
            
            ReservoirAtlas_History = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices * PersistentHistorySamples, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_ReservoirAtlas_History", enableRandomWrite: true);
            
            ReservoirAtlasRadianceData_A = RTHandles.Alloc(ProbeAtlasRes , TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ReservoirAtlasRadianceData_A", enableRandomWrite: true);
            
            ReservoirAtlasRadianceData_B = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ReservoirAtlasRadianceData_B", enableRandomWrite: true);
            
            ReservoirAtlasRadianceData_C = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ReservoirAtlasRadianceData_C", enableRandomWrite: true);
            
            ReservoirAtlasRayData_A = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32_UInt, name: "_ReservoirAtlasRayData_A", enableRandomWrite: true);
            
            ReservoirAtlasRayData_B = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices * PersistentHistorySamples, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32_UInt, name: "_ReservoirAtlasRayData_B", enableRandomWrite: true);
            
            ReservoirAtlasRayData_C = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32_UInt, name: "_ReservoirAtlasRayData_C", enableRandomWrite: true);


            
            // -------------------------------------- SHADOW GUIDANCE MASK RT -------------------------------------- //
            
            ShadowGuidanceMask = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices , dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask", enableRandomWrite: true);
            
            ShadowGuidanceMask_Accumulated = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask_Accumulated", enableRandomWrite: true);
                
            ShadowGuidanceMask_Filtered  = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask_Filtered", enableRandomWrite: true);

            ShadowGuidanceMask_History = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask_History", enableRandomWrite: true);
            
            ShadowGuidanceMask_CheckerboardHistory = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask_CheckerboardHistory", enableRandomWrite: true); 
            
            ShadowGuidanceMask_Samplecount = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_ShadowGuidanceMask_Samplecount", enableRandomWrite: true);
            
            ShadowGuidanceMask_SamplecountHistory = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_ShadowGuidanceMask_SamplecountHistory", enableRandomWrite: true);

            
            // -------------------------------------- INTERPOLATION RT -------------------------------------- //
            
            PackedSH_A = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_PackedSH_A", enableRandomWrite: true);
            
            PackedSH_B = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_PackedSH_B", enableRandomWrite: true);
            
            Radiance_Interpolated = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32_UInt, name: "_Radiance_Interpolated", enableRandomWrite: true); 
            
            
            // -------------------------------------- TEMPORAL DENOISER RT -------------------------------------- //
           
            RadianceAccumulated = RTHandles.Alloc(FullRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_RadianceAccumulated", enableRandomWrite: true);
            
            RadianceAccumulated_History = RTHandles.Alloc(FullRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_RadianceAccumulated_History", enableRandomWrite: true);
            
            LuminanceDelta = RTHandles.Alloc(FullRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_RadianceLumaDelta", enableRandomWrite: true);
            
            LuminanceDelta_History = RTHandles.Alloc(FullRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_RadianceLumaDelta_History", enableRandomWrite: true);
            
            // TODO: figure out if we need this, do not delete and do not allocate for now
            // RadianceCacheFiltered = RTHandles.Alloc(600, 100, 100, dimension: TextureDimension.Tex3D,
            //     colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, name: "_RadianceCacheFiltered", enableRandomWrite: true);
        }

        private void AllocateDepthPyramidRT(Vector2Int PyramidRes, bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(DepthPyramid);
                HExtensions.HRelease(DepthIntermediate_Pyramid);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();

            DepthPyramid = RTHandles.Alloc(PyramidRes.x, PyramidRes.y, TextureXR.slices, dimension: TextureDimension.Tex2DArray, useMipMap: true, autoGenerateMips: false,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_DepthPyramid", enableRandomWrite: true);
            
            DepthIntermediate_Pyramid = RTHandles.Alloc(PyramidRes.x / 16, PyramidRes.y / 16, TextureXR.slices, dimension: TextureDimension.Tex2DArray,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_DepthIntermediate_Pyramid", enableRandomWrite: true);
        }

        private void AllocateSSAO_RT(bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(ProbeSSAO);
                HExtensions.HRelease(NormalDepthHalf);
                HExtensions.HRelease(BentNormalsAO);
                HExtensions.HRelease(BentNormalsAO_Interpolated);
                HExtensions.HRelease(BentNormalsAO_History);
                HExtensions.HRelease(BentNormalsAO_Accumulated);
                HExtensions.HRelease(BentNormalsAO_Samplecount);
                HExtensions.HRelease(BentNormalsAO_SamplecountHistory);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();
            
            Vector2 fullRes = Vector2.one;
            Vector2 halfRes = Vector2.one / 2;
            Vector2 probeSize = Vector2.one / _probeSize;
       
            // -------------------------------------- SSAO RT -------------------------------------- //

            if (ScreenSpaceLightingData.DirectionalOcclusion)
            {
                ProbeSSAO = RTHandles.Alloc(probeSize, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R8_UNorm, name: "_ProbeSSAO", enableRandomWrite: true);
                
                BentNormalsAO = RTHandles.Alloc(fullRes, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO", enableRandomWrite: true);
                
                BentNormalsAO_Interpolated = RTHandles.Alloc(fullRes, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO_Interpolated", enableRandomWrite: true);
                
                NormalDepthHalf = RTHandles.Alloc(fullRes / 2, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_NormalDepthHalf", enableRandomWrite: true);

                // TODO: porbably we will no need these, leave them for now, but don't allocate.
                // BentNormalsAO_History = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                //     colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO_History", enableRandomWrite: true);
                //
                // BentNormalsAO_Accumulated = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                //     colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO_Accumulated", enableRandomWrite: true);
                //
                // BentNormalsAO_SamplecountHistory = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                //     colorFormat: GraphicsFormat.R8_UInt, name: "_BentNormalsAO_SamplecountHistory", enableRandomWrite: true);
                //
                // BentNormalsAO_Samplecount = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                //     colorFormat: GraphicsFormat.R8_UInt, name: "_BentNormalsAO_Samplecount", enableRandomWrite: true);
            }
            else //Warnings suppression
            {
                ProbeSSAO = RTHandles.Alloc(1,1, 1, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R8_UNorm, name: "_ProbeSSAO", enableRandomWrite: true);
                
                BentNormalsAO = RTHandles.Alloc(1,1, 1, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO", enableRandomWrite: true);
                
                BentNormalsAO_Interpolated = RTHandles.Alloc(1,1, 1, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO_Interpolated", enableRandomWrite: true);
                
                NormalDepthHalf = RTHandles.Alloc(1,1, 1, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_NormalDepthHalf", enableRandomWrite: true);
            }
        }

        private void AllocateDebugRT(bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(VoxelVisualizationRayDirections);
                HExtensions.HRelease(DebugOutput);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();
            
            Vector2 fullRes = Vector2.one;
            
            // -------------------------------------- DEBUG RT -------------------------------------- //

            if (GeneralData.DebugModeWS != DebugModeWS.None)
            {
                VoxelVisualizationRayDirections = RTHandles.Alloc(fullRes, TextureXR.slices, dimension: TextureXR.dimension, useDynamicScale: true,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_VoxelVisualizationRayDirections", enableRandomWrite: true);
            
                DebugOutput = RTHandles.Alloc(fullRes,  TextureXR.slices, dimension: TextureDimension.Tex2DArray,
                    colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, name: "_DebugOutput", enableRandomWrite: true);
            }
        }

        private void AllocateIndirectionBuffers(Vector2Int resolution, bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(IndirectCoordsSS);
                HExtensions.HRelease(IndirectCoordsWS);
                HExtensions.HRelease(IndirectCoordsOV);
                HExtensions.HRelease(IndirectCoordsSF);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();
            
            // -------------------------------------- BUFFERS -------------------------------------- //

            int resolutionMul = resolution.x * resolution.y;
            IndirectCoordsSS = new ComputeBuffer(resolutionMul * TextureXR.slices, 2 * sizeof(uint));
            IndirectCoordsWS = new ComputeBuffer(resolutionMul * TextureXR.slices, 2 * sizeof(uint));
            IndirectCoordsOV = new ComputeBuffer(resolutionMul * TextureXR.slices, 2 * sizeof(uint));
            IndirectCoordsSF = new ComputeBuffer(resolutionMul * TextureXR.slices, 2 * sizeof(uint));
        }

        private void ReallocHashBuffers(bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(HashBuffer_Key);
                HExtensions.HRelease(HashBuffer_Payload);
                HExtensions.HRelease(HashBuffer_Counter);
                HExtensions.HRelease(HashBuffer_Radiance);
                HExtensions.HRelease(HashBuffer_Position);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();

            HashBuffer_Key      = new ComputeBuffer(HashStorageSize, 1 * sizeof(uint));
            HashBuffer_Payload  = new ComputeBuffer(HashStorageSize / HashUpdateFraction, 2 * sizeof(uint));
            HashBuffer_Counter  = new ComputeBuffer(HashStorageSize, 1 * sizeof(uint));
            HashBuffer_Radiance = new ComputeBuffer(HashStorageSize, 4 * sizeof(uint));
            HashBuffer_Position = new ComputeBuffer(HashStorageSize, 4 * sizeof(uint));
        }

        #endregion
    
        protected internal void Initialize( DebugData debugData,
                                            GeneralData generalData, 
                                            VoxelizationData voxelizationData,
                                            ScreenSpaceLightingData screenSpaceLightingData,
                                            VoxelizationRuntimeData voxelizationRuntimeData)
        {
            enabled = true;

            DebugData = debugData;
            GeneralData = generalData;
            VoxelizationData = voxelizationData;
            ScreenSpaceLightingData = screenSpaceLightingData;
            VoxelizationRuntimeData = voxelizationRuntimeData;
            _probeSize = GeneralData.RayCountMode.ParseToProbeSize();

            VoxelizationRuntimeData.OnReallocTextures += () => ReallocHashBuffers();
            GeneralData.OnRayCountChanged += RayCountChanged;

            _initialized = true;
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            name = HTraceNames.HTRACE_MAIN_PASS_NAME_FRAME_DEBUG;
            ResourcesLoad();
            MaterialSetup();
            AllocTextures();
            _firstFrame = true;
        }

        private void RayCountChanged(RayCountMode rayCountMode)
        {
            _probeSize = rayCountMode.ParseToProbeSize();
            AllocTextures();
        }

        private void AllocTextures()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false) // if unity_editor we need only 1 eye render
                TextureXR.maxViews = 1;          
#endif

            AllocateMainRT();
            AllocateSSAO_RT();
            ReallocHashBuffers();
            AllocateDebugRT();
        }

        Vector2Int CalculateDepthPyramidResolution(Vector2Int screenResolution, int lowestMipLevel)
        {
            int lowestMipScale = (int)Mathf.Pow(2.0f, lowestMipLevel);
            Vector2Int lowestMipResolutiom = new Vector2Int(Mathf.CeilToInt( (float)screenResolution.x / (float)lowestMipScale), 
                                                            Mathf.CeilToInt( (float)screenResolution.y / (float)lowestMipScale));

            Vector2Int paddedDepthPyramidResolution = lowestMipResolutiom * lowestMipScale;
            return paddedDepthPyramidResolution; 
        }
        
        Matrix4x4 ComputeFrustumCorners(Camera cam)
        {
            Transform cameraTransform = cam.transform;
            
            Vector3[] frustumCorners = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1 / cam.rect.xMax, 1 / cam.rect.yMax), cam.farClipPlane, cam.stereoActiveEye, frustumCorners); 
                
            Vector3 bottomLeft = cameraTransform.TransformVector(frustumCorners[1]);
            Vector3 topLeft = cameraTransform.TransformVector(frustumCorners[0]);
            Vector3 bottomRight = cameraTransform.TransformVector(frustumCorners[2]); 

            Matrix4x4 frustumVectorsArray = Matrix4x4.identity;
            frustumVectorsArray.SetRow(0, bottomLeft);
            frustumVectorsArray.SetRow(1, bottomLeft + (bottomRight - bottomLeft) * 2);
            frustumVectorsArray.SetRow(2, bottomLeft + (topLeft - bottomLeft) * 2);

            return frustumVectorsArray;
            
        }
        

        protected override void Execute(CustomPassContext ctx)
        {
            if (_initialized == false)
                return;

#if UNITY_EDITOR
            if (HExtensions.PipelineSupportsSSGI == false)
                return;
#endif

            Texture VoxelData = Shader.GetGlobalTexture("_VoxelPositionPyramid");
            if (VoxelData == null || VoxelData.width != VoxelizationData.ExactData.Resolution.x)
                return;
            
            Texture ShadowmapData = Shader.GetGlobalTexture("_HTraceShadowmap");
            if (ShadowmapData == null || ShadowmapData.width != 2048)
                return;
            
            bool UseAPVMultibounce = DebugData.TestCheckbox;
            
            var cmdList = ctx.cmd;
            var camera = ctx.hdCamera.camera;

            cmdList.SetGlobalInt("_TestCheckbox", DebugData.TestCheckbox ? 1 : 0);
            
            HFrameIndex = HFrameIndex > 15 ? 0 : HFrameIndex;
            HashUpdateFrameIndex = HashUpdateFrameIndex > HashUpdateFraction ? 0 : HashUpdateFrameIndex;
            
            cmdList.SetGlobalInt("_ProbeSize", _probeSize);
            cmdList.SetGlobalInt("_OctahedralSize", _octahedralSize);
            cmdList.SetGlobalInt("_HFrameIndex", HFrameIndex);
            cmdList.SetGlobalInt("_ReprojectSkippedFrame", Time.frameCount % 8 == 0 ? 1 : 0);
            cmdList.SetGlobalInt("_PersistentHistorySamples", PersistentHistorySamples);
            
            // Constants set
            cmdList.SetGlobalFloat("_SkyOcclusionCone", HConfig.SkyOcclusionCone);
            cmdList.SetGlobalFloat("_DirectionalLightIntensity", HConfig.DirectionalLightIntensity);
            cmdList.SetGlobalFloat("_SurfaceDiffuseIntensity", HConfig.SurfaceDiffuseIntensity);
            cmdList.SetGlobalFloat("_SkyLightIntensity", HConfig.SkyLightIntensity);
            
            //cmdList.SetGlobalInt("_TestCheckbox", DebugData.TestCheckbox == true ? 1 : 0);

            //todo: stuggered uncomment
            //if (VoxelizationData.VoxelizationUpdateMode != VoxelizationUpdateMode.Staggered)
                cmdList.SetGlobalInt("_ReprojectSkippedFrame", 0);
             
            int screenResX = ctx.hdCamera.actualWidth;
            int screenResY = ctx.hdCamera.actualHeight;
            if (screenResX != PrevScreenResolution.x || screenResY != PrevScreenResolution.y)
            { 
                DepthPyramidResolution = CalculateDepthPyramidResolution(new Vector2Int(screenResX, screenResY), 7);
                AllocateDepthPyramidRT(DepthPyramidResolution);
                AllocateIndirectionBuffers(new Vector2Int(screenResX, screenResY));
            }
            
            PrevScreenResolution = new Vector2Int(screenResX, screenResY);

            // Calculate Resolution for Compute Shaders
            Vector2Int runningRes = new Vector2Int(screenResX, screenResY);
            Vector2 probeAtlasRes = runningRes * Vector2.one / _probeSize * _octahedralSize;
            
            //Dispatch resolutions
            int fullResX_8  = Mathf.CeilToInt((float)runningRes.x / 8);
            int fullResY_8  = Mathf.CeilToInt((float)runningRes.y / 8);
            int probeResX_8 = Mathf.CeilToInt(((float)runningRes.x / (float)_probeSize / 8.0f));
            int probeResY_8 = Mathf.CeilToInt(((float)runningRes.y / (float)_probeSize / 8.0f));
            int probeAtlasResX_8 = Mathf.CeilToInt((Mathf.CeilToInt((float)runningRes.x / (float)_probeSize) * (float)_octahedralSize) / 8);
            int probeAtlasResY_8 = Mathf.CeilToInt((Mathf.CeilToInt((float)runningRes.y / (float)_probeSize) * (float)_octahedralSize) / 8);
            
            bool useDirectionalOcclusion = ScreenSpaceLightingData.DirectionalOcclusion && ScreenSpaceLightingData.OcclusionIntensity > Single.Epsilon;

            if (_prevDirectionalOcclusion != useDirectionalOcclusion)
                AllocateSSAO_RT(!useDirectionalOcclusion); //release when disable occlusion
            _prevDirectionalOcclusion = useDirectionalOcclusion;

            if (_prevDebugModeEnabled == (GeneralData.DebugModeWS == DebugModeWS.None))
                AllocateDebugRT(GeneralData.DebugModeWS == DebugModeWS.None); //release when disable debug
            _prevDebugModeEnabled = GeneralData.DebugModeWS != DebugModeWS.None;
            
            bool DiffuseBufferUnavailable = false;
            if (HExtensions.HdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly
                || ctx.hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward)
                DiffuseBufferUnavailable = true;

            ctx.cmd.SetGlobalBuffer("_HashBuffer_Key", HashBuffer_Key);
            ctx.cmd.SetGlobalBuffer("_HashBuffer_Payload", HashBuffer_Payload);
            ctx.cmd.SetGlobalBuffer("_HashBuffer_Counter", HashBuffer_Counter);
            ctx.cmd.SetGlobalBuffer("_HashBuffer_Radiance", HashBuffer_Radiance);
            ctx.cmd.SetGlobalBuffer("_HashBuffer_Position", HashBuffer_Position);
            
            ctx.cmd.SetGlobalInt("_HashStorageSize", HashStorageSize);
            ctx.cmd.SetGlobalInt("_HashUpdateFraction", HashUpdateFraction);
            
            cmdList.SetGlobalTexture("_GeometryNormal", GeometryNormal);
            cmdList.SetGlobalTexture("_HTraceBufferGI", RadianceAccumulated);
            
            if (camera.cameraType == CameraType.Reflection)
                return;
            
            if (_startFrameCounter < 2) { _startFrameCounter++; return; }
            
            // ---------------------------------------- Compose Motion Vectors ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Compose Motion Vectors")))
            {
                MotionVectorsMaterial.SetInt("_StencilRef", 32);
                MotionVectorsMaterial.SetInt("_StencilMask", 32);
                CoreUtils.SetRenderTarget(cmdList, CustomCameraMotionVectors, ClearFlag.Color);
                if (ctx.cameraDepthBuffer.rt.volumeDepth == TextureXR.slices)
                    cmdList.CopyTexture(ctx.cameraMotionVectorsBuffer, CustomCameraMotionVectors);
                CoreUtils.DrawFullScreen(ctx.cmd, MotionVectorsMaterial, CustomCameraMotionVectors, ctx.cameraDepthBuffer, shaderPassId: 0, properties: ctx.propertyBlock);
                
                cmdList.SetGlobalTexture("_CustomMotionVectors", CustomCameraMotionVectors);
            }
            
      
            // ---------------------------------------- DEPTH PYRAMID GENERATION ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Depth Pyramid Generation")))
            {
                int depthPyramidResX = DepthPyramidResolution.x / 16;
                int depthPyramidResY = DepthPyramidResolution.y / 16;  
                
                // Generate 0-4 mip levels
                int generateDepthPyramid_1_Kernel = HDepthPyramid.FindKernel("GenerateDepthPyramid_1");
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, "_DepthPyramid_OutputMIP0", DepthPyramid, 0); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, "_DepthPyramid_OutputMIP1", DepthPyramid, 1); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, "_DepthPyramid_OutputMIP2", DepthPyramid, 2); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, "_DepthPyramid_OutputMIP3", DepthPyramid, 3); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, "_DepthPyramid_OutputMIP4", DepthPyramid, 4);
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, "_DepthIntermediate_Output", DepthIntermediate_Pyramid);
                cmdList.DispatchCompute(HDepthPyramid, generateDepthPyramid_1_Kernel, depthPyramidResX, depthPyramidResY, TextureXR.slices);
                
                // Generate 5-7 mip levels
                int generateDepthPyramid_2_Kernel = HDepthPyramid.FindKernel("GenerateDepthPyramid_2");
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, "_DepthIntermediate", DepthIntermediate_Pyramid); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, "_DepthPyramid_OutputMIP5", DepthPyramid, 5); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, "_DepthPyramid_OutputMIP6", DepthPyramid, 6); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, "_DepthPyramid_OutputMIP7", DepthPyramid, 7); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, "_DepthPyramid_OutputMIP8", DepthPyramid, 8); 
                cmdList.DispatchCompute(HDepthPyramid, generateDepthPyramid_2_Kernel, depthPyramidResX / 8, depthPyramidResY / 8, TextureXR.slices);
            }

            using (new ProfilingScope(cmdList, new ProfilingSampler("Render Ambient Occlsuion")))
            {
                int computeResX_GI = (runningRes.x / 2  + 8 - 1) / 8;
                int computeResY_GI = (runningRes.y / 2  + 8 - 1) / 8;
                
                if (useDirectionalOcclusion)
                {
                    HInterpolation.EnableKeyword("USE_DIRECTIONAL_OCCLUSION");
                    HSpatialPrepass.EnableKeyword("USE_DIRECTIONAL_OCCLUSION");
                    
                    int horizonTracing_Kernel = HRenderAO.FindKernel("HorizonTracing");
                    cmdList.SetComputeTextureParam(HRenderAO, horizonTracing_Kernel, "_DepthPyramid", DepthPyramid);
                    cmdList.SetComputeTextureParam(HRenderAO, horizonTracing_Kernel, "_BentNormalAmbientOcclusion_Output", BentNormalsAO);
                    cmdList.SetComputeTextureParam(HRenderAO, horizonTracing_Kernel, "_NormalDepthHalf_Output", NormalDepthHalf);
                    cmdList.SetComputeFloatParam(HRenderAO, "_Camera_FOV", camera.fieldOfView);
                    cmdList.DispatchCompute(HRenderAO, horizonTracing_Kernel, computeResX_GI, computeResY_GI, TextureXR.slices);
                    
                    int occlusionInterpolation_Kernel = HRenderAO.FindKernel("OcclusionInterpolation");
                    cmdList.SetComputeTextureParam(HRenderAO, occlusionInterpolation_Kernel, "_AmbientOcclusion", BentNormalsAO);
                    cmdList.SetComputeTextureParam(HRenderAO, occlusionInterpolation_Kernel, "_NormalDepthHalf", NormalDepthHalf);
                    cmdList.SetComputeTextureParam(HRenderAO, occlusionInterpolation_Kernel, "_BentNormalAO_Output", BentNormalsAO_Interpolated);
                    cmdList.SetComputeTextureParam(HRenderAO, occlusionInterpolation_Kernel, "_GeometryNormal_Output", GeometryNormal);
                    cmdList.DispatchCompute(HRenderAO, occlusionInterpolation_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
                    
                    // int OcclusionAccumulationKernel = HRenderAO.FindKernel("OcclusionAccumulation");
                    // cmdList.SetComputeTextureParam(HRenderAO, OcclusionAccumulationKernel, "_AmbientOcclusion", BentNormalsAO_Interpolated);
                    // cmdList.SetComputeTextureParam(HRenderAO, OcclusionAccumulationKernel, "_AmbientOcclusion_Output", AmbientOcclusion);
                    // cmdList.SetComputeTextureParam(HRenderAO, OcclusionAccumulationKernel, "_AmbientOcclusion_History", BentNormalsAO_History);
                    // cmdList.SetComputeTextureParam(HRenderAO, OcclusionAccumulationKernel, "_BentNormalAO_Output", BentNormalsAO_Accumulated);
                    // cmdList.SetComputeTextureParam(HRenderAO, OcclusionAccumulationKernel, "_NormalDepth_History", NormalDepth_History);
                    // cmdList.SetComputeTextureParam(HRenderAO, OcclusionAccumulationKernel, "_SampleCount_Output", BentNormalsAO_Samplecount);
                    // cmdList.SetComputeTextureParam(HRenderAO, OcclusionAccumulationKernel, "_SampleCount_History", BentNormalsAO_SamplecountHistory);
                    // cmdList.DispatchCompute(HRenderAO, OcclusionAccumulationKernel, computeResX_8, computeResY_8, TextureXR.slices);
 
                    // ctx.cmd.CopyTexture(BentNormalsAO_Samplecount, BentNormalsAO_SamplecountHistory);
                    // ctx.cmd.CopyTexture(BentNormalsAO_Accumulated, BentNormalsAO_History);
                }
                else
                {
                    HInterpolation.DisableKeyword("USE_DIRECTIONAL_OCCLUSION");
                    HSpatialPrepass.DisableKeyword("USE_DIRECTIONAL_OCCLUSION");
                }
            }
 
            // ---------------------------------------- PROBE GBUFFER DOWNSAMPLING ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Probe GBuffer Downsampling")))
            {
                // Fill sample offsets for disk filters
                int pointDistributionFill_Kernel = HSpatialPrepass.FindKernel("PointDistributionFill");
                cmdList.SetComputeBufferParam(HSpatialPrepass, pointDistributionFill_Kernel, "_PointDistribution_Output", PointDistributionBuffer);
                cmdList.DispatchCompute(HSpatialPrepass, pointDistributionFill_Kernel, 1, 1, 1);
                
                // Fill 4x4 spatial offset buffer
                int spatialOffsetsBufferFill_Kernel = HSpatialPrepass.FindKernel("SpatialOffsetsBufferFill");
                cmdList.SetComputeBufferParam(HSpatialPrepass, spatialOffsetsBufferFill_Kernel, "_SpatialOffsetsBuffer_Output", SpatialOffsetsBuffer);
                cmdList.DispatchCompute(HSpatialPrepass, spatialOffsetsBufferFill_Kernel, 1, 1, 1);
                
                // Calculate geometry normals 
                if (!useDirectionalOcclusion)
                {
                    int geometryNormals_Kernel = HSpatialPrepass.FindKernel("GeometryNormals");
                    cmdList.SetComputeTextureParam(HSpatialPrepass, geometryNormals_Kernel, "_GeometryNormal_Output", GeometryNormal);
                    cmdList.DispatchCompute(HSpatialPrepass, geometryNormals_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
                }
                
                if (DiffuseBufferUnavailable) HSpatialPrepass.EnableKeyword("DIFFUSE_BUFFER_UNAVAILABLE");
                if (!DiffuseBufferUnavailable) HSpatialPrepass.DisableKeyword("DIFFUSE_BUFFER_UNAVAILABLE");

                // Downsample depth, normal, diffuse and ambient occlusion 
                int gBufferDownsample_Kernel = HSpatialPrepass.FindKernel("GBufferDownsample");
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, "_GeometryNormal", GeometryNormal);
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, "_ProbeNormalDepth_Output", ProbeNormalDepth_Intermediate);
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, "_ProbeDiffuse_Output", ProbeDiffuse); 
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, "_SSAO", BentNormalsAO_Interpolated);
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, "_ProbeSSAO_Output", ProbeSSAO);
                cmdList.DispatchCompute(HSpatialPrepass, gBufferDownsample_Kernel, probeResX_8, probeResY_8, TextureXR.slices);

                // Smooth geometry normals
                int geometryNormalsSmoothing_Kernel = HSpatialPrepass.FindKernel("GeometryNormalsSmoothing");
                cmdList.SetComputeTextureParam(HSpatialPrepass, geometryNormalsSmoothing_Kernel, "_ProbeNormalDepth", ProbeNormalDepth_Intermediate);
                cmdList.SetComputeTextureParam(HSpatialPrepass, geometryNormalsSmoothing_Kernel, "_ProbeNormalDepth_Output", ProbeNormalDepth);
                cmdList.DispatchCompute(HSpatialPrepass, geometryNormalsSmoothing_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
            }
            
            
            // ---------------------------------------- PROBE TEMPORAL REPROJECTION ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Probe Temporal Reprojection")))
            {
                int probeReprojection_Kernel = HTemporalReprojection.FindKernel("ProbeReprojection");
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, "_HistoryIndirection", HistoryIndirection);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, "_ProbeWorldPosNormal_History", ProbeWorldPosNormal_History);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, "_ReprojectionCoords_Output", ReprojectionCoord);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, "_ReprojectionWeights_Output", ReprojectionWeights);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, "_PersistentReprojectionWeights_Output", PersistentReprojectionWeights);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, "_PersistentReprojectionCoord_Output", PersistentReprojectionCoord);
                cmdList.DispatchCompute(HTemporalReprojection, probeReprojection_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
            }
            
               
            // ---------------------------------------- RAY GENERATION ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Ray Generation")))
            {
                // Generate ray directions and compute lists of indirectly dispatched threads
                int rayGeneration_Kernel = HRayGeneration.FindKernel("RayGeneration");
                cmdList.SetComputeTextureParam(HRayGeneration, rayGeneration_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HRayGeneration, rayGeneration_Kernel, "_ReprojectionCoords", ReprojectionCoord);
                cmdList.SetComputeTextureParam(HRayGeneration, rayGeneration_Kernel, "_RayDirectionsJittered_Output", RayDirections);
                cmdList.SetComputeBufferParam(HRayGeneration, rayGeneration_Kernel, "_IndirectCoordsSS_Output", IndirectCoordsSS);
                cmdList.SetComputeBufferParam(HRayGeneration, rayGeneration_Kernel, "_IndirectCoordsOV_Output", IndirectCoordsOV);
                cmdList.SetComputeBufferParam(HRayGeneration, rayGeneration_Kernel, "_IndirectCoordsSF_Output", IndirectCoordsSF);
                cmdList.SetComputeBufferParam(HRayGeneration, rayGeneration_Kernel, "_RayCounter_Output", RayCounter);
                cmdList.DispatchCompute(HRayGeneration, rayGeneration_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
                
                // Prepare arguments for screen space indirect dispatch
                int indirectArguments_Kernel = HRayGeneration.FindKernel("IndirectArguments");
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_RayCounter", RayCounter);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_TracingCoords", IndirectCoordsSS);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_IndirectArguments_Output", IndirectArgumentsSS);
                cmdList.SetComputeIntParam(HRayGeneration, "_RayCounterIndex", 0);
                cmdList.DispatchCompute(HRayGeneration, indirectArguments_Kernel, 1, 1, TextureXR.slices);
                
                // Prepare arguments for occlusion validation indirect dispatch
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_RayCounter", RayCounter);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_TracingCoords", IndirectCoordsOV);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_IndirectArguments_Output", IndirectArgumentsOV);
                cmdList.SetComputeIntParam(HRayGeneration, "_RayCounterIndex", 1);
                cmdList.DispatchCompute(HRayGeneration, indirectArguments_Kernel, 1, 1, TextureXR.slices);
                
                // Prepare arguments for spatial filter indirect dispatch
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_RayCounter", RayCounter);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_TracingCoords", IndirectCoordsSF);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, "_IndirectArguments_Output", IndirectArgumentsSF);
                cmdList.SetComputeIntParam(HRayGeneration, "_RayCounterIndex", 2);
                cmdList.DispatchCompute(HRayGeneration, indirectArguments_Kernel, 1, 1, TextureXR.slices);
            }
            
            
            // ---------------------------------------- CLEAR CHECKERBOARD TARGETS ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Clear Targets")))
            {
                // Clear hit targets
                CoreUtils.SetRenderTarget(ctx.cmd, HitDistanceScreenSpace, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                CoreUtils.SetRenderTarget(ctx.cmd, HitDistanceWorldSpace, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                CoreUtils.SetRenderTarget(ctx.cmd, HitCoordScreenSpace, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                CoreUtils.SetRenderTarget(ctx.cmd, HitRadiance, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                
                // Clear voxel payload targets
                CoreUtils.SetRenderTarget(ctx.cmd, VoxelPayload, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
            }

            if (VoxelizationRuntimeData.VoxelizationModeChanged == true) return;
            
            // ---------------------------------------- SCREEN SPACE LIGHTING ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Screen Space Lighting")))
            {   
                var color_History = ctx.hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                
                if (ScreenSpaceLightingData.EvaluateHitLighting && !DiffuseBufferUnavailable) HTracingScreenSpace.EnableKeyword("HIT_SCREEN_SPACE_LIGHTING");
                if (!ScreenSpaceLightingData.EvaluateHitLighting || DiffuseBufferUnavailable) HTracingScreenSpace.DisableKeyword("HIT_SCREEN_SPACE_LIGHTING");

                // Trace screen-space rays
                int tracingSS_Kernel = HTracingScreenSpace.FindKernel("ScreenSpaceTracing");   
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_ColorPyramid_History", color_History);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_NormalDepth_History", NormalDepth_History);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_DepthPyramid", DepthPyramid);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_GeometryNormal", GeometryNormal);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_RayDirection", RayDirections);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_HitRadiance_Output", HitRadiance);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_HitDistance_Output", HitDistanceScreenSpace);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, "_HitCoord_Output", HitCoordScreenSpace);
                cmdList.SetComputeBufferParam(HTracingScreenSpace, tracingSS_Kernel, "_RayCounter", RayCounter);
                cmdList.SetComputeBufferParam(HTracingScreenSpace, tracingSS_Kernel, "_TracingCoords", IndirectCoordsSS);
                cmdList.SetComputeIntParam(HTracingScreenSpace, "_IndexXR", 0);
                cmdList.DispatchCompute(HTracingScreenSpace, tracingSS_Kernel, IndirectArgumentsSS, 0);
                if (TextureXR.slices > 1)
                {
                    cmdList.SetComputeIntParam(HTracingScreenSpace, "_IndexXR", 1);
                    cmdList.DispatchCompute(HTracingScreenSpace, tracingSS_Kernel, IndirectArgumentsSS, sizeof(uint) * 3);  
                }

                
                // Evaluate screen-space hit it requested 
                if (ScreenSpaceLightingData.EvaluateHitLighting && !DiffuseBufferUnavailable)
                {
                    int lightEvaluationSS_Kernel = HTracingScreenSpace.FindKernel("LightEvaluation");
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, "_ColorPyramid_History", color_History);
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, "_Radiance_History", RadianceAccumulated);
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, "_GeometryNormal", GeometryNormal);
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, "_HitCoord", HitCoordScreenSpace);
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, "_HitRadiance_Output", HitRadiance);
                    cmdList.SetComputeBufferParam(HTracingScreenSpace, lightEvaluationSS_Kernel, "_RayCounter", RayCounter);
                    cmdList.SetComputeBufferParam(HTracingScreenSpace, lightEvaluationSS_Kernel, "_TracingCoords", IndirectCoordsSS);
                    cmdList.SetComputeIntParam(HTracingScreenSpace, "_IndexXR", 0);
                    cmdList.DispatchCompute(HTracingScreenSpace, lightEvaluationSS_Kernel, IndirectArgumentsSS, 0);
                    if (TextureXR.slices > 1)
                    {
                        cmdList.SetComputeIntParam(HTracingScreenSpace, "_IndexXR", 1);
                        cmdList.DispatchCompute(HTracingScreenSpace, lightEvaluationSS_Kernel, IndirectArgumentsSS, sizeof(uint) * 3);  
                    }
                }
            }
            
            // ---------------------------------------- RAY COMPACTION ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Ray Compaction")))
            {
                // Compact rays
                int rayCompactionKernel = HRayGeneration.FindKernel("RayCompaction");
                cmdList.SetComputeTextureParam(HRayGeneration, rayCompactionKernel, "_HitDistance", HitDistanceScreenSpace);
                cmdList.SetComputeTextureParam(HRayGeneration, rayCompactionKernel, "_HitDistance_Output", HitDistanceWorldSpace);
                cmdList.SetComputeBufferParam(HRayGeneration, rayCompactionKernel, "_RayCounter", RayCounter);
                cmdList.SetComputeBufferParam(HRayGeneration, rayCompactionKernel, "_TracingCoords", IndirectCoordsSS);
                cmdList.SetComputeBufferParam(HRayGeneration, rayCompactionKernel, "_TracingRayCounter_Output", RayCounterWS);
                cmdList.SetComputeBufferParam(HRayGeneration, rayCompactionKernel, "_TracingCoords_Output", IndirectCoordsWS);
                cmdList.SetComputeIntParam(HRayGeneration, "_IndexXR", 0);
                cmdList.DispatchCompute(HRayGeneration, rayCompactionKernel, IndirectArgumentsSS, 0);
                if (TextureXR.slices > 1)
                {
                    cmdList.SetComputeIntParam(HRayGeneration, "_IndexXR", 1);
                    cmdList.DispatchCompute(HRayGeneration, rayCompactionKernel, IndirectArgumentsSS, sizeof(uint) * 3);
                }

                // Prepare indirect arguments for world space lighting
                int indirectArgumentsKernel = HRayGeneration.FindKernel("IndirectArguments");
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArgumentsKernel, "_RayCounter", RayCounterWS);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArgumentsKernel, "_TracingCoords", IndirectCoordsWS);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArgumentsKernel, "_IndirectArguments_Output", IndirectArgumentsWS); 
                cmdList.SetComputeIntParam(HRayGeneration, "_RayCounterIndex", 0);
                cmdList.DispatchCompute(HRayGeneration, indirectArgumentsKernel, 1, 1, TextureXR.slices);
            }
            
            // TDR timeout protection
            if (_firstFrame == true) { _firstFrame = false; return; }
            
            // ---------------------------------------- WORLD SPACE LIGHTING ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("World Space Lighting")))
            {
                if (GeneralData.Multibounce == Multibounce.None) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_CACHE"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_APV"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_OFF"); }
                if (GeneralData.Multibounce == Multibounce.IrradianceCache) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_OFF"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_APV"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_CACHE"); }
                if (GeneralData.Multibounce == Multibounce.AdaptiveProbeVolumes) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_CACHE"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_OFF"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_APV"); }

                // Trace world-space rays
                using (new ProfilingScope(cmdList, new ProfilingSampler("World Space Tracing"))) 
                {
                    int wsTracingKernel = HTracingWorldSpace.FindKernel("WorldSpaceTracing");
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, "_DepthPyramid", DepthPyramid);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, "_HitDistance", HitDistanceScreenSpace);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, "_ProbeNormalDepth", ProbeNormalDepth);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, "_GeometryNormal", GeometryNormal);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, "_RayDirection", RayDirections);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, "_HitDistance_Output", HitDistanceWorldSpace);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, "_VoxelPayload_Output", VoxelPayload);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, "_HitRadiance_Output", HitRadiance);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, wsTracingKernel, "_PointDistribution", PointDistributionBuffer);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, wsTracingKernel, "_TracingCoords", IndirectCoordsWS);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, wsTracingKernel, "_RayCounter", RayCounterWS);
                    cmdList.SetComputeFloatParam(HTracingWorldSpace, "_RayLength", GeneralData.RayLength);
                    cmdList.SetComputeIntParam(HTracingWorldSpace, "_IndexXR", 0);
                    cmdList.DispatchCompute(HTracingWorldSpace, wsTracingKernel, IndirectArgumentsWS, 0);
                    if (TextureXR.slices > 1)
                    {
                        cmdList.SetComputeIntParam(HTracingWorldSpace, "_IndexXR", 1);
                        cmdList.DispatchCompute(HTracingWorldSpace, wsTracingKernel, IndirectArgumentsWS, sizeof(uint) * 3);
                    }
                }
                
                // Evaluate world-space lighting
                using (new ProfilingScope(cmdList, new ProfilingSampler("Light Evaluation")))
                {
                    int lightEvaluation_Kernel = HTracingWorldSpace.FindKernel("LightEvaluation");
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, lightEvaluation_Kernel, "_VoxelPayload", VoxelPayload);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, lightEvaluation_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, lightEvaluation_Kernel, "_HitRadiance_Output", HitRadiance);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, lightEvaluation_Kernel, "_TracingCoords", IndirectCoordsWS);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, lightEvaluation_Kernel, "_RayCounter", RayCounterWS);
                    cmdList.SetComputeIntParam(HTracingWorldSpace, "_IndexXR", 0);
                    cmdList.DispatchCompute(HTracingWorldSpace, lightEvaluation_Kernel, IndirectArgumentsWS, 0);
                    if (TextureXR.slices > 1)
                    {
                        cmdList.SetComputeIntParam(HTracingWorldSpace, "_IndexXR", 1);
                        cmdList.DispatchCompute(HTracingWorldSpace, lightEvaluation_Kernel, IndirectArgumentsWS, sizeof(uint) * 3);
                    }
                }
            } 
            
      
            // ---------------------------------------- RADIANCE CACHING ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Radiance Caching")))
            {
                if (GeneralData.Multibounce == Multibounce.IrradianceCache)
                {
                    HRadianceCache.SetInt("_HashUpdateFrameIndex", HashUpdateFrameIndex);

                    // Cache tracing update
                    using (new ProfilingScope(cmdList, new ProfilingSampler("Cache Tracing Update")))
                    {
                        int cacheTracingUpdate_Kernel = HRadianceCache.FindKernel("CacheTracingUpdate");
                        cmdList.SetComputeFloatParam(HRadianceCache, "_RayLength", GeneralData.RayLength);
                        cmdList.DispatchCompute(HRadianceCache, cacheTracingUpdate_Kernel, (HashStorageSize / HashUpdateFraction) / 64, 1, 1); 
                    }

                    // Cache light evaluation at hit points
                    using (new ProfilingScope(cmdList, new ProfilingSampler("Cache Light Evaluation")))
                    {
                        int cacheLightEvaluation_Kernel = HRadianceCache.FindKernel("CacheLightEvaluation");
                        cmdList.DispatchCompute(HRadianceCache, cacheLightEvaluation_Kernel, (HashStorageSize / HashUpdateFraction) / 64, 1, 1); 
                    }

                    // Cache writing at primary surfaces
                    using (new ProfilingScope(cmdList, new ProfilingSampler("Primary Cache Spawn")))
                    {  
                        int cachePrimarySpawn_Kernel = HRadianceCache.FindKernel("CachePrimarySpawn");
                        cmdList.SetComputeTextureParam(HRadianceCache, cachePrimarySpawn_Kernel, "_ReprojectionCoords", ReprojectionCoord);
                        cmdList.SetComputeTextureParam(HRadianceCache, cachePrimarySpawn_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                        cmdList.SetComputeTextureParam(HRadianceCache, cachePrimarySpawn_Kernel, "_GeometryNormal", GeometryNormal);
                        cmdList.SetComputeTextureParam(HRadianceCache, cachePrimarySpawn_Kernel, "_RadianceAtlas", HitRadiance);
                        cmdList.DispatchCompute(HRadianceCache, cachePrimarySpawn_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                    }

                    // Cache counter update, deallocation of out-of-bounds entries, filtered cache population
                    using (new ProfilingScope(cmdList, new ProfilingSampler("Cache Data Update")))
                    {   
                        // Clear filtered cache every frame before writing to it
                        // CoreUtils.SetRenderTarget(ctx.cmd, RadianceCacheFiltered, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                        
                        int cacheDataUpdate_Kernel = HRadianceCache.FindKernel("CacheDataUpdate");
                        cmdList.DispatchCompute(HRadianceCache, cacheDataUpdate_Kernel, HashStorageSize / 64, 1, 1);
                    }  
                }
                
            }

            
            // ---------------------------------------- SPATIAL PREPASS ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Spatial Prepass")))
            {   
                // Gather probe ambient occlusion from ray hit distance and temporally accumulate
                int probeAmbientOcclusion_Kernel = HProbeAmbientOcclusion.FindKernel("ProbeAmbientOcclusion");
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, "_RayDistanceSS", HitDistanceScreenSpace);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, "_RayDistanceWS", HitDistanceWorldSpace);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, "_ReprojectionWeights", PersistentReprojectionWeights);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, "_PersistentReprojectionCoord", PersistentReprojectionCoord);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, "_ProbeAmbientOcclusion_History", ProbeAmbientOcclusion_History);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, "_ProbeAmbientOcclusion_Output", ProbeAmbientOcclusion);
                cmdList.DispatchCompute(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Prepare offsets and weights for further spatial passes
                int spatialPrepass_Kernel = HSpatialPrepass.FindKernel("SpatialPrepass");
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, "_ProbeAmbientOcclusion", ProbeAmbientOcclusion);
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, "_ProbeNormalDepth_History", ProbeNormalDepth_History);
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, "_SpatialOffsets_Output", SpatialOffsetsPacked);
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, "_SpatialWeights_Output", SpatialWeightsPacked);
                cmdList.SetComputeBufferParam(HSpatialPrepass, spatialPrepass_Kernel, "_PointDistribution", PointDistributionBuffer);
                cmdList.SetComputeBufferParam(HSpatialPrepass, spatialPrepass_Kernel, "_SpatialOffsetsBuffer", SpatialOffsetsBuffer);
                cmdList.DispatchCompute(HSpatialPrepass, spatialPrepass_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
       
                // Spatially filter probe ambient occlusion
                int probeAmbientOcclusionSpatialFilter_Kernel = HProbeAmbientOcclusion.FindKernel("ProbeAmbientOcclusionSpatialFilter");
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, "_SpatialWeightsPacked", SpatialWeightsPacked);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, "_SpatialOffsetsPacked", SpatialOffsetsPacked);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, "_ProbeAmbientOcclusion", ProbeAmbientOcclusion);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, "_ProbeAmbientOcclusion_OutputFiltered", ProbeAmbientOcclusion_Filtered);
                cmdList.DispatchCompute(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
            }
            

            // ---------------------------------------- ReSTIR TEMPORAL REUSE ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("ReSTIR Temporal Reuse")))
            {
                int probeAtlasTemporalReuse_Kernel = HReSTIR.FindKernel("ProbeAtlasTemporalReuse");
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_DepthPyramid", DepthPyramid);
                
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_ShadowGuidanceMask", ShadowGuidanceMask_Filtered);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_RayDirection", RayDirections);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_RayDistance", HitDistanceWorldSpace);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_RadianceAtlas", HitRadiance);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_ProbeDiffuse", ProbeDiffuse); 
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_ReprojectionWeights", PersistentReprojectionWeights);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_PersistentReprojectionCoord", PersistentReprojectionCoord);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_ReservoirAtlas_Output", ReservoirAtlas);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_ReservoirAtlas_History", ReservoirAtlas_History);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_ReservoirAtlasRayData_Output", ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, "_ReservoirAtlasRadianceData_Output", ReservoirAtlasRadianceData_A);
                cmdList.SetComputeIntParam(HReSTIR, "_UseDiffuseWeight", GeneralData.DebugModeWS == DebugModeWS.None ? 1 : 0);
                cmdList.DispatchCompute(HReSTIR, probeAtlasTemporalReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
            }
            
         
            // ---------------------------------------- RESERVOIR OCCLUSION VALIDATION ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Reservoir Occlusion Validation")))
            {
                // Run one pass of spatial reuse in disocclusion areas to generate shadow guidance mask
                int probeAtlasSpatialReuse_Kernel = HReSTIR.FindKernel("ProbeAtlasSpatialReuseDisocclusion");
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ProbeDiffuse", ProbeDiffuse); 
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_SpatialWeightsPacked", SpatialWeightsPacked);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_SpatialOffsetsPacked", SpatialOffsetsPacked);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData", ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData", ReservoirAtlasRadianceData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData_Output", ReservoirAtlasRayData_C);
                cmdList.SetComputeBufferParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_TracingCoords", IndirectCoordsSF);
                cmdList.SetComputeBufferParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_RayCounter", RayCounter);
                cmdList.SetComputeIntParam(HReSTIR, "_IndexXR", 0);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, IndirectArgumentsSF, 0);
                if (TextureXR.slices > 1)
                {
                    cmdList.SetComputeIntParam(HReSTIR, "_IndexXR", 1);
                    cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, IndirectArgumentsSF, sizeof(uint) * 3);
                }

                // Reproject occlusion checkerboarded history
                int reservoirOcclusionReprojection_Kernel = HReservoirValidation.FindKernel("OcclusionReprojection");
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionReprojection_Kernel, "_ReprojectionCoords", ReprojectionCoord);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionReprojection_Kernel, "_ProbeAmbientOcclusion", ProbeAmbientOcclusion_Filtered);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionReprojection_Kernel, "_ShadowGuidanceMask_History", ShadowGuidanceMask_CheckerboardHistory);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionReprojection_Kernel, "_ShadowGuidanceMask_Output", ShadowGuidanceMask);
                cmdList.DispatchCompute(HReservoirValidation, reservoirOcclusionReprojection_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);

                // Validate reservoir occlusion
                int reservoirOcclusionValidation_Kernel = HReservoirValidation.FindKernel("OcclusionValidation");
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_DepthPyramid", DepthPyramid);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_ReprojectionCoords", ReprojectionCoord);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_ReservoirAtlasRayData",  ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_ReservoirAtlasRayData_Disocclusion",  ReservoirAtlasRayData_C);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_ReservoirAtlas", ReservoirAtlas);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_ReservoirAtlasRadianceData_Inout", ReservoirAtlasRadianceData_B);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_ShadowGuidanceMask_Output", ShadowGuidanceMask);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_ProbeAmbientOcclusion", ProbeAmbientOcclusion_Filtered);
                cmdList.SetComputeBufferParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_PointDistribution", PointDistributionBuffer);
                cmdList.SetComputeBufferParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_RayCounter", RayCounter);
                cmdList.SetComputeBufferParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, "_TracingCoords", IndirectCoordsOV);
                cmdList.SetComputeIntParam(HReservoirValidation, "_IndexXR", 0);
                cmdList.DispatchCompute(HReservoirValidation, reservoirOcclusionValidation_Kernel, IndirectArgumentsOV, 0);
                if (TextureXR.slices > 1)
                {
                    cmdList.SetComputeIntParam(HReservoirValidation, "_IndexXR", 1);
                    cmdList.DispatchCompute(HReservoirValidation, reservoirOcclusionValidation_Kernel, IndirectArgumentsOV, sizeof(uint) * 3);
                }

                // Temporal accumulation pass
                int occlusionTemporalFilter_Kernel = HReservoirValidation.FindKernel("OcclusionTemporalFilter");
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, "_ReprojectionWeights", ReprojectionWeights);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, "_ReprojectionCoords", ReprojectionCoord);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, "_ShadowGuidanceMask", ShadowGuidanceMask);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, "_SampleCount_History", ShadowGuidanceMask_SamplecountHistory);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, "_SampleCount_Output", ShadowGuidanceMask_Samplecount);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, "_ShadowGuidanceMask_History", ShadowGuidanceMask_History);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, "_ShadowGuidanceMask_Output", ShadowGuidanceMask_Accumulated);
                cmdList.DispatchCompute(HReservoirValidation, occlusionTemporalFilter_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);

                // Spatial filtering pass
                int occlusionSpatialFilter_Kernel = HReservoirValidation.FindKernel("OcclusionSpatialFilter");
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, "_SpatialWeightsPacked", SpatialWeightsPacked);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, "_SpatialOffsetsPacked", SpatialOffsetsPacked);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, "_SampleCount", ShadowGuidanceMask_Samplecount);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, "_ShadowGuidanceMask", ShadowGuidanceMask_Accumulated);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, "_ShadowGuidanceMask_Output", ShadowGuidanceMask_Filtered);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, "_ReservoirAtlasRadianceData_Inout", ReservoirAtlasRadianceData_A);
                cmdList.DispatchCompute(HReservoirValidation, occlusionSpatialFilter_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
            }
            
            
            // ---------------------------------------- ReSTIR SPATIAL REUSE ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("ReSTIR Spatial Reuse")))
            {
                // Prepare spatial kernel
                int probeAtlasSpatialReuse_Kernel = HReSTIR.FindKernel("ProbeAtlasSpatialReuse");
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ProbeDiffuse", ProbeDiffuse);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_SpatialWeightsPacked", SpatialWeightsPacked);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_SpatialOffsetsPacked", SpatialOffsetsPacked);
                
                // 1st spatial disk pass
                cmdList.SetComputeIntParam(HReSTIR, "_PassNumber", 1);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData", ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData", ReservoirAtlasRadianceData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData_Output", ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData_Output", ReservoirAtlasRadianceData_B);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
                
                // 2nd spatial disk pass
                cmdList.SetComputeIntParam(HReSTIR, "_PassNumber", 2);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData", ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData", ReservoirAtlasRadianceData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData_Output", ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData_Output", ReservoirAtlasRadianceData_A);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);

                // 3rd spatial disk pass
                cmdList.SetComputeIntParam(HReSTIR, "_PassNumber", 3);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData", ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData", ReservoirAtlasRadianceData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData_Output", ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData_Output", ReservoirAtlasRadianceData_B);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
                
                // 3rd spatial disk pass
                cmdList.SetComputeIntParam(HReSTIR, "_PassNumber", 2);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData", ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData", ReservoirAtlasRadianceData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRayData_Output", ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, "_ReservoirAtlasRadianceData_Output", ReservoirAtlasRadianceData_A);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
            }   
            
      
            // ---------------------------------------- PERSISTENT HISTORY UPDATE ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Persistent History Update")))
            {   
                // Scroll history indirection array slice by slice
                int historyIndirectionScroll_Kernel = HTemporalReprojection.FindKernel("HistoryIndirectionScroll");
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyIndirectionScroll_Kernel, "_ReprojectionCoord", ReprojectionCoord);
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyIndirectionScroll_Kernel, "_HistoryIndirection", HistoryIndirection);
                
                // Scrolling cycle
                for (int i = PersistentHistorySamples - 1; i > 0; i--)
                {
                    cmdList.SetComputeIntParam(HTemporalReprojection, "_HistoryArrayIndex", i);
                    cmdList.DispatchCompute(HTemporalReprojection, historyIndirectionScroll_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                }
                
                // Update history indirection coord buffer
                int historyIndirectionUpdate_Kernel = HTemporalReprojection.FindKernel("HistoryIndirectionUpdate");
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyIndirectionUpdate_Kernel, "_HistoryIndirection", HistoryIndirection);
                cmdList.DispatchCompute(HTemporalReprojection, historyIndirectionUpdate_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Update probe world position & normal history buffer
                int historyProbeBuffersUpdate_Kernel = HTemporalReprojection.FindKernel("HistoryProbeBuffersUpdate");
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyProbeBuffersUpdate_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyProbeBuffersUpdate_Kernel, "_ProbeWorldPosNormal_HistoryOutput", ProbeWorldPosNormal_History);
                cmdList.DispatchCompute(HTemporalReprojection, historyProbeBuffersUpdate_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Update probe ambient occlusion history buffer
                int probeAmbientOcclusionHistoryUpdate_Kernel = HProbeAmbientOcclusion.FindKernel("ProbeAmbientOcclusionHistoryUpdate");
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionHistoryUpdate_Kernel, "_ProbeAmbientOcclusion", ProbeAmbientOcclusion);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionHistoryUpdate_Kernel, "_ProbeAmbientOcclusion_Output", ProbeAmbientOcclusion_History);
                cmdList.DispatchCompute(HProbeAmbientOcclusion, probeAmbientOcclusionHistoryUpdate_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Update reserovir history buffer
                int reservoirHistoryUpdate_Kernel = HReSTIR.FindKernel("ReservoirHistoryUpdate");
                cmdList.SetComputeTextureParam(HReSTIR, reservoirHistoryUpdate_Kernel, "_ReservoirAtlas", ReservoirAtlas);
                cmdList.SetComputeTextureParam(HReSTIR, reservoirHistoryUpdate_Kernel, "_ReservoirAtlas_Output", ReservoirAtlas_History);
                cmdList.DispatchCompute(HReSTIR, reservoirHistoryUpdate_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
            }
            
            
            // ---------------------------------------- INTERPOLATION ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Interpolation")))
            {   
                // Spherical harmonics gather
                int gatherSH_Kernel = HInterpolation.FindKernel("GatherSH");
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, "_ShadowGuidanceMask", ShadowGuidanceMask_Accumulated);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, "_ReservoirAtlasRadianceData", ReservoirAtlasRadianceData_A);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, "_ReservoirAtlasRayData", ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, "_Temp", ShadowGuidanceMask_Accumulated);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, "_PackedSH_A_Output", PackedSH_A);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, "_PackedSH_B_Output", PackedSH_B);
                cmdList.DispatchCompute(HInterpolation, gatherSH_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Interpolation to the final resolution
                int interpolation_Kernel = HInterpolation.FindKernel("Interpolation");
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, "_ProbeSSAO", ProbeSSAO);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, "_PackedSH_A", PackedSH_A);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, "_PackedSH_B", PackedSH_B);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, "_GeometryNormal", GeometryNormal);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, "_BentNormalsAO", BentNormalsAO_Interpolated);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, "_Radiance_Output", Radiance_Interpolated);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, "_ProbeNormalDepth", ProbeNormalDepth);
                cmdList.SetComputeFloatParam(HInterpolation, "_AO_Intensity", ScreenSpaceLightingData.OcclusionIntensity);
                cmdList.DispatchCompute(HInterpolation, interpolation_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
            }

            
            // ---------------------------------------- TEMPORAL DENOISER ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Temporal Denoising")))
            {
                int temporalDenoising_Kernel = HTemporalDenoiser.FindKernel("TemporalDenoising");
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, "_GeometryNormal", GeometryNormal);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, "_NormalDepth_History", NormalDepth_History);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, "_Radiance", Radiance_Interpolated);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, "_Radiance_History", RadianceAccumulated_History);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, "_Radiance_Output", RadianceAccumulated);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, "_LuminanceDelta_Output", LuminanceDelta);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, "_LuminanceDelta_History", LuminanceDelta_History);
                cmdList.DispatchCompute(HTemporalDenoiser, temporalDenoising_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
            }
            
            
            // ---------------------------------------- SPATIAL CLEANUP ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Spatial Cleanup")))
            {
                int spatialCleanup_Kernel = HTemporalDenoiser.FindKernel("SpatialCleanup");
                cmdList.SetComputeTextureParam(HTemporalDenoiser, spatialCleanup_Kernel, "_GeometryNormal", GeometryNormal);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, spatialCleanup_Kernel, "_Radiance", RadianceAccumulated);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, spatialCleanup_Kernel, "_Radiance_HistoryOutput", RadianceAccumulated_History);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, spatialCleanup_Kernel, "_NormalDepth_HistoryOutput", NormalDepth_History);
                cmdList.DispatchCompute(HTemporalDenoiser, spatialCleanup_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
            }
            
            
             // ---------------------------------------- COPY BUFFERS ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Copy Buffers")))
            {
                int hCopyProbeBuffers_Kernel = HCopy.FindKernel("CopyProbeBuffers");
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeBuffers_Kernel, "_ShadowGuidanceMask_Samplecount", ShadowGuidanceMask_Samplecount);
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeBuffers_Kernel, "_ShadowGuidanceMask_SamplecountHistoryOutput", ShadowGuidanceMask_SamplecountHistory);
                cmdList.DispatchCompute(HCopy, hCopyProbeBuffers_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                int hCopyProbeAtlases_Kernel = HCopy.FindKernel("CopyProbeAtlases");
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeAtlases_Kernel, "_ShadowGuidanceMask", ShadowGuidanceMask);
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeAtlases_Kernel, "_ShadowGuidanceMask_CheckerboardHistoryOutput", ShadowGuidanceMask_CheckerboardHistory);
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeAtlases_Kernel, "_ShadowGuidanceMask_Accumulated", ShadowGuidanceMask_Accumulated);
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeAtlases_Kernel, "_ShadowGuidanceMask_HistoryOutput", ShadowGuidanceMask_History);
                cmdList.DispatchCompute(HCopy, hCopyProbeAtlases_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);

                int hCopyFullResBuffers_Kernel = HCopy.FindKernel("CopyFullResBuffers");
                cmdList.SetComputeTextureParam(HCopy, hCopyFullResBuffers_Kernel, "_GeometryNormal", GeometryNormal);
                cmdList.SetComputeTextureParam(HCopy, hCopyFullResBuffers_Kernel, "_NormalDepth_HistoryOutput", NormalDepth_History);
                cmdList.DispatchCompute(HCopy, hCopyFullResBuffers_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
            }
            
            // Final output
            cmdList.SetGlobalTexture("_HTraceBufferGI", RadianceAccumulated);
            
            
            // ---------------------------------------- DEBUG (DON'T SHIP!) ---------------------------------------- //
            using (new ProfilingScope(cmdList, new ProfilingSampler("Debug Passthrough")))
            {
                if (GeneralData.DebugModeWS != DebugModeWS.None)
                {
                    int hDebugPassthrough_Kernel = HDebugPassthrough.FindKernel("DebugPassthrough");
                    cmdList.SetComputeTextureParam(HDebugPassthrough, hDebugPassthrough_Kernel, "_InputA", RadianceAccumulated);
                    cmdList.SetComputeTextureParam(HDebugPassthrough, hDebugPassthrough_Kernel, "_InputB", VoxelVisualizationRayDirections);
                    cmdList.SetComputeTextureParam(HDebugPassthrough, hDebugPassthrough_Kernel, "_Output", DebugOutput);
                    cmdList.DispatchCompute(HDebugPassthrough, hDebugPassthrough_Kernel, fullResX_8, fullResY_8, TextureXR.slices); 
                }
            }
            
            
            // Disable visualization keywords by default
            VoxelVisualization.EnableKeyword("VISUALIZE_OFF"); VoxelVisualization.DisableKeyword("VISUALIZE_LIGHTING"); VoxelVisualization.DisableKeyword("VISUALIZE_COLOR");
            
            // Visualize voxels if requested
            using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Visualize Voxels")))
            {
                if (GeneralData.DebugModeWS == DebugModeWS.VoxelizedLighting || GeneralData.DebugModeWS == DebugModeWS.VoxelizedColor)
                {
                    if (GeneralData.DebugModeWS == DebugModeWS.VoxelizedLighting)
                    {VoxelVisualization.EnableKeyword("VISUALIZE_LIGHTING"); VoxelVisualization.DisableKeyword("VISUALIZE_COLOR"); VoxelVisualization.DisableKeyword("VISUALIZE_OFF");}
					               
                    if (GeneralData.DebugModeWS == DebugModeWS.VoxelizedColor)
                    {VoxelVisualization.EnableKeyword("VISUALIZE_COLOR"); VoxelVisualization.DisableKeyword("VISUALIZE_LIGHTING"); VoxelVisualization.DisableKeyword("VISUALIZE_OFF");}
						
                    if (GeneralData.Multibounce == Multibounce.None) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_CACHE"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_APV"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_OFF"); }
                    if (GeneralData.Multibounce == Multibounce.IrradianceCache) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_OFF"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_APV"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_CACHE"); }
                    if (GeneralData.Multibounce == Multibounce.AdaptiveProbeVolumes) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_CACHE"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_OFF"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_APV"); }
                    
                    // Calculate rays in camera frustum
                    var debugCameraFrustum = ComputeFrustumCorners(ctx.hdCamera.camera);  
                    
                    // Vector4[] cameraFrustumCorners = new Vector4[8];
                    // Vector3[] cameraFrustumCornersTemp = new Vector3[4];
                    //
                    // float farClipPlane = Mathf.Min(100000.0f, camera.farClipPlane);
                    // camera.CalculateFrustumCorners(camera.rect, farClipPlane, (camera.stereoEnabled ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Mono), cameraFrustumCornersTemp);
                    // cameraFrustumCornersTemp[0].z = -cameraFrustumCornersTemp[0].z;
                    // cameraFrustumCornersTemp[1].z = -cameraFrustumCornersTemp[1].z;
                    // cameraFrustumCornersTemp[2].z = -cameraFrustumCornersTemp[2].z;
                    // cameraFrustumCornersTemp[3].z = -cameraFrustumCornersTemp[3].z;
                    // Matrix4x4 leftViewToWorld = (camera.stereoEnabled ? camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse : camera.cameraToWorldMatrix);
                    // cameraFrustumCorners[0] = leftViewToWorld * cameraFrustumCornersTemp[0]; // bottom left
                    // cameraFrustumCorners[1] = leftViewToWorld * cameraFrustumCornersTemp[1]; // top left
                    // cameraFrustumCorners[2] = leftViewToWorld * cameraFrustumCornersTemp[2]; // top right
                    // cameraFrustumCorners[3] = leftViewToWorld * cameraFrustumCornersTemp[3]; // bottom right
                    // camera.CalculateFrustumCorners(camera.rect, farClipPlane, Camera.MonoOrStereoscopicEye.Right, cameraFrustumCornersTemp);
                    // cameraFrustumCornersTemp[0].z = -cameraFrustumCornersTemp[0].z;
                    // cameraFrustumCornersTemp[1].z = -cameraFrustumCornersTemp[1].z;
                    // cameraFrustumCornersTemp[2].z = -cameraFrustumCornersTemp[2].z;
                    // cameraFrustumCornersTemp[3].z = -cameraFrustumCornersTemp[3].z;
                    // Matrix4x4 rightViewToWorld = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;
                    // cameraFrustumCorners[4] = rightViewToWorld * cameraFrustumCornersTemp[0];
                    // cameraFrustumCorners[5] = rightViewToWorld * cameraFrustumCornersTemp[1];
                    // cameraFrustumCorners[6] = rightViewToWorld * cameraFrustumCornersTemp[2];
                    // cameraFrustumCorners[7] = rightViewToWorld * cameraFrustumCornersTemp[3];
                    
                    // camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Right, frustumCorners);
                    //
                    // for (int i = 0; i < 4; i++)
                    // {
                    //     var worldSpaceCorner = camera.transform.TransformVector(frustumCorners[i]);
                    //     Debug.DrawRay(camera.transform.position, worldSpaceCorner, Color.red);
                    // }
                    //
                                    
                    // Vector3[] frustumCorners = new Vector3[4];
                    // camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners); 
                    //
                    // Transform cameraTransform = camera.transform;
                    // Vector3 bottomLeft = cameraTransform.TransformVector(frustumCorners[1]);
                    // Vector3 topLeft = cameraTransform.TransformVector(frustumCorners[0]);
                    // Vector3 bottomRight = cameraTransform.TransformVector(frustumCorners[2]); 
                    //
                    // debugCameraFrustum= Matrix4x4.identity;
                    // debugCameraFrustum.SetRow(0, bottomLeft);
                    // debugCameraFrustum.SetRow(1, bottomLeft + (bottomRight - bottomLeft) * 2);
                    // debugCameraFrustum.SetRow(2, bottomLeft + (topLeft - bottomLeft) * 2);
                    //
                    // Interpolate rays in vf shader
                    VoxelVisualizationMaterial.SetMatrix("_DebugCameraFrustum", debugCameraFrustum);
                  //  VoxelVisualizationMaterial.SetVectorArray("_DebugCameraFrustumArray", cameraFrustumCorners);
                    CoreUtils.DrawFullScreen(ctx.cmd, VoxelVisualizationMaterial, VoxelVisualizationRayDirections, shaderPassId: 0, properties: ctx.propertyBlock);

                    // Trace into voxels for debug
                    int voxelVisualization_Kernel = VoxelVisualization.FindKernel("VisualizeVoxels");
                    ctx.cmd.SetComputeTextureParam(VoxelVisualization, voxelVisualization_Kernel, "_DebugRayDirection", VoxelVisualizationRayDirections);
                    ctx.cmd.SetComputeTextureParam(VoxelVisualization, voxelVisualization_Kernel, "_Visualization_Output", DebugOutput);
                    cmdList.SetComputeIntParam(VoxelVisualization, "_MultibounceMode", (int)GeneralData.Multibounce);
                    ctx.cmd.DispatchCompute(VoxelVisualization, voxelVisualization_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
                }
            }
            
            if (Time.frameCount % 2 == 0)
                HFrameIndex++;
            
            HashUpdateFrameIndex++;

            if (GeneralData.DebugModeWS !=  DebugModeWS.None)
                cmdList.SetGlobalTexture("_HTraceBufferGI", DebugOutput);
        }

        protected internal void Release() //don't use Clenup, because after lauch it called
        {
            _initialized = false;
            HExtensions.HRelease(PointDistributionBuffer);
            AllocateMainRT(true);
            AllocateDepthPyramidRT(new Vector2Int(0,0) ,true);
            AllocateSSAO_RT(true);
            AllocateDebugRT(true);
            AllocateIndirectionBuffers(new Vector2Int(0,0), true);
            ReallocHashBuffers(true);
            
            if (VoxelizationRuntimeData != null)
                VoxelizationRuntimeData.OnReallocTextures -= () => ReallocHashBuffers();
            if (GeneralData != null)
                GeneralData.OnRayCountChanged -= RayCountChanged;
        }
    }
}
