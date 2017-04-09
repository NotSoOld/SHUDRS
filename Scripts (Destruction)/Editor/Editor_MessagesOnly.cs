using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SHUDRS.Destructibles;

namespace SHUDRS  {


	[CustomEditor(typeof(DestructibleBaseObject))]
	public class Editor_DestructibleBaseObject : Editor  {

		public override void OnInspectorGUI()  {

			GUILayout.Space(10f);
			EditorGUILayout.HelpBox(
				"Since this object is marked as 'Base Object' for some Destructible " +
				"(RootElement, RootConstruction or Structure) which is related " +
				"to this, it is meant not to move or destroy this object " +
				"to prevent unrealistic behaviour.",
				MessageType.Warning
			);
			GUILayout.Space(10f);

		}

	}


	[CustomEditor(typeof(TimeManager))]
	public class Editor_TimeManager : Editor  {

		public override void OnInspectorGUI()  {

			GUILayout.Space(10f);
			EditorGUILayout.HelpBox(
				"You definitely should NOT destroy this object if you want your scene destructibles " +
				"continue to work. It clocks their internal checks for integrity and stability.",
				MessageType.Warning
			);
			GUILayout.Space(10f);

		}

	}

}