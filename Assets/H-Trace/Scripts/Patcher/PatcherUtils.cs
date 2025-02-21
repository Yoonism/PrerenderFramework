using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace HTrace.Scripts.Patcher
{
	internal static class PatcherUtils
	{
		
		internal static void ReplacePatterns(string filePath, List<string[]> patterns, List<string[]> newpatterns, ref List<string> resultLines)
		{
			var readAllLines = File.ReadAllLines(filePath);

			int patternIndex = 0;

			for (int i = 0; i < readAllLines.Length; i++)
			{
				var possiblePattern = patterns.Any(pattern => DeleteTabulationAndSpaces(readAllLines[i]) == DeleteTabulationAndSpaces(pattern[0]));
				if (possiblePattern)
				{
					int          countLinesPattern = patterns[patternIndex].Length;
					List<string> patternMatch      = new List<string>(countLinesPattern);
					for (int j = 0; j < countLinesPattern; j++)
					{
						patternMatch.Add(readAllLines[j + i]);
					}

					if (CheckPattern(patterns[patternIndex], patternMatch.ToArray()))
					{
						resultLines.AddRange(newpatterns[patternIndex]);
						i += patterns[patternIndex].Length - 1; // -1 because our loop has i++
						patternIndex++;
						continue;
					}
				}

				resultLines.Add(readAllLines[i]);
			}
		}

		private static bool CheckPattern(string[] pattern, string[] patternMatch)
		{
			for (int i = 0; i < pattern.Length; i++)
			{
				if (DeleteTabulationAndSpaces(pattern[i]) == DeleteTabulationAndSpaces(patternMatch[i]))
					continue;
				return false;
			}

			return true;
		}

		private static string DeleteTabulationAndSpaces(string input)
		{
			var result = input.Replace("	", "");
			result = result.Replace("\t", "");
			result = result.Replace(" ",  "");
			return result;
		}

		private static bool CheckHTraceWords(string[] allLines)
		{
			foreach (var line in allLines)
			{
				if (line.ToUpper().Contains("H-TRACE") || line.ToUpper().Contains("HTRACE"))
				{
					return true;
				}
			}

			return false;
		}

		public static string ReplaceLeftSlashesToRight(this string input)
		{
			string replaced = input.Replace('\\', '/');
			return replaced;
		}

		public static bool IsGlobalSettingsWithHtraceResources()
		{
			
			RenderPipelineGlobalSettings globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
#if UNITY_6000_0
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

			return ssgiComputeFieldInfo != null && ((ComputeShader)ssgiComputeFieldInfo.GetValue(hdRenderPipelineRuntimeShadersObject)).name.Contains("HTrace");
#else
			FieldInfo hdrpResourcesField = globalSettings.GetType().GetField("m_RenderPipelineResources", BindingFlags.Instance | BindingFlags.NonPublic);
			object    runtimeResourcesObjectInGlobalSettings = hdrpResourcesField?.GetValue(globalSettings);
			
			return runtimeResourcesObjectInGlobalSettings != null && runtimeResourcesObjectInGlobalSettings.ToString().Contains("HTrace");
#endif
		}
		
	}
}
