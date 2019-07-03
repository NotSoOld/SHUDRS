using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS.Destructibles {

	///  ConstrElement is an Element-typed container for Fragments. Construction usually has a plenty of this.
	/// If ConstrElement doesn't belong to Construction, it becomes RootElement.
	public class ConstrElement : Element, IDestructible {

		///  Gets called to initialize element when signal comes from construction.
		public override void Initialize() {

			base.Initialize();

			index = transform.GetSiblingIndex();
			construction = GetComponentInParent<Construction>();

		}


		///  Use this for re-initialization.
		public override void Reinitialize() {

			base.Reinitialize();

			index = transform.GetSiblingIndex();
			construction = GetComponentInParent<Construction>();

			BuildAdjacencyMatrix(false);

			/// Stability coroutine will recalculate stability values.
			StartCoroutine(StabilityCoroutine());

		}


		///  Creates adjacency matrix of fragments of this element, treating them like a graph.
		public override void BuildAdjacencyMatrix(bool isInitializing = true) {

			/// Initialize matrix.
			adjMatrix = new AdjacencyMatrix(len);

			if (isInitializing) {
				if (connections == null) {
					connections = new List<Connection>();
				} else {
					connections.Clear();
				}
			}

			/// For every 'vertex' (i.e. Fragment)...
			Transform tfrag;
			Collider[] neighbours;
			Mesh tmesh;
			Connection connection;

			for (int i = 0; i < len; i++) {
				tfrag = transform.GetChild(i);
				tmesh = tfrag.GetComponent<MeshFilter>().sharedMesh;

				/// Cast around Fragment.
				neighbours = Physics.OverlapBox(
					tfrag.position,
					Vector3.Scale(tmesh.bounds.extents, tfrag.localScale * 1.05f),
					tfrag.rotation
				);

				/// Handle findings.
				for (int j = 0; j < neighbours.Length; j++) {
					/// If this Fragment belongs to us...
					if (neighbours[j].GetComponent<Fragment>() != null) {
						if (neighbours[j].GetComponentInParent<Element>() == this) {
							adjMatrix[i, neighbours[j].GetComponent<Fragment>().index] = true;
						}
						/// If we mean to have neighbour elements...
						else {
							if (isInitializing) {
								/// Build some connection relations.
								connection = new Connection();
								connection.fragment1 = transform.GetChild(i).GetComponent<Fragment>();
								connection.fragment2 = neighbours[j].GetComponent<Fragment>();
								Debug.Log("New connection with fragment1 = " + connection.fragment1.name + ", fragment2 = " + connection.fragment2.name);
								connections.Add(connection);
							}
						}
					}
				}

				/// Exclude us from being connected to us.
				adjMatrix[i, i] = false;
			}

		}


		///  Erases destroyed Fragment from matrix.
		public override void MarkFragmentAsDestroyed(int findex) {

			base.MarkFragmentAsDestroyed(findex);

			/// If it was one of connection Fragments, delete all connections with it from list.
			for (int i = 0; i < connections.Count; i++) {
				if (
					connections[i].fragment1.index == findex &&
					connections[i].fragment2.element != null &&
					connections[i].fragment2.element.connections != null
				) {
					int ind = connections[i].fragment2.element.connections.FindIndex(
						c => c.fragment2.element == this && c.fragment2.index == findex
					);
					if (ind != -1) {
						connections[i].fragment2.element.connections.RemoveAt(ind);
					}

					if (connections[i].fragment2.element.connections.Count == 0) {
						/// Cast .element2 to RootElement.
						RootElement re = CastFromConstrToRootElement(connections[i].fragment2.element.transform);
						re.Reinitialize();

						re.rb = re.GetComponent<Rigidbody>();
						if (re.groundFragments == null || re.groundFragments.Count == 0) {
							re.rb.velocity = GetComponentInParent<Rigidbody>().velocity;
							re.rb.isKinematic = false;
							re.rb.WakeUp();
						}
						re.rb.ResetCenterOfMass();

						Destroy(connections[i].fragment2.element.gameObject);
					}
				}
			}
			/// If we really erase some connections above, this flag will raise integrity check in our Construction.
			smthChanged = (connections.RemoveAll(
				c => c.fragment1.element == this && c.fragment1.index == findex
			) == 0) ? (false) : (true);

		}


		/// General entry point for checking existing connection groups of fragments.
		protected override void CheckConnectionsInElement() {

			Transform newElement;
			Element elemscript;
			RootElement re;

			/// Initialize bool array for check.
			isVisited = new bool[len];
			System.Array.Copy(isDestroyed, isVisited, len);
			bool allAreVisited = true;
			bool someoneWasVisited = false;
			bool firstTime = true;

			/// Switch renderers at this point ('cause we're likely damaged).
			construction.SwitchRenderers();

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

					allAreVisited = true;
					someoneWasVisited = false;
					for (int j = 0; j < len; j++) {
						allAreVisited &= isVisited[j];
						someoneWasVisited |= isTempVisited[j];
					}

					if (allAreVisited) {
						/// If only we are present in construction:
						if (
							construction.GetComponentsInChildren<Element>().Length == 1 &&
							GetComponentsInChildren<Fragment>(false).Length == 0
						) {
							Destroy(construction.gameObject);
						}

						/// Get rid of buggy empty Elements.
						if (GetComponentsInChildren<Fragment>(false).Length == 0) {
							Destroy(this.gameObject);
						}

						/// If firstTime, nothing really happens, element is still whole
						/// and we don't need to change anything. In other cases, re-initialize (for safety).
						if (!firstTime) {
							Reinitialize();
						}
						if (smthChanged) {
							construction.connectionsFlag = true;
						}

						return;
					} else {
						/// There may be cases when we didn't fire CheckConnections() method above,
						/// but still getting there, so we check if there was graph bypass.
						if (someoneWasVisited) {
							/// Create new grounded element with found fragments.
							newElement = new GameObject(
								"New Element", typeof(ConstrElement)
							).transform;
							elemscript = newElement.GetComponent<Element>();
							elemscript.connections = new List<Connection>(connections.Capacity);
							elemscript.groundFragments = new List<Fragment>();
							bool isRoot = true;

							for (int k = 0; k < len; k++) {
								if (isTempVisited[k]) {
									fragments[k].transform.SetParent(newElement);
									/// Handle ground connections.
									if (groundFragments.Exists(f => f.index == fragments[k].index)) {
										elemscript.groundFragments.Add(fragments[k]);
										groundFragments.Remove(fragments[k]);
										i--;
									}
									/// Handle frag-to-frag connections.
									for (int l = connections.Count - 1; l >= 0; l--) {
										/// If this Fragment belonged to us but now it is part of newElement...
										if (connections[l].fragment1 == fragments[k]) {

											elemscript.connections.Add(connections[l]);
											connections.RemoveAt(l);
											isRoot = false;

										}
									}
								}
							}

							/// If we don't reassign any connection, this element is now floating.
							if (isRoot) {
								/// Cast to RootElement and reinitialize.
								re = CastFromConstrToRootElement(newElement);
								re.groundFragments = new List<Fragment>();
								re.groundFragments.AddRange(elemscript.groundFragments);
								re.Reinitialize();

								re.rb = re.GetComponent<Rigidbody>();
								if (re.groundFragments == null || re.groundFragments.Count == 0) {
									re.rb.velocity = GetComponentInParent<Rigidbody>().velocity;
									re.rb.isKinematic = false;
									re.rb.WakeUp();
								}
								re.rb.ResetCenterOfMass();

								Destroy(newElement.gameObject);
							} else {
								/// Else attach us [back] to construction.
								newElement.SetParent(construction.transform);
								newElement.SetAsLastSibling();
								elemscript.Reinitialize();
							}

						}

						firstTime = false;
					}

					i--;
				}
			}

			/// Note: we came here only if there are remaining spare fragments w/o ground points.
			/// Now we check remaining fragments (which we didn't find going from ground points).
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
						}
						break;
					} else {
						/// And this also... But let it be, let it be~...
						if (someoneWasVisited) {
							/// Someone's remaining; after creating an Element, search again.
							newElement = new GameObject(
								"New Element", typeof(ConstrElement)
							).transform;
							elemscript = newElement.GetComponent<Element>();
							elemscript.connections = new List<Connection>(connections.Capacity);
							elemscript.groundFragments = new List<Fragment>();
							bool isRoot = true;

							for (int k = 0; k < len; k++) {
								if (isTempVisited[k]) {
									fragments[k].transform.SetParent(newElement);

									if (groundFragments.Exists(f => f.index == fragments[k].index)) {
										elemscript.groundFragments.Add(fragments[k]);
										groundFragments.Remove(fragments[k]);
									}

									for (int l = connections.Count - 1; l >= 0; l--) {
										/// If this fragment belonged to us but now it is part of newElement...
										if (connections[l].fragment1 == fragments[k]) {

											elemscript.connections.Add(connections[l]);
											connections.RemoveAt(l);
											isRoot = false;

										}
									}
								}
							}

							/// If we don't reassign any connection, newElement is now floating.
							if (isRoot) {
								/// Cast to RootElement and reinitialize.
								re = CastFromConstrToRootElement(newElement);
								re.groundFragments = new List<Fragment>();
								re.groundFragments.AddRange(elemscript.groundFragments);
								re.Reinitialize();

								re.rb = re.GetComponent<Rigidbody>();
								if (re.groundFragments == null || re.groundFragments.Count == 0) {
									re.rb.velocity = GetComponentInParent<Rigidbody>().velocity;
									re.rb.isKinematic = false;
									re.rb.WakeUp();
								}
								re.rb.ResetCenterOfMass();

								Destroy(newElement.gameObject);
							} else {
								/// Else attach us [back] to construction.
								newElement.SetParent(construction.transform);
								newElement.SetAsLastSibling();
								elemscript.Reinitialize();
							}
						}
					}
				}
			}

			/// Maybe we are floating now too?
			if (connections.Count == 0) {
				/// Cast to RootElement and reinitialize.
				re = CastFromConstrToRootElement(transform);
				if (groundFragments != null && groundFragments.Count != 0) {
					re.groundFragments = new List<Fragment>();
					re.groundFragments.AddRange(groundFragments);
				}
				re.Reinitialize();

				re.rb = re.GetComponent<Rigidbody>();
				if (re.groundFragments == null || re.groundFragments.Count == 0) {
					re.rb.velocity = GetComponentInParent<Rigidbody>().velocity;
					re.rb.isKinematic = false;
					re.rb.WakeUp();
				}
				re.rb.ResetCenterOfMass();

				Destroy(this.gameObject);
			}

			/// Now we can reinitialize.
			construction.connectionsFlag = true;

			/// Some clean-ups.
			if (GetComponentsInChildren<Fragment>(false).Length == 0) {
				Destroy(this.gameObject);
			}
		}


		/// Casts from ConstrElement to RootElement.
		public RootElement CastFromConstrToRootElement(Transform constrElementToCast) {

			RootElement newRootElement = new GameObject(
				"RootElement", typeof(RootElement), typeof(Rigidbody)
			).GetComponent<RootElement>();
			for (int i = constrElementToCast.transform.childCount - 1; i >= 0; i--) {
				if (constrElementToCast.transform.GetChild(i).gameObject.activeSelf) {
					constrElementToCast.transform.GetChild(i).SetParent(newRootElement.transform);
				}
			}
			/// It must be so by default.
			newRootElement.GetComponent<Rigidbody>().isKinematic = true;

			return newRootElement;

		}

	}

}