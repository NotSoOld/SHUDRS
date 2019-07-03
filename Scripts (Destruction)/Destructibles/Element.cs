using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SHUDRS.Destructibles {

	///  Element is too big to be a Fragment, so it consists of Fragments.
	/// There are Root Elements (which live their own happy life of Destructible :D) and Constr(uction)
	/// Elements, which are the part of Construction.
	public abstract class Element : MonoBehaviour, IDestructible {

		/// The unique index of this Element.
		public int index;

		/// The Fragments this Element consists of.
		public Fragment[] fragments;

		/// Adjacency matrix of this Element.
		public AdjacencyMatrix adjMatrix;

		/// Flag; if true, connections check is needed.
		public bool connectionsFlag {
			get;
			set; }

		/// Our cached root Construction.
		public Construction construction;

		/// List of grounded Fragments (which are connected to a surface that means not to move).
		public List<Fragment> groundFragments;

		/// Array of visited Fragments. Is used in a whole graph bypassing.
		protected bool[] isVisited;

		/// Temp array that is cleared at every graph sub-pass.
		protected bool[] isTempVisited;

		/// Permanent array of destroyed Fragments. 
		/// Is copied to 'isVisited' array at the start of graph bypassing.
		public bool[] isDestroyed;

		/// Number of child Fragments. Is used everywhere.
		public int len;

		/// List of connections of this particular Element.
		public List<Connection> connections;

		/// Our cached Rigidbody (for RootElements).
		public Rigidbody rb;

		/// Internal flag for checking if we've removed some connections while last act of destruction.
		protected bool smthChanged;

		/// Position of mass point is an average sum of positions of all Fragments.
		public Vector3 massPoint;

		/// Position of support point is an average sum of positions of all ground Fragments and all connection Fragments.
		public Vector3 supportPoint;

		/// This is length of a vector among calculated 'mass' point and 'support' point of Element. 
		/// Increase this for more stability. Value '<= 0' means no stability check at all. You can set
		/// default value after '=' operator below.
		public float stabilityEdgeValue = 3f;

		/// Internal bool to store condition when we do not need, can't or not allowed to calculate stability
		/// values and check stability. 
		/// If you want to disable stability checks, set 'stabilityEdgeValue' <= 0.
		public bool noNeedToCheckStability { get; protected set; }

		/// Should we use this Element Renderer component instead of all Fragments' Renderers 
		/// if we're still whole (for perfomance reason)?
		public bool useRenderersSwitch;

		/// Show mass point and support point as gizmos for debugging?
		public bool showStabilityGizmos = true;


		///  Adds this Element to Update event at the start of lifetime.
		public void Start() {

			TimeManager.UpdateElements += UpdateElement;

		}


		///  Removes this Element from Update event at the end of lifetime.
		public void OnDestroy() {

			TimeManager.UpdateElements -= UpdateElement;

		}


		///  Use this for initialization.
		public virtual void Initialize() {

			/// Remove results of previous initialization.
			Cleanup();

			/// Add Fragment script to children.
			Fragment tfrag;
			for (int i = 0; i < transform.childCount; i++) {
				tfrag = transform.GetChild(i).gameObject.AddComponent<Fragment>();
				tfrag.Initialize();
			}

			/// Get array of this element' fragments.
			fragments = GetComponentsInChildren<Fragment>();
			len = fragments.Length;
#if UNITY_EDITOR
			if (len > 100) {
				Debug.LogWarning(
					"Element " + this.name + " has more than 100 fragments.\n" +
					"Note that extremely big elements may lead to performance issues."
				);
			}
#endif
			isDestroyed = new bool[len];

		}


		///  Use this for re-initialization.
		public virtual void Reinitialize() {

			/// Some clean-ups.
			if (GetComponentsInChildren<Fragment>(false).Length == 0) {
				Destroy(this.gameObject);
			}

			connectionsFlag = false;

			/// Get array of this element' Fragments.
			fragments = GetComponentsInChildren<Fragment>(false);
			len = fragments.Length;
			isDestroyed = new bool[len];

			/// Assign unique indexes to child Fragments.
			for (int i = 0; i < len; i++) {
				fragments[i].Reinitialize();
			}

		}


		///  Use this to undo initialization of Fragments.
		public void Cleanup() {

			for (int i = 0; i < transform.childCount; i++) {

				if (transform.GetChild(i).GetComponent<Fragment>()) {
					DestroyImmediate(transform.GetChild(i).GetComponent<Fragment>());
				}

				if (transform.GetChild(i).GetComponent<MeshCollider>()) {
					DestroyImmediate(transform.GetChild(i).GetComponent<MeshCollider>());
				}
			}

			if (GetComponent<Rigidbody>()) {
				DestroyImmediate(GetComponent<Rigidbody>());
			}
		}


		///  Is subscribed to an event and executes in the LateUpdate at frame 1.
		public void UpdateElement() {

			if (connectionsFlag) {

				connectionsFlag = false;
				/// Entry point for checking and dividing Element into physical pieces.
				Debug.Log(this.name + " UpdateElement invoke");
				CheckConnectionsInElement();

			}

		}


		///  For perfomance reasons we can have entire Element renderer enabled by default, and if some Fragment
		/// gets destroyed, we disable Element renderer and enable Fragments' renderers.
		public void SwitchRenderers() {

			/// If we done this already (or do not need to do this by default) - return.
			if (!useRenderersSwitch) {
				return;
			}

			/// If we allowed to do it, but there's no mesh to display - return.
			if (GetComponent<MeshFilter>() == null || GetComponent<MeshRenderer>() == null) {
				useRenderersSwitch = false;
				return;
			}

			/// Disable Element renderer.
			GetComponent<MeshRenderer>().enabled = false;

			/// Get and enable fragments' renderers.
			for (int i = 0; i < len; i++) {
				fragments[i].GetComponent<MeshRenderer>().enabled = true;
			}

			/// So, we done this operation. Flag it!
			useRenderersSwitch = false;

		}


		///  Creates adjacency matrix of fragments of this element, treating them like a graph.
		public abstract void BuildAdjacencyMatrix(bool isInitializing = true);


		///  Finds Fragments that are standing on the ground 
		/// ('ground' - objects with DestructibleBaseObject component attached).
		public void BuildGroundFragmentsArray() {

			Transform tfrag;
			Collider[] neighbours;
			Mesh tmesh;

			if (groundFragments == null) {
				groundFragments = new List<Fragment>();
			} else {
				groundFragments.Clear();
			}

			for (int i = 0; i < len; i++) {
				tfrag = transform.GetChild(i);
				tmesh = tfrag.GetComponent<MeshFilter>().sharedMesh;

				/// We need to find connections with ground.
				neighbours = Physics.OverlapBox(
					tfrag.position,
					Vector3.Scale(tmesh.bounds.extents, tfrag.localScale * 1.01f),
					tfrag.rotation
				);

				/// Are we touching ground?
				for (int j = 0; j < neighbours.Length; j++) {
					if (neighbours[j].GetComponent<DestructibleBaseObject>() != null) {
						groundFragments.Add(fragments[i]);
						break;
					}
				}
			}

		}


		///  For debug purposes.
		public void ShowAdjacencyMatrix() {

			string s = "";
			for (int i = 0; i < len; i++) {
				for (int j = 0; j < len; j++) {
					s += string.Format(" {0}", System.Convert.ToByte(adjMatrix[i, j]));
				}
				s += "\n";
			}
			Debug.Log(s);

		}


		///  Erases destroyed Fragment from matrix.
		public virtual void MarkFragmentAsDestroyed(int findex) {

			for (int i = 0; i < len; i++) {
				adjMatrix[i, findex] = false;
				adjMatrix[findex, i] = false;
			}

			/// We can't visit this Fragment, so just mark it as destroyed for convenience.
			isDestroyed[findex] = true;

			/// We also need to exclude this Fragment from ground Fragments (if it was here).
			if (groundFragments != null) {
				int tempindex = groundFragments.FindIndex(f => f.index == findex);
				if (tempindex != -1) {
					groundFragments.RemoveAt(tempindex);
				}
			}

		}


		///  General entry point for checking existing connection groups of Fragments.
		protected abstract void CheckConnectionsInElement();


		///  Tries to recursively visit all graph vertices for Element.
		protected void CheckConnections(int start) {

			isVisited[start] = true;
			isTempVisited[start] = true;
			for (int i = 0; i < len; i++) {
				if (adjMatrix[start, i] == null) {
					Debug.LogWarning(this.name + " Element.CheckConnections: adjMatrix element is null.");
				}
				if (adjMatrix[start, i].Value && !isVisited[i]) {
					CheckConnections(i);
				}
			}

		}


		///  User entry point. Calling this will divide whole Element into particular moving Fragments.
		/// (can be accessed through interface)
		public void TurnIntoDestructibleWithoutConnections() {

			for (int i = 0; i < len; i++) {
				fragments[i].DestroyFragment(true);
			}

		}


		/******  STABILITY MANAGING METHODS  ******/


		///  Simply calculates position of mass point of this Element.
		public void CalculateMassPoint() {

			Vector3 point = new Vector3();
			for (int i = 0; i < len; i++) {
				point += fragments[i].transform.position;
			}
			point /= len;
			massPoint = point;

		}


		///  Calculates support point, summing positions of all ground and connection Fragments.
		public void CalculateSupportPoint() {

			Vector3 point = new Vector3();

			if (groundFragments != null) {
				for (int i = 0; i < groundFragments.Count; i++) {
					point += groundFragments[i].transform.position;
				}
			}

			int ccnt = 0;
			if (connections != null) {
				for (int i = 0; i < connections.Count; i++) {
					/// We need to check if this is connection with an Element which is connected only to us.
					/// In such case it is inefficient to take such connection into account.
					/// So, if other Element isn't connected only to us OR is grounded...
					if (
						(connections[i].fragment2.element.connections != null &&
							connections[i].fragment2.element.connections.Exists(
							c => c.fragment2.element != connections[i].fragment1.element
						)) ||
						(connections[i].fragment2.element.groundFragments != null &&
						connections[i].fragment2.element.groundFragments.Count != 0)
					) {
						point += connections[i].fragment1.transform.position;
						ccnt++;
					}
				}
			}

			int cnt = 0;
			cnt += (groundFragments != null) ? (groundFragments.Count) : (0);
			cnt += (connections != null) ? (ccnt) : (0);
			point /= cnt;
			point.Set(point.x, massPoint.y, point.z);
			supportPoint = point;

		}


		///  Just a method that can wait some time before checking stability.
		protected IEnumerator StabilityCoroutine() {

			/// Calculate stability values (even if we don't need to check stability, 
			/// these values are still needed by parent constructions/structures).
			CalculateMassPoint();
			CalculateSupportPoint();

			if (noNeedToCheckStability) {
				yield break;
			}

			if (stabilityEdgeValue <= 0f) {
				noNeedToCheckStability = true;
				yield break;
			}

			/// If we have neither ground nor connection points...
			if ((connections == null || connections.Count == 0) && (groundFragments == null || groundFragments.Count == 0)) {
				noNeedToCheckStability = true;
				yield break;
			}

			/// If we now consist only of ground fragments...
			if (groundFragments.Count == fragments.Length) {
				noNeedToCheckStability = true;
				yield break;
			}

			/// If we now consist only of connected fragments...
			byte[] conIndexes = new byte[len];
			connections.ForEach(c => conIndexes[c.fragment1.index] = 1);
			if (System.Array.TrueForAll(conIndexes, ind => ind == 1)) {
				noNeedToCheckStability = true;
				yield break;
			}

			/// Wait for 30 frames...
			for (int i = 0; i < 30; i++) {
				yield return null;
			}

			/// Perform the check and re-build fragments hierachy.
			if (ElementIsUnstable()) {
				PerformDetachment();
				connectionsFlag = true;
			}

		}


		///  True, if mass point and support point are too far from each other.
		protected bool ElementIsUnstable() {

			if ((massPoint - supportPoint).sqrMagnitude > Mathf.Pow(stabilityEdgeValue, 2f)) {
				return true;
			} else {
				return false;
			}
		}


		///  User entry point. Removes some integrity information so Element itself will be still whole
		/// but it will break from its connection and ground fragments.
		/// (can be accessed through interface)
		public void PerformDetachment() {

			if (groundFragments != null) {

				for (int i = 0; i < groundFragments.Count; i++) {
					for (int j = groundFragments[i].index + 1; j < len; j++) {
						/// Remove connections between 'regular' and 'ground' fragments
						/// (but not between 'ground' and 'ground' or 'regular' and 'regular' fragments).
						if (!groundFragments.Exists(f => f.index == j)) {
							adjMatrix[groundFragments[i].index, j] = false;
							adjMatrix[j, groundFragments[i].index] = false;
						}

					}
				}

			}

			if (connections != null) {

				byte[] conIndexes = new byte[len];
				connections.ForEach(c => conIndexes[c.fragment1.index] = 1);

				for (int i = 0; i < len; i++) {
					if (conIndexes[i] == 0) {
						continue;
					}

					for (int j = i + 1; j < len; j++) {
						/// Remove connections between 'connection' and 'regular' fragments.
						if (conIndexes[j] == 0) {
							adjMatrix[i, j] = false;
							adjMatrix[j, i] = false;
						}

					}
				}

			}

		}


#if UNITY_EDITOR
		/// Displays massPoint and supportPoint for debug purposes.
		public void OnDrawGizmos() {

			if (showStabilityGizmos) {

				Gizmos.color = Color.green - new Color(0f, 0f, 0f, 0.5f);
				Gizmos.DrawSphere(massPoint, 0.2f);
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(massPoint, 0.2f);

				Gizmos.color = Color.magenta - new Color(0f, 0f, 0f, 0.5f);
				Gizmos.DrawSphere(supportPoint, 0.2f);
				Gizmos.color = Color.magenta;
				Gizmos.DrawWireSphere(supportPoint, 0.2f);

			}

		}
#endif

	}

}