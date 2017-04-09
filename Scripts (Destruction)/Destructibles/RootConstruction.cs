using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS.Destructibles  {

	///  A Destructible with type of a Construction which doesn't belong to any Structure.
	[RequireComponent(typeof(FragmentationSettings))]
	public class RootConstruction : Construction, IDestructible, IRootDestructible  {


		/// Rigidbody magnitude saved from last fixed frame.
		protected float lastMagnitude;

		/// Absolute delta between lastMagnitude and current magnitude.
		public float deltaMagnitude  { get; set; }


		///  Calculates delta and last velocity every physical frame.
		public void FixedUpdate()  {

			if(rb != null)  {
				deltaMagnitude = Mathf.Abs(rb.velocity.sqrMagnitude - lastMagnitude);
				lastMagnitude = rb.velocity.sqrMagnitude;
			}

		}


		///  All Destructibles need Time Manager to work. If we don't have one - create it.
		public void Awake()  {

			if(GameObject.FindObjectsOfType<TimeManager>().Length == 0)  {
				new GameObject("Time Manager", typeof(TimeManager));
			}

		}


		///  Use this for initialization (only for inside-inspector usage!).
		public void Initialize()  {

			InitCreateScripts();
			InitInfo();

			rb = gameObject.AddComponent<Rigidbody>();
			rb.isKinematic = true;

		}


		///  General entry point for checking existing connection groups of elements.
		protected override void CheckConnectionsInConstruction()  {

			/// Some clean-up.
			if(GetComponentsInChildren<Element>(false).Length == 0)
				Destroy(this.gameObject);

			/// Refresh data.
			Reinitialize();

			Transform newConstruction;
			Construction newConstrScript;

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
					if(!isVisited[groundElements[i].index])
						CheckConnections(groundElements[i].index);

					allAreVisited = true;
					someoneWasVisited = false;
					for(int j = 0; j < len; j++)  {
						allAreVisited &= isVisited[j]; 
						someoneWasVisited |= isTempVisited[j];
					}

					if(allAreVisited)  {

						/// Some clean-ups.
						if(GetComponentsInChildren<Element>().Length == 0)
							Destroy(this.gameObject);

						/// If firstTime, nothing really happens, Construction is still whole
						/// and we don't need to change anything. In other cases, re-initialize (for safety).
						if(!firstTime)  {
							Reinitialize();
							if(groundElements == null || groundElements.Count == 0)  {
								rb.isKinematic = false;
								rb.WakeUp();
							}
						}
						rb.ResetCenterOfMass();
						return;

					}
					else  {
						/// There may be cases when we didn't fire CheckConnections() method above,
						/// but still getting there, so we check if there was graph bypass.
						if(someoneWasVisited)  {
							/// Create new grounded construction with founded elements.
							newConstruction = new GameObject(
								"New Construction", typeof(RootConstruction), typeof(Rigidbody)
							).transform;
							for(int k = 0; k < len; k++)  {
								if(isTempVisited[k])
									elements[k].transform.SetParent(newConstruction);
							}
							newConstrScript = newConstruction.GetComponent<Construction>();
							newConstrScript.rb = newConstruction.GetComponent<Rigidbody>();
							newConstrScript.Reinitialize();
							newConstrScript.rb.isKinematic = true;
							newConstrScript.rb.ResetCenterOfMass();
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
							if(groundElements == null || groundElements.Count == 0)  {
								rb.isKinematic = false;
								rb.WakeUp();
							}
							rb.ResetCenterOfMass();
						}
						break;
					}
					else  {
						/// And this also... But let it be, let it be~...
						if(someoneWasVisited)  {
							/// Someone's remaining; after creating a Construction, search again.
							newConstruction = new GameObject(
								"New Construction", typeof(RootConstruction), typeof(Rigidbody)
							).transform;
							for(int k = 0; k < len; k++)  {
								if(isTempVisited[k])
									elements[k].transform.SetParent(newConstruction);
							}

							newConstrScript = newConstruction.GetComponent<Construction>();
							newConstrScript.rb = newConstruction.GetComponent<Rigidbody>();
							newConstrScript.Reinitialize();
							newConstrScript.rb.ResetCenterOfMass();
							newConstrScript.rb.velocity = rb.velocity;
							rb.ResetCenterOfMass();

						}
					}
				}
			}

			/// Some clean-ups.
			if(GetComponentsInChildren<Element>().Length == 0)
				Destroy(this.gameObject);

		}


	}

}