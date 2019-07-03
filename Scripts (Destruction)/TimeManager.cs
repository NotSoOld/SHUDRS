using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS {

	///  This is really a chronometer for all Destructibles in the scene. To make their destruction methods
	/// work, you need one (and only one!) object of this class present in the scene.
	public class TimeManager : MonoBehaviour {

		/// Elements subscribe to this at level start.
		public static event UpdateDel UpdateElements;

		/// Constructions subscribe to this at level start.
		public static event UpdateDel UpdateConstructions;

		/// Structures subscribe to this at level start.
		public static event UpdateDel UpdateStructures;


		///  LateUpdate is called once per frame after all regular calculations.
		/// The order of events in code and the fact that they are all in one method guarantees
		/// right execution order of integrity checks.
		void LateUpdate() {
			UpdateElements?.Invoke();
			UpdateConstructions?.Invoke();
			UpdateStructures?.Invoke();
		}

	}

}