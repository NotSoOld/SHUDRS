using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS  {

	///  All Destructible classes implement this, so you can work with any of them without knowing exact type of a Destructible.
	public interface IDestructible  {

		///  Entry point for perfoming full-constructional detachments (like caved floor, roof, etc.)
		void PerformDetachment();


		///  Entry point for erasing all connections between every Fragment (in the Element) or between every 
		/// Element (in Construction/Structure), so the Destructible is going to collapse like 'house of cards'.
		void TurnIntoDestructibleWithoutConnections();
		
	}

}