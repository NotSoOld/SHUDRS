using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS  {

	///  Some fragmentation settings that are more convenient to store in a such container rather 
	/// than in one of the Destructible script itself.
	///  Fragments search for this object by going through parents in the hierachy, so root
	/// FragmentationSettings can be 'rewritten' by some other that can be on ConstrElements, 
	/// StructConstructions or Fragments themselves.
	public class FragmentationSettings : MonoBehaviour  {

		/// Fragments' 'OnCollisionEnter' messages will be triggered by colliders with following tags,
		/// causing Fragment to move.
		[Tooltip(
			"Fragments' 'OnCollisionEnter' messages will be triggered by colliders with following tags, " +
			"causing Fragment to move."
		)]
		public string[] tagsToMove;

		/// Fragments' 'OnCollisionEnter' messages will be triggered by colliders with names containing 
		/// the following substrings, causing Fragment to move.
		[Tooltip(
			"Fragments' 'OnCollisionEnter' messages will be triggered by colliders with names containing " +
			"the following substrings, causing Fragment to move."
		)]
		public string[] nameSubstringsToMove;

		/// Prefab with directional debris burst. Mesh and material will be applied automatically.
		[Tooltip("Prefab with directional debris burst. Mesh and material will be applied automatically.")]
		public GameObject directionalDebris;

		/// Prefab with 'go-down' debris. Mesh and material will be applied automatically.
		[Tooltip("Prefab with 'go-down' debris. Mesh and material will be applied automatically.")]
		public GameObject goDownDebris;

		/// Material tag of Fragments (for example, "glass" or "concrete") for varying weaponry impact.
		[Tooltip("Material tag of Fragments (for example, 'glass' or 'concrete') for varying weaponry impact.")]
		public string materialTag;

		/// Some special tags that may be defined and used by user (in any way he wants).
		[Tooltip("Some special tags that may be defined and used by user (in any way he wants).")]
		public string[] specialTags;

		////////////
		///
		/// TIP: here you can add some your own fields and use them, it won't break the whole system. :)
		/// 
		/// 
		
	}

}