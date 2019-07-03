using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SHUDRS.Destructibles {

	///  Construction is a Destructible which consists of Elements (i.e. is too big to be just an Element).
	/// Integrity check goes during two frames: at first one we rebuild Elements, at second one - whole Construction.
	/// There are RootConstructions which doesn't belong to anything bigger, and Struct(ure)Constructions 
	/// which are the part of Structure.
	public abstract class Construction : MonoBehaviour, IDestructible {

		/// Unique sibling index of this Construction.
		public int index;

		/// Elements of our construction.
		public Element[] elements;

		/// Adjacency matrix that represents connections of elements in a convenient form.
		public AdjacencyMatrix adjMatrix;

		/// Flag that means that we need to do integrity check at the next frame.
		public bool connectionsFlag;

		/// Our cached parent structure (for StructConstructions).
		public Structure structure;

		/// Number of child Elements. Is used everywhere.
		public int len;

		/// Array of visited Elements. Is used in a whole graph bypassing.
		protected bool[] isVisited;

		/// Temp array that is cleared at every graph sub-pass.
		protected bool[] isTempVisited;

		/// List of grounded Elements (which are connected to a surface that means not to move).
		public List<Element> groundElements;

		/// This coroutine starts when 'connectionsFlag' is set to 'true', suspends for one frame
		/// and then does integrity check.
		protected Coroutine checkWaiter;

		/// Our cached Rigidbody (for RootConstruction).
		public Rigidbody rb;

		/// Position of mass point is an average sum of mass points of all Elements.
		public Vector3 massPoint;

		/// Position of support point is an average sum of support points of all ground Elements
		/// and positions of all child connection Fragments which
		/// are connected to different Construction.
		public Vector3 supportPoint;

		/// This is length of a vector among calculated 'mass' point and 'support' point of Construction. 
		/// Increase this for more stability. Value '<= 0' means no stability check at all. You can set
		/// default value after '=' operator below.
		public float stabilityEdgeValue = 5f;

		/// Internal bool to store condition when we do not need, can't or not allowed to calculate stability
		/// values and check stability. 
		/// If you want to disable stability checks, set 'stabilityEdgeValue' <= 0.
		public bool noNeedToCheckStability { get; protected set; }

		/// Show mass point and support point as gizmos for debugging?
		public bool showStabilityGizmos = true;

		/// Should we use this Construction Renderer component instead of all Elements' Renderers 
		/// if we're still whole (for perfomance reason)?
		public bool useRenderersSwitch;


		///  Adds this Construction to Update event at the start of lifetime.
		public void Start() {

			TimeManager.UpdateConstructions += UpdateConstruction;

		}


		///  Removes this Construction from Update event at the end of lifetime.
		public void OnDestroy() {

			TimeManager.UpdateConstructions -= UpdateConstruction;

		}


		///  Use this for script initialization. MUST be called before InitInfo, or there'll be data loss.
		public void InitCreateScripts() {

			/// Get rid of Elements' scripts.
			/// Fragments will be cleaned up later automatically.
			Cleanup();

			/// Add Element script to children and initialize them.
			Element telem;
			for (int i = 0; i < transform.childCount; i++) {
				telem = transform.GetChild(i).gameObject.AddComponent<ConstrElement>();
				telem.Initialize();
			}

		}


		/// Consequently initializes our Elements (order is essential! Otherwise there will be loss of data).
		public virtual void InitInfo() {

			elements = GetComponentsInChildren<Element>();
			len = elements.Length;
#if UNITY_EDITOR
			if (len > 100) {
				Debug.LogWarning(
					"Construction " + this.name + " has more than 100 elements.\n" +
					"Note that extremely big constructions may lead to performance issues."
				);
			}
#endif
			/// Firstly, we need to calculate all these (in that order):
			for (int i = 0; i < len; i++) {
				elements[i].BuildAdjacencyMatrix();
				elements[i].BuildGroundFragmentsArray();
			}
			for (int i = 0; i < len; i++) {
				elements[i].CalculateMassPoint();
				elements[i].CalculateSupportPoint();
			}
			if (groundElements == null) {
				groundElements = new List<Element>();
			} else {
				groundElements.Clear();
			}

			/// Secondly, take information...
			TakeInfoFromElements();
			/// And only now we can calculate stability values.
			CalculateStabilityTransform();
			CalculateSupportCenter();

		}


		///  Method-helper that collects and organizes information from child Elements during initialization.
		protected void TakeInfoFromElements() {

			adjMatrix = new AdjacencyMatrix(len);

			for (int i = 0; i < len; i++) {
				elements[i].connections.ForEach(c => {
					if (c.fragment1.element.construction == c.fragment2.element.construction) {
						Debug.Log("Found adjacency: " + c.fragment1.element.name + " and " + c.fragment2.element.name
							+ " through " + c.fragment1.name + " connected to " + c.fragment2.name);
						adjMatrix[c.fragment1.element.index, c.fragment2.element.index] = true;
					}
				});
				if (elements[i].groundFragments != null && elements[i].groundFragments.Count > 0) {
					groundElements.Add(elements[i]);
				}
			}

		}


		///  Use this for reinitialization.
		public virtual void Reinitialize() {

			/// Get child elements array.
			elements = GetComponentsInChildren<Element>();
			len = elements.Length;
			for (int i = 0; i < len; i++) {
				elements[i].construction = this;
				elements[i].index = i;
			}
			/// Caution: these elements were already reinitialized!

			/// When this gets executed, all connections lists in Elements will be already corrected.
			/// So, we just take information from them.
			if (groundElements == null) {
				groundElements = new List<Element>();
			} else {
				groundElements.Clear();
			}

			TakeInfoFromElements();

			StartCoroutine(StabilityCoroutine());

		}


		///  Use this to clean up initialized elements.
		public void Cleanup() {

			for (int i = 0; i < transform.childCount; i++) {

				if (transform.GetChild(i).GetComponent<Element>()) {

					transform.GetChild(i).GetComponent<Element>().Cleanup();
					DestroyImmediate(transform.GetChild(i).GetComponent<Element>());

				}

			}

			if (GetComponent<Rigidbody>()) {
				DestroyImmediate(GetComponent<Rigidbody>());
			}
		}


		///  For perfomance reasons we can have entire Construction renderer enabled by default, and if
		/// some Element gets damaged, we disable Construction renderer and enable Elements' renderers.
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

			/// Disable Construction renderer.
			GetComponent<MeshRenderer>().enabled = false;

			/// Get and enable Elements' renderers.
			for (int i = 0; i < len; i++) {
				/// If condition is false, this particular Element must be displayed by its Fragments.
				if (elements[i].useRenderersSwitch) {
					elements[i].GetComponent<MeshRenderer>().enabled = true;
				}
			}

			/// So, we done this operation. Flag it!
			useRenderersSwitch = false;

		}


		///  Is subscribed to an event and executes in the LateUpdate (delayed to frame 2).
		public void UpdateConstruction() {

			if (connectionsFlag) {
				if (checkWaiter != null) {
					StopCoroutine(checkWaiter);
				}

				Debug.Log(this.name + " UpdateConstruction invoke (before coroutine)");

				checkWaiter = StartCoroutine(StartCheckWaiter());
			}

		}


		///  A coroutine that will wait one frame before integrity check execution.
		public IEnumerator StartCheckWaiter() {

			/// This will suspend the execution for one frame.
			yield return null;
			connectionsFlag = false;
			Debug.Log(this.name + " UpdateConstruction / StartCheckWaiter invoke (inside coroutine after waiting)");

			CheckConnectionsInConstruction();

		}


		///  Checks integrity in this Construction and divides it to several Constructions if they're not
		/// physically connected anymore.
		protected abstract void CheckConnectionsInConstruction();


		/// For debug purposes.
		public void ShowAdjacencyMatrix() {

			string s = "";
			for (int i = 0; i < len; i++) {
				s += elements[i].name;
				for (int j = 0; j < len; j++) {
					s += string.Format(" {0}", System.Convert.ToByte(adjMatrix[i, j]));
				}
				s += "\n";
			}
			Debug.Log(s);

		}


		///  Tries to recursively visit all Elements from 'start' using connections matrix.
		protected void CheckConnections(int start) {

			isVisited[start] = true;
			isTempVisited[start] = true;
			for (int i = 0; i < len; i++) {
				if (adjMatrix[start, i] == null) {
					Debug.LogWarning(this.name + " Construction.CheckConnections: adjMatrix element is null.");
				}
				if (adjMatrix[start, i].Value && !isVisited[i]) {
					CheckConnections(i);
				}
			}

		}


		///  User entry point to turn this Construction in a 'house of cards'-like Destructible.
		/// (can be accessed through interface)
		public void TurnIntoDestructibleWithoutConnections() {

			for (int i = 0; i < len; i++) {
				elements[i].connections = null;
				elements[i].connectionsFlag = true;
			}

		}


		/******  STABILITY MANAGING METHODS  ******/


		///  Sums positions of mass points of all Elements.
		public void CalculateStabilityTransform() {

			Vector3 point = new Vector3();
			int cnt = 0;

			for (int i = 0; i < len; i++) {
				point += elements[i].massPoint;
				cnt++;
			}
			point /= cnt;
			massPoint = point;

		}


		///  Sums positions of support points of all ground Elements and positions of all child connection fragments
		/// that are connected to different Construction.
		public void CalculateSupportCenter() {

			Vector3 point = new Vector3();
			int cnt = 0;

			for (int i = 0; i < groundElements.Count; i++) {
				point += groundElements[i].supportPoint;
				cnt++;
			}

			for (int i = 0; i < len; i++) {
				if (elements[i].connections != null) {
					int[] conIndexes = new int[elements[i].len];
					/// Find connections only between different Constructions.
					elements[i].connections.ForEach(c => {
						if (c.fragment1.element.construction != c.fragment2.element.construction) {
							conIndexes[c.fragment1.index] = 1;
						}
					});
					/// Exclude ground fragments (we're already summed them above).
					elements[i].groundFragments.ForEach(f => {
						conIndexes[f.index] = 0;
					});

					for (int j = 0; j < elements[i].len; j++) {
						if (conIndexes[j] != 0) {
							point += elements[i].fragments[j].transform.position;
							cnt++;
						}
					}
				}
			}

			point /= cnt;
			point.Set(point.x, massPoint.y, point.z);
			supportPoint = point;

		}


		///  Just a method that can wait some time before executing calculations and checks about stability.
		protected IEnumerator StabilityCoroutine() {

			if (noNeedToCheckStability) {
				yield break;
			}

			if (stabilityEdgeValue <= 0f) {
				noNeedToCheckStability = true;
				yield break;
			}

			/// Calculate stability values.
			CalculateStabilityTransform();
			CalculateSupportCenter();

			for (int i = 0; i < 30; i++) {
				yield return null;
			}

			/// Perform the check and re-build fragments hierachy.
			if (ConstructionIsUnstable()) {
				PerformDetachment();
			}

		}


		///  True, if mass point and support point are too far from each other.
		protected bool ConstructionIsUnstable() {

			if ((massPoint - supportPoint).sqrMagnitude > Mathf.Pow(stabilityEdgeValue, 2f)) {
				return true;
			} else {
				return false;
			}
		}


		///  User entry point. Removes information about Construction's connections with ground and other Constructions
		/// so it can look and behave detached or something like this.
		/// (can be accessed through interface)
		public void PerformDetachment() {

			if (groundElements != null) {

				for (int k = 0; k < groundElements.Count; k++) {

					for (int i = 0; i < groundElements[k].groundFragments.Count; i++) {

						for (int j = groundElements[k].groundFragments[i].index + 1; j < groundElements[k].len; j++) {

							if (!groundElements[k].groundFragments.Exists(f => f.index == j)) {

								groundElements[k].adjMatrix[groundElements[k].groundFragments[i].index, j] = false;

								groundElements[k].adjMatrix[j, groundElements[k].groundFragments[i].index] = false;

								groundElements[k].connectionsFlag = true;
							}

						}

					}

				}

			}

			for (int k = 0; k < len; k++) {

				if (elements[k].connections != null) {

					byte[] conIndexes = new byte[elements[k].len];
					elements[k].connections.ForEach(
						c => {
							if (c.fragment1.element.construction != c.fragment2.element.construction) {
								conIndexes[c.fragment1.index] = 1;
							}
						}
					);

					for (int i = 0; i < elements[k].len; i++) {
						if (conIndexes[i] == 0) {
							continue;
						}

						for (int j = i + 1; j < elements[k].len; j++) {

							if (conIndexes[j] == 0) {
								elements[k].adjMatrix[i, j] = false;
								elements[k].adjMatrix[j, i] = false;
								elements[k].connectionsFlag = true;
							}

						}
					}

				}

			}

		}


#if UNITY_EDITOR
		/// Displays mass point and support point as spheres for debug purposes.
		public void OnDrawGizmos() {

			if (showStabilityGizmos) {

				Gizmos.color = Color.yellow - new Color(0f, 0f, 0f, 0.5f);
				Gizmos.DrawSphere(massPoint, 0.4f);
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireSphere(massPoint, 0.4f);

				Gizmos.color = Color.black - new Color(0f, 0f, 0f, 0.5f);
				Gizmos.DrawSphere(supportPoint, 0.4f);
				Gizmos.color = Color.black;
				Gizmos.DrawWireSphere(supportPoint, 0.4f);

			}

		}
#endif

	}

}