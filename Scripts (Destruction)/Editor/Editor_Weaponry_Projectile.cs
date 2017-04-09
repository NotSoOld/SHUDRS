using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SHUDRS.Destructibles;
using SHUDRS.Weaponry;

namespace SHUDRS  {

	[CustomEditor(typeof(Projectile))]
	public class Editor_Weaponry_Projectile : Editor  {

		public Projectile projectile;
		public SerializedProperty hitTriggers;
		public SerializedProperty hitMask;
		public SerializedProperty impactType;
		public SerializedProperty innerRadius;
		public SerializedProperty outerRadius;


		/// This method is called when we click on the Projectile to show it in Inspector.
		public virtual void OnEnable()  {

			projectile = (Projectile)target;
			hitTriggers = serializedObject.FindProperty("hitTriggers");
			hitMask = serializedObject.FindProperty("hitMask");
			impactType = serializedObject.FindProperty("impactType");
			innerRadius = serializedObject.FindProperty("innerRadius");
			outerRadius = serializedObject.FindProperty("outerRadius");

		}


		///
		public override void OnInspectorGUI()  {
			
			GUILayout.Space(10f);

			serializedObject.Update();

			GUIContent content = new GUIContent(
				"Hit Triggers?",
				"Should this projectile collide with triggers?"
			);
			EditorGUILayout.PropertyField(hitTriggers, content, GUILayout.Width(250f));
			GUILayout.Space(10f);

			content = new GUIContent(
				"Hit Mask",
				"Defines which layers this projectile can collide with."
			);
			EditorGUILayout.PropertyField(hitMask, content, GUILayout.Width(250f));
			GUILayout.Space(10f);

			content = new GUIContent(
				"Impact Type",
				"Choose which type of impact should the projectile make when colliding." +
				"\n\n- NoImpact: projectile will have no effect at the object it's colliding with " +
				"(like bullet hits the wall)." +
				"\n- Point: projectile will affect the only object it hits." +
				"\n- InnerOnly: projectile will cast sphere of 'innerRadius' radius, and you can manage objects " +
				"inside this sphere. For example, default 'PerformDestruction' method will turn Fragments in the inner " +
				"sphere into debris." +
				"\n- OuterOnly: projectile will cast sphere of 'outerRadius' radius, and you can manage objects " +
				"inside this sphere. For example, default 'PerformDestruction' method will cause Fragments in the outer " +
				"sphere to move out of the Element." +
				"\n- Bispherical: projectile will cast two spheres mentioned above."
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