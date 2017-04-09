using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SHUDRS.Destructibles;
using SHUDRS.Weaponry;

namespace SHUDRS  {

	[CustomEditor(typeof(Explosive))]
	public class Editor_Weaponry_Explosive : Editor  {

		public Explosive explosive;
		public SerializedProperty impactType;
		public SerializedProperty innerRadius;
		public SerializedProperty outerRadius;
		public SerializedProperty explosionForce;


		/// This method is called when we click on the Explosive to show it in Inspector.
		public virtual void OnEnable()  {

			explosive = (Explosive)target;
			impactType = serializedObject.FindProperty("impactType");
			innerRadius = serializedObject.FindProperty("innerRadius");
			outerRadius = serializedObject.FindProperty("outerRadius");
			explosionForce = serializedObject.FindProperty("explosionForce");

		}


		///
		public override void OnInspectorGUI()  {
			
			GUILayout.Space(10f);

			serializedObject.Update();

			GUIContent content = new GUIContent(
				"Explosion Force",
				"How powerful is this explosion?"
			);
			EditorGUILayout.PropertyField(explosionForce, content, GUILayout.Width(250f));
			explosionForce.floatValue = Mathf.Clamp(explosionForce.floatValue, 0f, Mathf.Infinity);
			GUILayout.Space(10f);

			content = new GUIContent(
				"Impact Type",
				"Choose which type of impact should the explosive make when activating." +
				"\n\n- NoImpact: explosive will have no effect at the objects. (???)" +
				"\n- Point: explosive will affect the only object. (???)" +
				"\n- InnerOnly: explosive will cast sphere of 'innerRadius' radius, and you can manage objects " +
				"inside this sphere. For example, default 'PerformDestruction' method will turn Fragments in the inner " +
				"sphere into debris." +
				"\n- OuterOnly: explosive will cast sphere of 'outerRadius' radius, and you can manage objects " +
				"inside this sphere. For example, default 'PerformDestruction' method will cause Fragments in the outer " +
				"sphere to move out of the Element." +
				"\n- Bispherical: explosive will cast two spheres mentioned above."
			);
			EditorGUILayout.PropertyField(impactType, content, GUILayout.Width(250f));
			GUILayout.Space(10f);

			switch(impactType.enumValueIndex)  {

			case (int)ImpactType.InnerOnly:  {
					
					content = new GUIContent(
						"Inner Radius",
						"Radius of inner impact sphere."
					);
					EditorGUILayout.PropertyField(innerRadius, content, GUILayout.Width(250f));
					GUILayout.Space(10f);

					innerRadius.floatValue = Mathf.Clamp(innerRadius.floatValue, 0.001f, 500f);

					break;

				}

			case (int)ImpactType.OuterOnly:  {
					
					content = new GUIContent(
						"Outer Radius",
						"Radius of outer impact sphere."
					);
					EditorGUILayout.PropertyField(outerRadius, content, GUILayout.Width(250f));
					GUILayout.Space(10f);

					outerRadius.floatValue = Mathf.Clamp(outerRadius.floatValue, 0.001f, 1000f);

					break;

				}

			case (int)ImpactType.Bispherical:  {
					
					content = new GUIContent(
						"Inner Radius",
						"Radius of inner impact sphere. Should be smaller than outer radius."
					);
					EditorGUILayout.PropertyField(innerRadius, content, GUILayout.Width(250f));
					GUILayout.Space(10f);

					content = new GUIContent(
						"Outer Radius",
						"Radius of outer impact sphere. Should be greater than inner radius."
					);
					EditorGUILayout.PropertyField(outerRadius, content, GUILayout.Width(250f));
					GUILayout.Space(10f);

					outerRadius.floatValue = Mathf.Clamp(outerRadius.floatValue, 0.001f, 1000f);
					innerRadius.floatValue = Mathf.Clamp(innerRadius.floatValue, 0.001f, outerRadius.floatValue);

					break;

				}

			}

			serializedObject.ApplyModifiedProperties();

		}
			
	}

}