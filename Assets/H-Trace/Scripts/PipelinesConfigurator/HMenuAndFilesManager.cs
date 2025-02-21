#if UNITY_EDITOR
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEditor;

namespace H_Trace.Scripts.PipelinesConfigurator
{
	
	public class HMenuAndFilesManager : EditorWindow
	{
		[MenuItem("GameObject/Rendering/HTrace World Space Global Illumination", false, priority: 30)]
		static void CreateHTraceGameObject(MenuCommand menuCommand)
		{
			HTrace[] hTraces = FindObjectsOfType(typeof(HTrace)) as HTrace[];
			if (hTraces != null && hTraces.Length > 0)
			{
				Debug.Log("Can't create HTrace, because the scene already contains HTrace instance");
				return;
			}

			GameObject go = new GameObject("HTraceWSGI");
			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			go.AddComponent<HTrace>();

			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}

		[MenuItem("Window/HTrace/HTrace WSGI Open documentation", false)]
		private static void OpenDocumentation()
		{
			Application.OpenURL("https://ipgames.gitbook.io/htrace-wsgi");
		}
		
		// Rect buttonRect;
		// private void OnGUI()
		// {
		// 	{
		// 		GUILayout.Label("Editor window with Popup example", EditorStyles.boldLabel);
		// 		if (GUILayout.Button("Popup Options", GUILayout.Width(200)))
		// 		{
		// 			PopupWindow.Show(buttonRect, new PopupExample());
		// 		}
		// 		if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
		// 	}
		// }

	}
	
}

#endif