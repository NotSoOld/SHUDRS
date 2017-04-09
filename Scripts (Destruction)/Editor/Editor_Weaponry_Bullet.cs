using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SHUDRS.Destructibles;
using SHUDRS.Weaponry;

namespace SHUDRS  {

	[CustomEditor(typeof(Bullet))]
	public class Editor_Weaponry_Bullet : Editor_Weaponry_Projectile  {

		/// Here go your own SerializedProperties (the ones that you add to a custom projectile class).

		/// This method is called when we click on the Bullet to show it in Inspector.
		public override void OnEnable()  {

			base.OnEnable();

			/// Here goes initialization of custom properties.

		}


		///
		public override void OnInspectorGUI()  {

			base.OnInspectorGUI();

			/// Here goes visualization of custom properties.

		}
			
	}

}