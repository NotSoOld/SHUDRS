using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS.Destructibles  {

	///  Type of Construction which is a part of a Structure (it usually has several of these).
	public class StructConstruction : Construction, IDestructible  {


		///  General entry point for checking existing connection groups of elements.
		protected override void CheckConnectionsInConstruction()  {

			/// Some clean-up.
			if(GetComponentsInChildren<Element>(false).Length == 0) {
				Destroy(this.gameObject);
			}

			/// Refresh data.
			Reinitialize();

			Transform newConstruction;
			Construction constrScript;

			/// Initialize bool array for check.
			isVisited = new bool[len];
			bool allAreVisited = true;
			bool someoneWasVisited = false;
			bool firstTime = true;

			/// If we're grounded (not divided in the air)...
			if(groundElements != null)  {
				/// We are trying to visit all elements from grounded ones.
				for(int i = 0; i < groundElements.Count; i++)  {
					isTempVisited = new bool[len];

					/// If groundElement was not visited in previous iterations...
					if(!isVisited[groundElements[i].index]) {
						CheckConnections(groundElements[i].index);
					}

					allAreVisited = true;
					someoneWasVisited = false;
					for(int j = 0; j < len; j++)  {
						allAreVisited &= isVisited[j]; 
						someoneWasVisited |= isTempVisited[j];
					}

					if(allAreVisited)  {

						/// Some clean-ups.
						if(GetComponentsInChildren<Element>().Length == 0) {
							Destroy(this.gameObject);
						}

						/// If firstTime, nothing really happens, Construction is still whole
						/// and we don't need to change anything. In other cases, re-initialize (for safety).
						if (!firstTime)  {
							Reinitialize();
							structure.connectionsFlag = true;
						}

						return;

					}
					else  {
						/// There may be cases when we didn't fire CheckConnections() method above,
						/// but still getting there, so we check if there was graph bypass.
						if(someoneWasVisited)  {
							/// Create new grounded construction with founded elements.
							newConstruction = new GameObject(
								"New Construction", typeof(StructConstruction)
							).transform;
							constrScript = newConstruction.GetComponent<Construction>();
							bool isRoot = true;

							for(int k = 0; k < len; k++)  {
								if(isTempVisited[k])  {
									elements[k].transform.SetParent(newConstruction);
								}
							}

							/// After moving all Elements, handle information about connections.
							for(int k = 0; k < len; k++)  {
								if(isTempVisited[k])  {
									elements[k].connections.ForEach(c => {
										if(c.fragment1.element.construction != c.fragment2.element.construction) {
											isRoot = false;
										}
									}
									);
									if(!isRoot) {
										break;
									}
								}
							}

							/// If we don't reassign any connection, this construction is now floating.
							if(isRoot)  {
								/// Cast to RootConstruction and reinitialize.
								RootConstruction rc = CastFromStructToRootConstruction(newConstruction);
								rc.Reinitialize();

								rc.rb = rc.GetComponent<Rigidbody>();
								if(rc.groundElements == null || rc.groundElements.Count == 0)  {
									rc.rb.velocity = structure.rb.velocity;
									rc.rb.isKinematic = false;
									rc.rb.WakeUp();
								}
								rc.rb.ResetCenterOfMass();

								Destroy(newConstruction.gameObject);
							}
							else  {
								/// Else attach us [back] to structure.
								newConstruction.SetParent(structure.transform);
								newConstruction.SetAsLastSibling();
								constrScript.Reinitialize();
							}

						}

						firstTime = false;
					}
				}
			}

			/// Note: we came here only if there are remaining spare elements w/o ground points.
			/// Now we check remaining elements (which we didn't find going from ground points).
			for(int i = 0; i < len; i++)  {
				if(!isVisited[i])  {
					/// Found another spare element. Check its connections:
					isTempVisited = new bool[len];
					CheckConnections(elements[i].index);

					/// After graph bypassing:
					allAreVisited = true;
					someoneWasVisited = false;
					for(int j = 0; j < len; j++)  {
						allAreVisited &= isVisited[j]; 
						someoneWasVisited |= isTempVisited[j];
					}
					if(allAreVisited)  {
						/// Maybe this condition is not needed... Just in case, okay?
						if(someoneWasVisited)  {
							/// Re-new this Construction (because these elements were the last ones).
							Reinitialize();
						}
						break;
					}
					else  {
						/// And this also... But let it be, let it be~...
						if(someoneWasVisited)  {
							/// Someone's remaining; after creating a Construction, search again.
							newConstruction = new GameObject(
								"New Construction", typeof(StructConstruction)
							).transform;
							constrScript = newConstruction.GetComponent<Construction>();
							bool isRoot = true;

							for(int k = 0; k < len; k++)  {
								if(isTempVisited[k])  {
									elements[k].transform.SetParent(newConstruction);
								}
							}

							for(int k = 0; k < len; k++)  {
								if(isTempVisited[k])  {
									elements[k].connections.ForEach(c => {
										if(c.fragment1.element.construction != c.fragment2.element.construction) {
											isRoot = false;
										}
									}
									);
									if(!isRoot) {
										break;
									}
								}
							}

							/// If we don't reassign any connection, this construction is now floating.
							if(isRoot)  {
								/// Cast to RootConstruction and reinitialize.
								RootConstruction rc = CastFromStructToRootConstruction(newConstruction);
								rc.Reinitialize();

								rc.rb = rc.GetComponent<Rigidbody>();
								if(rc.groundElements == null || rc.groundElements.Count == 0)  {
									rc.rb.velocity = structure.rb.velocity;
									rc.rb.isKinematic = false;
									rc.rb.WakeUp();
								}
								rc.rb.ResetCenterOfMass();

								Destroy(newConstruction.gameObject);
							}
							else  {
								/// Else attach us [back] to structure.
								newConstruction.SetParent(structure.transform);
								newConstruction.SetAsLastSibling();
								constrScript.Reinitialize();
							}

						}
					}
				}
			}

			/// Maybe we are floating now too?
			bool isFloating = true;
			for(int k = 0; k < transform.childCount; k++)  {
				if(transform.GetChild(k).GetComponent<Element>()) {
					transform.GetChild(k).GetComponent<Element>().connections.ForEach(c => {
						if(c.fragment1.element.construction != c.fragment2.element.construction) {
		isFloating = false;
	}
}
					);
				}

				if (!isFloating) {
					break;
				}
			}

			if(isFloating)  {

				/// Some clean-ups.
				if(GetComponentsInChildren<Element>().Length == 0) {
					Destroy(this.gameObject);
				}

				/// Cast to RootElement and reinitialize.
				RootConstruction rc = CastFromStructToRootConstruction(transform);
				rc.Reinitialize();

				rc.rb = rc.GetComponent<Rigidbody>();
				if(rc.groundElements == null || rc.groundElements.Count == 0)  {
					rc.rb.velocity = structure.rb.velocity;
					rc.rb.isKinematic = false;
					rc.rb.WakeUp();
				}
				rc.rb.ResetCenterOfMass();

				Destroy(this.gameObject);
			}

			/// Now we can check further.
			structure.connectionsFlag = true;

			/// Some clean-ups.
			if(GetComponentsInChildren<Element>().Length == 0) {
				Destroy(this.gameObject);
			}
		}


		/// Casts from StructConstruction to RootConstruction.
		public RootConstruction CastFromStructToRootConstruction(Transform structConstructionToCast)  {
			
			RootConstruction newRootConstruction = new GameObject(
				"Root Construction", typeof(RootConstruction), typeof(Rigidbody)
			).GetComponent<RootConstruction>();
			for(int i = structConstructionToCast.transform.childCount - 1; i >= 0; i--)  {
				if(structConstructionToCast.transform.GetChild(i).gameObject.activeSelf) {
					structConstructionToCast.transform.GetChild(i).SetParent(newRootConstruction.transform);
				}
			}
			/// It must be so by default.
			newRootConstruction.GetComponent<Rigidbody>().isKinematic = true;

			return newRootConstruction;

		}
		
	}

}
