using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.Infrastructure
{
	public class CustomPassObject
	{
		public PassHandler      Handler;
		public CustomPass[]     CustomPass;
		public CustomPassVolume CustomPassVolume;

		public CustomPassObject(PassHandler handler, CustomPassVolume customPassVolume, params CustomPass[] customPass)
		{
			Handler = handler;
			CustomPass = customPass;
			CustomPassVolume = customPassVolume;
		}
	}
}
