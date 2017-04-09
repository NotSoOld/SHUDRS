using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using SHUDRS.Destructibles;

namespace SHUDRS  {

	[CustomEditor(typeof(Structure))]
	public class Editor_Structure : Editor  {

		public Structure structure;
		public SerializedProperty stabilityEdgeVal;
		public SerializedProperty showStabilityGizmos;


		/// This method is called when we click on the Structure to show it in Inspector.
		public void OnEnable()  {

			structure = (Structure)target;
			stabilityEdgeVal = serializedObject.FindProperty("stabilityEdgeValue");
			showStabilityGizmos = serializedObject.FindProperty("showStabilityGizmos");

		}


		///
		public override void OnInspectorGUI()  {

			GUILayout.Space(10f);
			if(GUILayout.Button("Initialize this Structure!", GUILayout.Width(250f), GUILayout.Height(30f)))  {
				structure.Initialize();
				EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
				EditorGUIUtility.ExitGUI();
			}
			GUILayout.Space(10f);

			if(GUILayout.Button(
				"Clean up child Fragments,\nElements and Constructions", 
				GUILayout.Width(250f), GUILayout.Height(40f)
			))  {
				structure.Cleanup();
				EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
				EditorGUIUtility.ExitGUI();
			}
			GUILayout.Space(10f);

			if(GUILayout.Button("Print Adjacency Matrix\n(Debugging)", GUILayout.Width(250f), GUILayout.Height(40f)))  {
				structure.ShowAdjacencyMatrix();
			}
			GUILayout.Space(10f);

			serializedObject.Update();

			GUIContent content = new GUIContent(
				"Stability Edge Value",
				"This is length of a vector among calculated 'mass' point and 'support' point of Structure. " +
				"Increase this for more stability.\n'<= 0' means no stability check at all."
			);
			EditorGUILayout.PropertyField(stabilityEdgeVal, content, GUILayout.Width(250f));
			GUILayout.Space(10f);

			content = new GUIContent(
				"Show Stability Gizmos",
				"If true, positions of 'mass' point and 'support' point of Element will be drawn as gizmos. " +
				"May be helpful when tweaking 'stability edge value'."
			);
			EditorGUILayout.PropertyField(showStabilityGizmos, content, GUILayout.Width(250f));

			serializedObject.ApplyModifiedProperties();
			GUILayout.Space(10f);

		}
			
	}

}