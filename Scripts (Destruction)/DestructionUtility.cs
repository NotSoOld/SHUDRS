using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SHUDRS.Destructibles;

namespace SHUDRS  {


	/// Template delegate for TimeManager events (void type, zero args).
	public delegate void UpdateDel();


	/// Struct to store information about connections between different Destructibles.
    /// Physical connections are presented as relations between two Fragments.
    /// Fragments' Elements or Constructions can be accessed through cached refs
    /// which Fragments contain.
    [System.Serializable]
	public struct Connection  {
		
		/// Local fragment which participates in connection.
		public Fragment fragment1;

		/// Fragment of other Element. 'fragment1' is connected to it.
		public Fragment fragment2;

	}


	///  Special data type to store adjacency matrix of a non-weighted graph. It is a bit array in a 
	/// nutshell, but is accessed as matrix (by two indices) for convenience. Also:
	/// - Standart .Net BitArray cannot be used because it doesn't serialize;
	/// - This type needs 8 times less memory than byte[] array (since I use only non-weighted graphs in my 
	/// system, I can sacrifice all values of byte type except 0 and 1).
	[System.Serializable]
	public class AdjacencyMatrix  {

		/// Inner byte array.
		[SerializeField]
		private byte[] arr;

		/// Inner constant length value (is changed only when initializing; I don't need adjacency matrices
		/// to expand or shrink during code execution, only get and set values one by one or all values 
		/// at one time, so...).
		[SerializeField]
		private int len;

		/// Accesses a bit value using specialized formulas, using row and column indices (for classical
		/// one-dimensional array it is like i * length + j, but we need to go deeper and access the particular bit).
		public bool this[int i, int j]  {

			get  {
				if(i >= len || j >= len || i < 0 || j < 0)  {
					#if UNITY_EDITOR
					Debug.LogErrorFormat(
						"Array index is out of range! Indexes must be between '(0, 0)' and '({0}, {0}), was ({1}, {2})",
						(len - 1), i, j
					);
					#endif
					return false;
				}
				else  {	
					/// 1) Get ((i * len + j)/8)th byte (don't forget to floor index);
					/// 2) Shift this byte (%8) so we get needed bit at position of least significant bit;
					/// 3) If this bit is 1, return true, otherwise return false
					/// (*******1 % 2 == true, *******0 % 2 == false).
					return System.Convert.ToBoolean((arr[(int)((i * len + j) / 8)] >> ((i * len + j) % 8)) % 2);
				}
			}

			set  {
				if(i >= len || j >= len || i < 0 || j < 0)  {
					#if UNITY_EDITOR
					Debug.LogErrorFormat(
						"Array index is out of range! Indexes must be between '(0, 0)' and '({0}, {0}), was ({1}, {2})",
						(len - 1), i, j
					);
					#endif
				}
				else  {
					/// Again, find a byte and logically sum (or invert and logically multiply)
					/// it with a value that we are trying to set. You can check that these formulas work properly
					/// using a list of paper and a pencil :D
					if(value)
						arr[(int)((i * len + j) / 8)] |= (byte)(1 << ((i * len + j) % 8));
					else
						arr[(int)((i * len + j) / 8)] &= (byte)(~(1 << ((i * len + j) % 8)));
				}
			}

		}


		/// Initializes new bit-storing AdjacencyMatrix with a width and height equal to 'newlen'.
		public AdjacencyMatrix(int newlen)  {

			arr = new byte[(int)(newlen * newlen / 8) + 1];
			len = newlen;

		}

	}


	///  Some useful static methods are presented here.
	public static class DestructionUtility  {



	}

}