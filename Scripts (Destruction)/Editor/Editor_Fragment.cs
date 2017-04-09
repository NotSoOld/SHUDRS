using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SHUDRS.Destructibles;

namespace SHUDRS  {

	[CustomEditor(typeof(Fragment))]
	[CanEditMultipleObjects]
	public class Editor_Fragment : Editor  {

		public Fragment fragment;
		public SerializedProperty damagedGO;
		public SerializedProperty isIndestructible;
		public SerializedProperty startHealth;


		/// This method is called when we click on the Element to show it in Inspector.
		public void OnEnable()  {

			fragment = (Fragment)target;
			damagedGO = serializedObject.FindProperty("damagedGO");
			isIndestructible = serializedObject.FindProperty("isIndestructible");
			startHealth = serializedObject.FindProperty("startHealth");

		}


		///
		public override void OnInspectorGUI()  {

			GUILayout.Space(10f);

			EditorGUILayout.HelpBox(
				"Fragmentation settings of this fragment can be accessed at '" +
				fragment.settings.name+"' parent object.",
				MessageType.Info
			);
			GUILayout.Space(10f);

			serializedObject.Update();

			GUIContent content = new GUIContent(
				"Damaged GO",
				"Sometimes, when this Fragment is supposed to move because of damage, it should look damaged. "+
				"You can put damaged model of this Fragment here (without collider and rigidbody)."
			);
			EditorGUILayout.PropertyField(damagedGO, content, GUILayout.Width(250f));
			GUILayout.Space(10f);

			content = new GUIContent(
				"Fragment Is Indestructible?",
				"If true, this Fragment will remain indestructible in all cases of destruction, but will be still "+
				"taken into account by system as a part of Destructible."
			);
			EditorGUILayout.PropertyField(isIndestructible, content, GUILayout.Width(250f));
			GUILayout.Space(10f);

			content = new GUIContent(
				"Start Health",
				"If you need this value, you can set it, and Fragment will have this number of 'health points' "+
				"when game starts."
			);
			EditorGUILayout.PropertyField(startHealth, content, GUILayout.Width(250f));

			serializedObject.ApplyModifiedProperties();
			GUILayout.Space(10f);

		}
			
	}

}