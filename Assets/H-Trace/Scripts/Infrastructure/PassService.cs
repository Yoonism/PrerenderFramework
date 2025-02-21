using System;
using System.Collections.Generic;
using System.Linq;
using H_Trace.Scripts.Passes;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace H_Trace.Scripts.Infrastructure
{
	internal class PassService
	{
		private readonly Dictionary<Type, CustomPassObject> _customPasses = new Dictionary<Type, CustomPassObject>();
		private readonly Dictionary<Type, CustomPass>       _customPassesImplementations;
		private readonly HTrace                             _hTrace;

		public PassService(HTrace hTrace)
		{
			_hTrace = hTrace;

			_customPassesImplementations = new Dictionary<Type, CustomPass>()
			{
				[typeof(HTracePrePass)]             = new HTracePrePass() {enabled             = false},
				[typeof(HTraceMainPass)]            = new HTraceMainPass() {enabled            = false},
				[typeof(HTraceFinalPass)]           = new HTraceFinalPass() {enabled           = false},
				[typeof(VoxelizationPassStaggered)] = new VoxelizationPassStaggered() {enabled = false},
				[typeof(VoxelizationPassConstant)]  = new VoxelizationPassConstant() {enabled  = false},
				[typeof(VoxelizationPassPartial)]   = new VoxelizationPassPartial() {enabled   = false},
			};
		}

		/// <summary>
		/// It's not flexible, only for HTrace
		/// </summary>
		/// <param name="injectionPoint"></param>
		/// <param name="passName"></param>
		/// <param name="priority"></param>
		/// <param name="passes"></param>
		/// <typeparam name="K"></typeparam>
		/// <returns></returns>
		public CustomPassObject GetOrCreateCustomPassObject<K>(CustomPassInjectionPoint injectionPoint, string passName, int priority = 0, params Type[] passes)
			where K : PassHandler
		{
			for (int index = 0; index < passes.Length; index++)
			{
				if (_customPasses.TryGetValue(passes[index], out var implementation))
					return implementation;
			}

			GameObject passGO = new GameObject(passName);
			//passGO.hideFlags = HideFlags.HideInHierarchy;//TODO: release uncomment
			PassHandler passHandler = null;
			passHandler = passGO.AddComponent<K>();

			CustomPassVolume volume = passGO.AddComponent<CustomPassVolume>();
			volume.injectionPoint = injectionPoint;
			volume.priority = priority;

			CustomPass[] customPasses = new CustomPass[passes.Length];
			for (int index = 0; index < customPasses.Length; index++)
			{
				customPasses[index] = _customPassesImplementations[passes[index]];
			}

			CustomPassObject passObject = new CustomPassObject(passHandler, volume, customPasses);
			for (int index = 0; index < passes.Length; index++)
			{
				volume.customPasses.Add(customPasses[index]);
				_customPasses.Add(passes[index], passObject);
			}

			passHandler.Initialize(_hTrace, _hTrace.transform, passObject);

			return passObject;
		}

		public void DeletePass<T>()
		{
			if (_customPasses.TryGetValue(typeof(T), out CustomPassObject customPass))
			{
				Object.DestroyImmediate(customPass.Handler.gameObject);
				_customPasses.Remove(typeof(T));
			}
		}

		public bool CustomPassObjectContains(CustomPassObject customPassObject)
		{
			if (_customPasses.Values.Contains(customPassObject))
				return true;

			return false;
		}

		public void Cleanup()
		{
			while (_customPasses.Any())
			{
				var keyValuePair = _customPasses.First();

				if (_customPasses[keyValuePair.Key].Handler != null)
					Object.DestroyImmediate(_customPasses[keyValuePair.Key].Handler.gameObject);
				
				_customPasses.Remove(keyValuePair.Key);
			}

			_customPasses.Clear();
			_customPassesImplementations.Clear();
		}
	}
}
