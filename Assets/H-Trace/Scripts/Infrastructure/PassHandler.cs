using UnityEngine;

namespace H_Trace.Scripts.Infrastructure
{
	[ExecuteInEditMode]
	public class PassHandler : MonoBehaviour
	{
		private IPing _ping;
		private CustomPassObject _customPassObject;

		internal virtual void Initialize(IPing ping, Transform parent, CustomPassObject customPassObject)
		{
			_ping = ping;
			_customPassObject = customPassObject;

			transform.parent = parent;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
		}

		protected virtual void Update()
		{
			if (_ping == null || _ping.Ping(_customPassObject) == false)
			{
				if (Application.isEditor && !Application.isPlaying)
					DestroyImmediate(this.gameObject);
				else
					Destroy(this.gameObject);
			}
		}
	}
}
