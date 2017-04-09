using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS  {

	///  All root Destructibles implement this, so by finding object of this interface you can find the root of the Destructible.
	public interface IRootDestructible  {

		/// Absolute delta between velocity at the last frame and the current frame.
		float deltaMagnitude  { get; set; }
		
	}

}