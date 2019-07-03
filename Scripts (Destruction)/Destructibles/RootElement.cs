using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS.Destructibles {

	///  Root Element is a simpliest form of a Destructible. It processes its integrity in a one
	/// frame. It is not a part of something bigger.
	[RequireComponent(typeof(FragmentationSettings))]
	public class RootElement : Element, IDestructible, IRootDestructible {


		/// Rigidbody magnitude saved from last fixed frame.
		protected float lastMagnitude;

		/// Absolute delta between lastMagnitude and current magnitude.
		public float deltaMagnitude { get; set; }


		///  All Destructibles need Time Manager to work. If we don't have one - create it.
		public void Awake() {

			if (GameObject.FindObjectsOfType<TimeManager>().Length == 0) {
				new GameObject("Time Manager", typeof(TimeManager));
			}

		}


		///  Calculates delta and last velocity every physical frame.
		public void FixedUpdate() {

			if (rb != null) {
				deltaMagnitude = Mathf.Abs(rb.velocity.sqrMagnitude - lastMagnitude);
				lastMagnitude = rb.velocity.sqrMagnitude;
			}

		}


		///  Since we cannot track collisions in particular Fragments (because of dumb PhysX manners...),
		/// and all collisions are tracked by object who has Rigidbody (not colliders),
		/// this method is needed to distribute collision events between Fragments.
		public void OnCollisionEnter(Collision col) {

			/// Make a list of all our colliders that were being touched.
			List<Collider> frags = new List<Collider>();
			foreach (ContactPoint point in col.contacts) {
				if (!frags.Contains(point.thisCollider)) {
					frags.Add(point.thisCollider);
				}
			}
			for (int i = 0; i < frags.Count; i++) {
				if (frags[i].GetComponent<Fragment>()) {
					frags[i].GetComponent<Fragment>().OnCollisionInRoot(col);
				}
			}

		}


		///  Use this for initialization as root.
		public override void Initialize() {

			base.Initialize();

			index = 0;

			/// Build initial adjacency matrix.
			BuildAdjacencyMatrix();
			BuildGroundFragmentsArray();

			/// As root, we also need Rigidbody.
			rb = gameObject.AddComponent<Rigidbody>();
			rb.isKinematic = true;

			/// Calculate initial stability values.
			CalculateMassPoint();
			CalculateSupportPoint();

		}


		///  Use this for re-initialization.
		public override void Reinitialize() {

			base.Reinitialize();

			index = 0;

			rb = GetComponent<Rigidbody>();

			BuildAdjacencyMatrix();

			/// Stability coroutine will recalculate stability values.
			StartCoroutine(StabilityCoroutine());

		}


		///  Creates adjacency matrix of fragments of this element, treating them like a graph.
		public override void BuildAdjacencyMatrix(bool isInitializing = true) {

			/// Initialize matrix.
			adjMatrix = new AdjacencyMatrix(len);

			/// For every 'vertex' (i.e. Fragment)...
			Transform tfrag;
			Collider[] neighbours;
			Mesh tmesh;

			for (int i = 0; i < len; i++) {
				tfrag = transform.GetChild(i);

				tmesh = tfrag.GetComponent<MeshFilter>().sharedMesh;

				/// Cast around fragment.
				neighbours = Physics.OverlapBox(
					tfrag.position,
					Vector3.Scale(tmesh.bounds.extents, tfrag.localScale * 1.01f),
					tfrag.rotation
				);

				/// Handle findings.
				for (int j = 0; j < neighbours.Length; j++) {
					/// If this fragment belongs to us...
					if (
						neighbours[j].GetComponent<Fragment>() != null &&
						neighbours[j].GetComponentInParent<Element>() == this
					) {
						adjMatrix[i, neighbours[j].GetComponent<Fragment>().index] = true;
					}
				}

				/// Exclude us from being connected to ourselves.
				adjMatrix[i, i] = false;

			}

		}


		///  Erases destroyed fragment from matrix.
		public override void MarkFragmentAsDestroyed(int findex) {

			base.MarkFragmentAsDestroyed(findex);

		}


		///  General entry point for checking existing connection groups of fragments.
		protected override void CheckConnectionsInElement() {

			Transform newElement;

			/// Initialize bool array for check.
			isVisited = new bool[len];
			System.Array.Copy(isDestroyed, isVisited, len);
			bool allAreVisited = true;
			bool someoneWasVisited = false;
			bool firstTime = true;

			/// If we're grounded (not divided in the air)...
			if (groundFragments != null) {
				/// We are trying to visit all fragments from ground points.
				int i = groundFragments.Count - 1;
				while (i >= 0) {

					isTempVisited = new bool[len];

					/// If groundFragment is present and was not visited in previous iterations...
					if (!isVisited[groundFragments[i].index]) {
						CheckConnections(groundFragments[i].index);
					}

					/// Trying to understand if we visited all Fragments or visited at least something...
					allAreVisited = true;
					someoneWasVisited = false;
					for (int j = 0; j < len; j++) {
						allAreVisited &= isVisited[j];
						someoneWasVisited |= isTempVisited[j];
					}

					if (allAreVisited) {
						/// get rid of buggy empty Elements.
						if (GetComponentsInChildren<Fragment>(false).Length == 0) {
							Destroy(this.gameObject);
						}

						/// If firstTime, nothing really happens, construction is still whole
						/// and we don't need to change anything. In other cases, re-initialize (for safety).
						if (groundFragments.Count == 0) {
							rb.isKinematic = false;
						}

						if (!firstTime) {
							Reinitialize();
						}

						return;
					} else {
						/// There may be cases when we didn't fire CheckConnections() method above,
						/// but still getting there, so we check if there was graph bypass.
						if (someoneWasVisited) {
							/// Create new grounded element with found fragments.
							newElement = new GameObject(
								"New Element", typeof(RootElement), typeof(Rigidbody)
							).transform;
							Element ne = newElement.GetComponent<Element>();
							ne.groundFragments = new List<Fragment>();

							for (int k = 0; k < len; k++) {
								if (isTempVisited[k]) {
									fragments[k].transform.SetParent(newElement);
									if (groundFragments != null) {
										/// Handle information about ground Fragments.
										if (groundFragments.Exists(f => f.index == fragments[k].index)) {
											ne.groundFragments.Add(fragments[k]);
											groundFragments.Remove(fragments[k]);
											i--;
										}
									}
								}
							}

							ne.Reinitialize();

							/// If we are not standing on the ground...
							if (ne.groundFragments.Count == 0) {
								ne.rb.velocity = rb.velocity;
								ne.rb.WakeUp();
							} else {
								ne.rb.isKinematic = true;
							}
							ne.rb.ResetCenterOfMass();

						}

						firstTime = false;
					}

					i--;
				}
			}

			/// Note: we came here only if there are remaining spare fragments without ground points.
			/// Now we check remaining Fragments (which we didn't find going from ground points).
			for (int i = 0; i < len; i++) {
				if (!isVisited[i]) {
					/// Found another spare fragment. Check its connections:
					isTempVisited = new bool[len];
					CheckConnections(fragments[i].index);

					/// After graph bypassing:
					allAreVisited = true;
					someoneWasVisited = false;
					for (int j = 0; j < len; j++) {
						allAreVisited &= isVisited[j];
						someoneWasVisited |= isTempVisited[j];
					}
					if (allAreVisited) {
						/// Maybe this condition is not needed... Just in case, okay?
						if (someoneWasVisited) {
							/// Re-new this Element (because these fragments were the last ones).
							Reinitialize();
							if (groundFragments == null || groundFragments.Count == 0) {
								rb.isKinematic = false;
								rb.WakeUp();
							}
							rb.ResetCenterOfMass();
						}
						break;
					} else {
						/// And this also... But let it be, let it be~...
						if (someoneWasVisited) {
							/// Someone's remaining; after creating an Element, search again.
							newElement = new GameObject(
								"New Element", typeof(RootElement), typeof(Rigidbody)
							).transform;
							Element ne = newElement.GetComponent<Element>();
							ne.groundFragments = new List<Fragment>();

							for (int k = 0; k < len; k++) {
								if (isTempVisited[k]) {
									fragments[k].transform.SetParent(newElement);
									if (groundFragments != null) {
										if (groundFragments.Exists(f => f.index == fragments[k].index)) {
											ne.groundFragments.Add(fragments[k]);
											groundFragments.Remove(fragments[k]);
										}
									}
								}
							}

							ne.Reinitialize();

							if (ne.groundFragments.Count == 0) {
								ne.rb.velocity = rb.velocity;
								ne.rb.WakeUp();
							} else {
								ne.rb.isKinematic = true;
							}
							ne.rb.ResetCenterOfMass();
						}
					}
				}
			}

			/// Some clean-ups.
			if (GetComponentsInChildren<Fragment>(false).Length == 0) {
				Destroy(this.gameObject);
			}
		}

	}

}