#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using H_Trace.Scripts.PipelinesConfigurator;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = System.Object;

using UnityEngine.Rendering.HighDefinition;

namespace HTrace.Scripts.Patcher
{
	internal static class HPatcher
	{
		private static bool IsGlobalSettingsWithHtraceResources()
		{
			RenderPipelineGlobalSettings globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
			
#if UNITY_2022_2_OR_NEWER && !UNITY_6000_0
			
			object hdrpResourcesField = globalSettings.GetType().GetField("m_RenderPipelineResources", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(globalSettings);
			
			object shadersObj = hdrpResourcesField.GetType().GetField("shaders")?.GetValue(hdrpResourcesField);
			object ssgi  = shadersObj?.GetType().GetField("screenSpaceGlobalIlluminationCS")?.GetValue(shadersObj);
			
			return ssgi != null&& ssgi.ToString().Contains("HTrace");
#endif
#if  UNITY_6000_0
			object    msettingsObject                      = globalSettings.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(globalSettings);
			object    settingsListObject                   = msettingsObject?.GetType().GetField("m_SettingsList", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(msettingsObject);
			object    mListObject                          = settingsListObject?.GetType().GetField("m_List", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(settingsListObject);
			
			object    hdRenderPipelineRuntimeShadersObject = null;
			FieldInfo ssaoComputeFieldInfo                 = null;
			
			foreach (object obj in (IList)mListObject)
			{
				if (obj.GetType().Name.Contains("HDRenderPipelineRuntimeShaders"))
				{
					ssaoComputeFieldInfo                 = obj.GetType().GetField("m_ScreenSpaceGlobalIlluminationCS", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					hdRenderPipelineRuntimeShadersObject = obj;
				}
			}

			return hdRenderPipelineRuntimeShadersObject != null && ssaoComputeFieldInfo != null &&
			       ((ComputeShader)ssaoComputeFieldInfo.GetValue(hdRenderPipelineRuntimeShadersObject)).name.Contains("HTrace");
#endif
		}
		
		//Project settings - Global Settings - Resources
		public static void RenderPipelineRuntimeResourcesPatch(bool forceReplace = false)
		{
			if (IsGlobalSettingsWithHtraceResources() == true)
				return;

			CreateRpResourcesFolders();
			CopyAndChangeSSGICompute(forceReplace: forceReplace);

#if UNITY_2022_2_OR_NEWER && !UNITY_6000_0
			//new
			SetNewRuntimeResourcesInUnity();
			
			//old
			// {
			// 	GetHDRenderPipelineRuntimeResources(out UnityEngine.Object localRuntimeResourcesObject); //todo we can avoid copied resources and change it directly
			// 	ReplaceSSGIComputeInLocalResources(localRuntimeResourcesObject);
			// 	SetNewRuntimeResources(localRuntimeResourcesObject);
			// }
#endif
#if  UNITY_6000_0
			SetNewRuntimeResourcesInUnity6000();
#endif
		}
		

		private static void CreateRpResourcesFolders()
		{
			if (!Directory.Exists(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources")))
			{
				Directory.CreateDirectory(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources"));
				AssetDatabase.Refresh();
			}
			
			if (!Directory.Exists(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP")))
			{
				Directory.CreateDirectory(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP"));
				AssetDatabase.Refresh();
			}
			
			// if (!Directory.Exists(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "URP")))
			// {
			// 	Directory.CreateDirectory(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "URP"));
			// 	AssetDatabase.Refresh();
			// }
			
			// if (!Directory.Exists(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "BIRP")))
			// {
			// 	Directory.CreateDirectory(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "BIRP"));
			// 	AssetDatabase.Refresh();
			// }
		}
		
		public static void GlobalSettingsPlayerResourcesRestore()
		{
#if  UNITY_6000_0
			RenderPipelineGlobalSettings globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();

			object    msettingsObject                      = globalSettings.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(globalSettings);
			object    settingsListObject                   = msettingsObject?.GetType().GetField("m_SettingsList", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(msettingsObject);
			object    mListObject                          = settingsListObject?.GetType().GetField("m_List", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(settingsListObject);
			object    hdRenderPipelineRuntimeShadersObject = null;
			FieldInfo ssgiComputeFieldInfo                 = null;
			foreach (object obj in (IList)mListObject)
			{
				if (obj.GetType().Name.Contains("HDRenderPipelineRuntimeShaders"))
				{
					ssgiComputeFieldInfo                 = obj.GetType().GetField("m_ScreenSpaceGlobalIlluminationCS", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					hdRenderPipelineRuntimeShadersObject = obj;
				}
			}
				
			string originalSsgiComputePath = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "HDRP Resources", "ScreenSpaceGlobalIlluminationOriginal.compute");
			string ssgiComputeRelativePath = Path.Combine("Assets",                        Path.GetRelativePath(Application.dataPath, originalSsgiComputePath)).ReplaceLeftSlashesToRight();
			Object originalSsgiComputeObject  = AssetDatabase.LoadMainAssetAtPath(ssgiComputeRelativePath);
			
			ssgiComputeFieldInfo?.SetValue(hdRenderPipelineRuntimeShadersObject, originalSsgiComputeObject);
#endif
		}

		private static void CopyAndChangeSSGICompute(bool revert = false, bool forceReplace = false)
		{
			string ssgiComputeFullPath       = Path.Combine(ConfiguratorUtils.GetFullHdrpPath(),    "Runtime", "Lighting", "ScreenSpaceLighting", "ScreenSpaceGlobalIllumination.compute");
			string ssgiComputeHtraceFullPath = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(),  "RP Resources", "HDRP",      "ScreenSpaceGlobalIlluminationHTrace.compute");
			if (File.Exists(ssgiComputeHtraceFullPath) && forceReplace == false)
			{
				return;
			}
			try
			{
				File.Copy(ssgiComputeFullPath, ssgiComputeHtraceFullPath, true);
			}
			catch (Exception e)
			{
				Debug.LogError($"{ConfiguratorUtils.GetUnityAndHdrpVersion()} \n" +
				          $"Source path: {ssgiComputeFullPath} \n" +
				          $"Destination path: {ssgiComputeHtraceFullPath} \n" +
				          $"{e.Message}");
				return;
			}
			
			string[] pattern1 =
			{
				"// deferred opaque always use FPTL",
				"#define USE_FPTL_LIGHTLIST 1",
				"",
				"// HDRP generic includes",
			};

			
			string[] newpattern1 =
			{
				"// deferred opaque always use FPTL",
				"#define USE_FPTL_LIGHTLIST 1",
				"",
				"#pragma multi_compile _ HTRACE_OVERRIDE",
				"// HDRP generic includes",
			};
			
			string[] pattern2 =
			{
				"// Input depth pyramid texture",
				"TEXTURE2D_X(_DepthTexture);",
				"// Stencil buffer of the current frame",
				"TEXTURE2D_X_UINT2(_StencilTexture);",
				"// Input texture that holds the offset for every level of the depth pyramid",
				"StructuredBuffer<int2>  _DepthPyramidMipLevelOffsets;",
				"",
				"// Constant buffer that holds all scalar that we need",
				"CBUFFER_START(UnityScreenSpaceGlobalIllumination)",
			};

			
			string[] newpattern2 =
			{
				"// Input depth pyramid texture",
				"TEXTURE2D_X(_DepthTexture);",
				"// Stencil buffer of the current frame",
				"TEXTURE2D_X_UINT2(_StencilTexture);",
				"// Input texture that holds the offset for every level of the depth pyramid",
				"StructuredBuffer<int2>  _DepthPyramidMipLevelOffsets;",
				"// HTrace buffer",
				"TEXTURE2D_X(_HTraceBufferGI);",
				"",
				"// Constant buffer that holds all scalar that we need",
				"CBUFFER_START(UnityScreenSpaceGlobalIllumination)",
			};
			
			string[] pattern3 =
			{
				"[numthreads(INDIRECT_DIFFUSE_TILE_SIZE, INDIRECT_DIFFUSE_TILE_SIZE, 1)]",
				"void TRACE_GLOBAL_ILLUMINATION(uint3 dispatchThreadId : SV_DispatchThreadID, uint2 groupThreadId : SV_GroupThreadID, uint2 groupId : SV_GroupID)",
				"{",
				"    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);",
				"",
				"    // Compute the pixel position to process",
				"    uint2 currentCoord = dispatchThreadId.xy;",
				"    uint2 inputCoord = dispatchThreadId.xy;",
			};

			
			string[] newpattern3 =
			{
				"[numthreads(INDIRECT_DIFFUSE_TILE_SIZE, INDIRECT_DIFFUSE_TILE_SIZE, 1)]",
				"void TRACE_GLOBAL_ILLUMINATION(uint3 dispatchThreadId : SV_DispatchThreadID, uint2 groupThreadId : SV_GroupThreadID, uint2 groupId : SV_GroupID)",
				"{",
				"    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);",
				"",
				"#if defined HTRACE_OVERRIDE",
				"    return;",
				"#endif",
				"    // Compute the pixel position to process",
				"    uint2 currentCoord = dispatchThreadId.xy;",
				"    uint2 inputCoord = dispatchThreadId.xy;",
			};
			
			string[] pattern4 =
			{
				"[numthreads(INDIRECT_DIFFUSE_TILE_SIZE, INDIRECT_DIFFUSE_TILE_SIZE, 1)]",
				"void REPROJECT_GLOBAL_ILLUMINATION(uint3 dispatchThreadId : SV_DispatchThreadID, uint2 groupThreadId : SV_GroupThreadID, uint2 groupId : SV_GroupID)",
				"{",
				"    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);",
				"",
				"    // Compute the pixel position to process",
				"    uint2 inputCoord = dispatchThreadId.xy;",
				"    uint2 currentCoord = dispatchThreadId.xy;",
			};

			
			string[] newpattern4 =
			{
				"[numthreads(INDIRECT_DIFFUSE_TILE_SIZE, INDIRECT_DIFFUSE_TILE_SIZE, 1)]",
				"void REPROJECT_GLOBAL_ILLUMINATION(uint3 dispatchThreadId : SV_DispatchThreadID, uint2 groupThreadId : SV_GroupThreadID, uint2 groupId : SV_GroupID)",
				"{",
				"    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);",
				"",
				"#if defined HTRACE_OVERRIDE",
				"        _IndirectDiffuseTextureRW[COORD_TEXTURE2D_X(dispatchThreadId.xy)] = LOAD_TEXTURE2D_X(_HTraceBufferGI, dispatchThreadId.xy).xyz;",
				"        return;",
				"#endif",
				"    // Compute the pixel position to process",
				"    uint2 inputCoord = dispatchThreadId.xy;",
				"    uint2 currentCoord = dispatchThreadId.xy;",
			};
			
			List<string[]> patterns = new List<string[]>()
			{
				pattern1, pattern2, pattern3, pattern4,
			};
			
			List<string[]> newpatterns = new List<string[]>()
			{
				newpattern1, newpattern2, newpattern3, newpattern4,
			};
			
			List<string> resultLines = new List<string>();
			if(revert == false)
				PatcherUtils.ReplacePatterns(ssgiComputeHtraceFullPath, patterns, newpatterns, ref resultLines);
			else
				PatcherUtils.ReplacePatterns(ssgiComputeHtraceFullPath, newpatterns, patterns, ref resultLines);


			File.WriteAllLines(ssgiComputeHtraceFullPath, resultLines);
			AssetDatabase.Refresh();
		}

		private static void SetNewRuntimeResourcesInUnity()
		{
			RenderPipelineGlobalSettings globalSettings   = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
			
			string runtimeResourcePath = Path.Combine(ConfiguratorUtils.GetFullHdrpPath(),    "Runtime", "RenderPipelineResources", "HDRenderPipelineRuntimeResources.asset");
			string runtimeResourcesRelativePath = Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, runtimeResourcePath)).ReplaceLeftSlashesToRight();
			UnityEngine.Object unityRuntimeResourcesObject = AssetDatabase.LoadMainAssetAtPath(runtimeResourcesRelativePath);

			string             ssgiFullPath     = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP", "ScreenSpaceGlobalIlluminationHTrace.compute");
			string             ssgiRelativePath = Path.Combine("Assets",                                Path.GetRelativePath(Application.dataPath, ssgiFullPath)).ReplaceLeftSlashesToRight();
			UnityEngine.Object ssgiObject       = AssetDatabase.LoadMainAssetAtPath(ssgiRelativePath);
			
			
			object    hdrpResourcesObj = globalSettings.GetType().GetField("m_RenderPipelineResources", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(globalSettings);
			object    shadersObj       = hdrpResourcesObj.GetType().GetField("shaders")?.GetValue(hdrpResourcesObj);
			FieldInfo ssgiField        = shadersObj?.GetType().GetField("screenSpaceGlobalIlluminationCS");
			ssgiField?.SetValue(shadersObj, ssgiObject);
		}
		
		private static void SetNewRuntimeResourcesInUnity6000()
		{
			RenderPipelineGlobalSettings globalSettings                       = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
			object                       msettingsObject                      = globalSettings.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(globalSettings);
			object                       settingsListObject                   = msettingsObject?.GetType().GetField("m_SettingsList", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(msettingsObject);
			object                       mListObject                          = settingsListObject?.GetType().GetField("m_List", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(settingsListObject);
			
			object    hdRenderPipelineRuntimeShadersObject = null;
			object    hdrpRayTracingResourcesObject        = null;
			FieldInfo ssgiComputeFieldInfo                 = null;
			
			string ssgiComputePath    = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP", "ScreenSpaceGlobalIlluminationHTrace.compute");
			
			string ssgiComputeRelativePath   = Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, ssgiComputePath)).ReplaceLeftSlashesToRight();

			Object gtaoComputeObject   = AssetDatabase.LoadMainAssetAtPath(ssgiComputeRelativePath);
			
			foreach (object obj in (IList)mListObject)
			{
				if (obj.GetType().Name.Contains("HDRenderPipelineRuntimeShaders"))
				{
					ssgiComputeFieldInfo                 = obj.GetType().GetField("m_ScreenSpaceGlobalIlluminationCS",               BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					hdRenderPipelineRuntimeShadersObject = obj;
				}
			}

			ssgiComputeFieldInfo?.SetValue(hdRenderPipelineRuntimeShadersObject, gtaoComputeObject);
			
			AssetDatabase.ImportAsset(ssgiComputeRelativePath,   ImportAssetOptions.ForceUpdate);
		}
	}
}
#endif
