using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHUDRS.Destructibles  {

	///  Structure is a biggest possible Destructible. In consists of Constructions, which consist of Elements,
	/// which are made of Fragments, and all this sh*t is connected, and we have access to connection data so
	/// we can perform beautiful integrity checks. Nothing's really hard, is it? :D
	[RequireComponent(typeof(FragmentationSettings))]
	public class Structure : MonoBehaviour, IDestructible, IRootDestructible  {

		/// Array of our child Constructions.
		public Construction[] constructions;

		/// Adjacency matrix that represents connections of constructions in a convenient form.
		public AdjacencyMatrix adjMatrix;

		/// Flag that means that we need to do integrity check at the next frame.
		public bool connectionsFlag;

		/// Number of child Constructions. Is used everywhere.
		public int len;

		/// Array of visited Constructions. Is used in a whole graph bypassing.
		protected bool[] isVisited;

		/// Temp array that is cleared at every graph sub-pass.
		protected bool[] isTempVisited;

		/// List of our Constructions that have child Ground Elements (and then, are touching ground).
		public List<Construction> groundConstructions;

		/// This coroutine starts when 'connectionsFlag' is set to 'true', suspends for one frame
		/// and then does integrity check.
		private Coroutine checkWaiter;

		/// Our cached Rigidbody.
		public Rigidbody rb;

		/// Our 'point of mass'; is calculated by summing all mass points of child Constructions.
		public Vector3 massPoint;

		///
		public Vector3 supportPoint;

		/// This is length of a vector among calculated 'mass' point and 'support' point of Construction. 
		/// Increase this for more stability. Value '<= 0' means no stability check at all. You can set
		/// default value after '=' operator below.
		public float stabilityEdgeValue = 10f;

		/// Internal bool to store condition when we do not need, can't or not allowed to calculate stability
		/// values and check stability. 
		/// If you want to disable stability checks, set 'stabilityEdgeValue' <= 0.
		public bool noNeedToCheckStability  { get; private set; }

		/// Render mass and support points and gizmos for debug purposes?
		public bool showStabilityGizmos = true;

		/// Rigidbody magnitude saved from last fixed frame.
		protected float lastMagnitude;

		/// Absolute delta between lastMagnitude and current magnitude.
		public float deltaMagnitude  { get; set; }


		///  Use this for initialization.
		public void Initialize()  {

			/// Get rid of Constructions' scripts.
			/// Elements and fragments will be cleaned up automatically.
			Cleanup();

			/// Add Construction script to children and initialize them (again, order of operations is critical).
			Construction tconstr;
			for(int i = 0; i < transform.childCount; i++)  {
				tconstr = transform.GetChild(i).gameObject.AddComponent<StructConstruction>();
				tconstr.InitCreateScripts();
			}
			for(int i = 0; i < transform.childCount; i++)  {
				tconstr = transform.GetChild(i).GetComponent<StructConstruction>();
				tconstr.structure = this;
				tconstr.index = i;
				tconstr.InitInfo();
			}

			/// Get array of our constructions and get initial connections.
			constructions = GetComponentsInChildren<Construction>();
			len = constructions.Length;
			#if UNITY_EDITOR
			if(len > 100)
				Debug.LogWarning(
					"Structure "+this.name+" has more than 100 constructions.\n" +
					"Note that extremely big structures may lead to performance issues."
				);
			#endif

			if(groundConstructions == null)
				groundConstructions = new List<Construction>();
			else
				groundConstructions.Clear();

			/// Take connection data into adjacency matrix.
			adjMatrix = new AdjacencyMatrix(len);
			for(int i = 0; i < len; i++)  {
				for(int j = 0; j < constructions[i].len; j++)  {
					constructions[i].elements[j].connections.ForEach(c => {
						if(c.fragment1.element.construction != c.fragment2.element.construction)
							adjMatrix[c.fragment1.element.construction.index, c.fragment2.element.construction.index] = true;
					});
				}

				if(constructions[i].groundElements != null && constructions[i].groundElements.Count > 0)  {
					groundConstructions.Add(constructions[i]);
				}
			}

			rb = gameObject.AddComponent<Rigidbody>();
			rb.isKinematic = true;
			rb.ResetCenterOfMass();

			/// Calculate initial stability values.
			CalculateStabilityTransform();
			CalculateSupportCenter();

		}


		///  Use this for re-initialization.
		public void Reinitialize()  {

			/// Get child elements array.
			constructions = GetComponentsInChildren<Construction>();
			len = constructions.Length;
			if(groundConstructions == null)
				groundConstructions = new List<Construction>();
			else
				groundConstructions.Clear();

			for(int i = 0; i < len; i++)  {
				constructions[i].structure = this;
				constructions[i].index = i;
			}

			adjMatrix = new AdjacencyMatrix(len);
			/// Re-take info...
			for(int i = 0; i < len; i++)  {
				for(int j = 0; j < constructions[i].len; j++)  {
					constructions[i].elements[j].connections.ForEach(c => {
						if(c.fragment1.element.construction != c.fragment2.element.construction)
							adjMatrix[c.fragment1.element.construction.index, c.fragment2.element.construction.index] = true;
					});
				}

				if(constructions[i].groundElements != null && constructions[i].groundElements.Count > 0)  {
					groundConstructions.Add(constructions[i]);
				}
			}
			/// Re-calculate...
			StartCoroutine(StabilityCoroutine());

		}


		///  Cleans up all script stuff from constructions, elements and fragments.
		public void Cleanup()  {

			for(int i = 0; i < transform.childCount; i++)  {
				
				if(transform.GetChild(i).GetComponent<Construction>())  {
					
					transform.GetChild(i).GetComponent<Construction>().Cleanup();
					DestroyImmediate(transform.GetChild(i).GetComponent<Construction>());

				}

			}

			DestroyImmediate(GetComponent<Rigidbody>());

		}


		///  Adds this Structure to Update event at the start of lifetime.
		public void Start()  {
			
			TimeManager.UpdateStructures += UpdateStructure;

		}


		///  Calculates delta and last velocity every physical frame.
		public void FixedUpdate()  {

			if(rb != null)  {
				deltaMagnitude = Mathf.Abs(rb.velocity.sqrMagnitude - lastMagnitude);
				lastMagnitude = rb.velocity.sqrMagnitude;
			}

		}


		///  Removes this Structure from Update event at the end of lifetime.
		public void OnDestroy()  {
			
			TimeManager.UpdateStructures -= UpdateStructure;

		}


		///  For debug purposes.
		public void ShowAdjacencyMatrix()  {
			string s = "";
			for(int i = 0; i < len; i++)  {
				for(int j = 0; j < len; j++)  {
					s += string.Format(" {0}", System.Convert.ToByte(adjMatrix[i, j]));
				}
				s += "\n";
			}
			Debug.Log(s);
		}


		///  Is subscribed to an event and executes in the LateUpdate (delayed to frame 3).
		public void UpdateStructure()  {

			if(connectionsFlag)  {
				if(checkWaiter != null)
					StopCoroutine(checkWaiter);
				checkWaiter = StartCoroutine(StartCheckWaiter());
			}

		}


		///  A coroutine that will wait one frame before integrity check execution.
		public IEnumerator StartCheckWaiter()  {

			/// This will suspend the execution for one frame.
			yield return null;
			connectionsFlag = false;

			CheckConnectionsInStructure();

		}


		///  General entry point for checking existing connection groups of Constructions.
		private void CheckConnectionsInStructure()  {

			/// Some clean-up.
			if(GetComponentsInChildren<Construction>().Length == 0)
				Destroy(this.gameObject);

			/// Refresh data.
			Reinitialize();

			Transform newStructure;
			Structure newStructScript;

			/// Initialize bool array for check.
			isVisited = new bool[len];
			bool allAreVisited = true;
			bool someoneWasVisited = false;
			bool firstTime = true;

			/// If we're grounded (not divided in the air)...
			if(groundConstructions != null)  {
				/// We are trying to visit all elements from grounded ones.
				for(int i = 0; i < groundConstructions.Count; i++)  {
					isTempVisited = new bool[len];

					/// If groundConstruction was not visited in previous iterations...
					if(!isVisited[groundConstructions[i].index])
						CheckConnections(groundConstructions[i].index);
					
					allAreVisited = true;
					someoneWasVisited = false;
					for(int j = 0; j < len; j++)  {
						allAreVisited &= isVisited[j]; 
						someoneWasVisited |= isTempVisited[j];
					}

					if(allAreVisited)  {

						/// Some clean-ups.
						if(GetComponentsInChildren<Construction>().Length == 0)
							Destroy(this.gameObject);

						/// If firstTime, nothing really happens, Structure is still whole
						/// and we don't need to change anything. In other cases, re-initialize (for safety).
						if(!firstTime)  {
							Reinitialize();
							if(groundConstructions == null || groundConstructions.Count == 0)  {
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
							/// Create new grounded Structure with founded elements.
							newStructure = new GameObject(
								"New Structure", typeof(Structure), typeof(Rigidbody)
							).transform;

							/// Because it's better to change hierarchy as little as we can.
							int cnt = 0;
							for(int a = 0; a < len; a++)  {
								if(isTempVisited[a])
									cnt++;
							}
							if(cnt > len * 0.5f)  {
								for(int a = 0; a < len; a++)  {
									if(!(isVisited[i] ^ isTempVisited[i]))  {
										isVisited[a] = !isVisited[a];
										isTempVisited[a] = !isTempVisited[a];
									}
								}
							}

							for(int k = 0; k < len; k++)  {
								if(isTempVisited[k])  {
									constructions[k].transform.SetParent(newStructure);
									constructions[k].transform.SetAsLastSibling();
								}
							}
							newStructScript = newStructure.GetComponent<Structure>();
							newStructScript.rb = newStructure.GetComponent<Rigidbody>();
							newStructScript.Reinitialize();
							if(
								newStructScript.groundConstructions == null || 
								newStructScript.groundConstructions.Count == 0
							)  {
								newStructScript.rb.isKinematic = false;
								newStructScript.rb.WakeUp();
							}
							else  {
								newStructScript.rb.isKinematic = true;
							}
							newStructScript.rb.ResetCenterOfMass();
						}

						firstTime = false;
					}
				}
			}

			/// Note: we came here only if there are remaining spare Constructions w/o ground points.
			/// Now we check remaining Constructions (which we didn't find going from ground points).
			for(int i = 0; i < len; i++)  {
				if(!isVisited[i])  {
					/// Found another spare Construction. Check its connections:
					isTempVisited = new bool[len];
					CheckConnections(constructions[i].index);

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
							/// Re-new this Structure (because these elements were the last ones).
							Reinitialize();
							if(groundConstructions == null || groundConstructions.Count == 0)  {
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
							/// Someone's remaining; after creating a Structure, search again.
							newStructure = new GameObject(
								"New Structure", typeof(Structure), typeof(Rigidbody)
							).transform;

							/// Because it's better to change hierarchy as little as we can.
							int cnt = 0;
							for(int a = 0; a < len; a++)  {
								if(isTempVisited[a])
									cnt++;
							}
							if(cnt > len * 0.5f)  {
								for(int a = 0; a < len; a++)  {
									if(!(isVisited[i] ^ isTempVisited[i]))  {
										isVisited[a] = !isVisited[a];
										isTempVisited[a] = !isTempVisited[a];
									}
								}
							}

							for(int k = 0; k < len; k++)  {
								if(isTempVisited[k])  {
									constructions[k].transform.SetParent(newStructure);
									constructions[k].transform.SetAsLastSibling();
								}
							}

							newStructScript = newStructure.GetComponent<Structure>();
							newStructScript.rb = newStructure.GetComponent<Rigidbody>();
							newStructScript.Reinitialize();
							newStructScript.rb.ResetCenterOfMass();
							newStructScript.rb.velocity = rb.velocity;
							rb.ResetCenterOfMass();

						}
					}
				}
			}

			/// Some clean-ups.
			if(GetComponentsInChildren<Construction>().Length == 0)
				Destroy(this.gameObject);

		}


		///  Tries to recursively visit all Structures from 'start' using connections matrix.
		private void CheckConnections(int start)  {

			isVisited[start] = true;
			isTempVisited[start] = true;
			for(int i = 0; i < len; i++)  {
				if(adjMatrix[start, i] && !isVisited[i])  {
					CheckConnections(i);
				}
			}

		}


		///  User entry point. Removes all connection data, so Structure turns in a rigid 'house of cards'.
		/// (can be accessed thorugh interface)
		public void TurnIntoDestructibleWithoutConnections()  {

			for(int i = 0; i < len; i++)  {
				for(int j = 0; j < constructions[i].len; j++)  {
					constructions[i].elements[j].connections = null;
					constructions[i].elements[j].connectionsFlag = true;
				}
			}

		}


		/******  STABILITY MANAGING METHODS  ******/


		///  Sums all mass point positions of our Constructions and divides the sum by a Construction count.
		public void CalculateStabilityTransform()  {

			Vector3 point = new Vector3();
			for(int i = 0; i < len; i++)  {
				point += constructions[i].massPoint;
			}
			point /= len;
			massPoint = point;

		}


		///  Sums all support point positions of our Constructions and divides the sum by a Construction count.
		public void CalculateSupportCenter()  {

			Vector3 point = new Vector3();

			for(int i = 0; i < groundConstructions.Count; i++)  {
				point += groundConstructions[i].supportPoint;
			}

			point /= groundConstructions.Count;
			point.Set(point.x, massPoint.y, point.z);
			supportPoint = point;

		}


		///  Just a method that can wait some time before executing calculations and checks about stability.
		protected IEnumerator StabilityCoroutine()  {

			if(noNeedToCheckStability)
				yield break;

			if(stabilityEdgeValue <= 0f)  {
				noNeedToCheckStability = true;
				yield break;
			}

			/// Calculate stability values.
			CalculateStabilityTransform();
			CalculateSupportCenter();

			/// Wait for 30 frames...
			for(int i = 0; i < 30; i++)  {
				yield return null;
			}

			/// Perform the check and re-build fragments hierachy.
			if(StructureIsUnstable())  {
				PerformDetachment();
			}

		}


		///  True, if mass point and support point are too far from each other.
		protected bool StructureIsUnstable()  {
			
			if((massPoint - supportPoint).sqrMagnitude > Mathf.Pow(stabilityEdgeValue, 2f))
				return true;
			else
				return false;

		}


		///  User entry point. Completely detaches Structure from the ground.
		/// (can be accessed through interface)
		public void PerformDetachment()  {

			if(groundConstructions != null)  {

				for(int l = 0; l < groundConstructions.Count; l++)  {

					for(int k = 0; k < groundConstructions[l].groundElements.Count; k++)  {

						for(int i = 0; i < groundConstructions[l].groundElements[k].groundFragments.Count; i++)  {

							for(
								int j = groundConstructions[l].groundElements[k].groundFragments[i].index + 1; 
								j < groundConstructions[l].groundElements[k].len; 
								j++
							)  {

								if(!groundConstructions[l].groundElements[k].groundFragments.Exists(
									f => f.index == j
								))  {

									/// If you want to destroy some Fragments which are close to ground Fragments
									/// which we are going to detach, uncomment the following:
									/*if(groundConstructions[l].groundElements[k].adjMatrix[
										groundConstructions[l].groundElements[k].groundFragments[i].index, j
									] ||
									groundConstructions[l].groundElements[k].adjMatrix[
										j, groundConstructions[l].groundElements[k].groundFragments[i].index
									])  {
										groundConstructions[l].groundElements[k].fragments[j].DestroyFragment();
									}*/

									groundConstructions[l].groundElements[k].adjMatrix[
										groundConstructions[l].groundElements[k].groundFragments[i].index, j
									] = false;

									groundConstructions[l].groundElements[k].adjMatrix[
										j, groundConstructions[l].groundElements[k].groundFragments[i].index
									] = false;

									groundConstructions[l].groundElements[k].connectionsFlag = true;
								}

							}

						}

						groundConstructions[l].groundElements[k].connections.RemoveAll(
							c => c.fragment1.element.construction != c.fragment2.element.construction
						);

					}

				}

			}

		}


		#if UNITY_EDITOR
		/// Displays massPoint and supportPoint as cubes for debug purposes.
		public void OnDrawGizmos()  {

			if(showStabilityGizmos)  {
				
				Gizmos.color = Color.white;
				Gizmos.DrawWireCube(massPoint, Vector3.one * 1.5f);
				Gizmos.color = Color.blue;
				Gizmos.DrawWireCube(supportPoint, Vector3.one * 1.5f);

				Gizmos.color = Color.white - new Color(0f, 0f, 0f, 0.5f);
				Gizmos.DrawCube(massPoint, Vector3.one * 1.5f);
				Gizmos.color = Color.blue - new Color(0f, 0f, 0f, 0.5f);
				Gizmos.DrawCube(supportPoint, Vector3.one * 1.5f);

			}

		}
		#endif

	}

}