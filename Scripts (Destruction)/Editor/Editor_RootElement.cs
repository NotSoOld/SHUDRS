﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using SHUDRS.Destructibles;

namespace SHUDRS  {

	[CustomEditor(typeof(RootElement))]
	public class Editor_RootElement : Editor  {

		public RootElement element;
		public SerializedProperty stabilityEdgeVal;
		public SerializedProperty useRenderersSwitch;
		public SerializedProperty showStabilityGizmos;


		/// This method is called when we click on the Element to show it in Inspector.
		public void OnEnable()  {

			element = (RootElement)target;
			stabilityEdgeVal = serializedObject.FindProperty("stabilityEdgeValue");
			useRenderersSwitch = serializedObject.FindProperty("useRenderersSwitch");
			showStabilityGizmos = serializedObject.FindProperty("showStabilityGizmos");

		}


		///
		public override void OnInspectorGUI()  {

			GUILayout.Space(10f);
			if(GUILayout.Button("Initialize Element!", GUILayout.Width(250f), GUILayout.Height(30f)))  {
				element.Initialize();
				EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
				EditorGUIUtility.ExitGUI();
			}
			GUILayout.Space(10f);

			if(GUILayout.Button("Clean up child Fragments", GUILayout.Width(250f), GUILayout.Height(25f)))  {
				element.Cleanup();
				EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
				EditorGUIUtility.ExitGUI();
			}
			GUILayout.Space(10f);

			if(GUILayout.Button("Print Adjacency Matrix\n(Debugging)", GUILayout.Width(250f), GUILayout.Height(40f)))  {
				element.ShowAdjacencyMatrix();
			}
			GUILayout.Space(10f);

			serializedObject.Update();

			GUIContent content = new GUIContent(
				"Stability Edge Value",
				"This is length of a vector among calculated 'mass' point and 'support' point of Element. " +
				"Increase this for more stability.\n'<= 0' means no stability check at all."
			);
			EditorGUILayout.PropertyField(stabilityEdgeVal, content, GUILayout.Width(250f));
			GUILayout.Space(10f);

			content = new GUIContent(
				"Use Renderers Switch",
				"If true, Element renderer will be used instead of all Fragments' renderers while this Element " +
				"will be whole. This can increase graphics perfomance but requires a Mesh Renderer on the Element " +
				"with a mesh that displays the whole Element at the same place."
			);
			EditorGUILayout.PropertyField(useRenderersSwitch, content, GUILayout.Width(250f));
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