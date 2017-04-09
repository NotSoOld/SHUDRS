using UnityEngine;
using System.Linq;

namespace SHUDRS.Destructibles  {

	///  The smallest part of a Destructible (and the only one that is physically represented, because it has a collider).
	[RequireComponent(typeof(MeshCollider))]
	public class Fragment : MonoBehaviour {	

		/// Destroyed Fragment flag (maybe sometimes it can be useful).
		public bool isDestroyed;

		/// Unique index of Fragment.
		public int index;

		/// Parent Element which we belong to.
		public Element element;

		/// Note that these fragmentation settings are the first object searching in parents in the hierachy.
		public FragmentationSettings settings;

		/// Sometimes, when this Fragment is supposed to move because of damage, it should look damaged.
		/// You can put damaged model of this Fragment here (without collider and rigidbody).
		public GameObject damagedGO;

		/// If true, this Fragment will still participate in various calculations (system will treat it as a part of
		/// a Destructible), but Fragment itself will be indestructible (like armature, etc.).
		public bool isIndestructible;

		/// "Health" of the Fragment at the start (this is visible in Inspector).
		public float startHealth = 100f;

		/// "Health" of the Fragment during runtime (this is visible only from code).
		public float currenthealth;


		///  Just for some actions happened at very beginning.
		public void Start()  {

			currenthealth = startHealth;

		}


		///  Use this for initialization.
		public void Initialize()  {
			
			/// Set up collider (is added automatically by [RequireComponent]).
			GetComponent<MeshCollider>().convex = true;

			/// Set unique index.
			index = transform.GetSiblingIndex();

			/// Find references to Element and first Fragmentation settings.
			element = GetComponentInParent<Element>();
			settings = GetComponentInParent<FragmentationSettings>();

		}


		///  Use this for re-initialization.
		public void Reinitialize()  {

			/// Set unique index.
			index = transform.GetSiblingIndex();

			/// Update references to Element and first Fragmentation settings.
			element = GetComponentInParent<Element>();
			settings = GetComponentInParent<FragmentationSettings>();

		}


		/// Returns true if object is from 'list of objects that must trigger OnCollisionEnter event'.
		private bool ContainsTagOrNameSubstringToMove(string tag, string name)  {

			if(settings.tagsToMove.Contains(tag))  {
				return true;
			}
			else  {
				for(int i = 0; i < settings.nameSubstringsToMove.Length; i++)  {
					if(name.Contains(settings.nameSubstringsToMove[i]))  {
						return true;
					}
				}
			}

			return false;

		}
			

		///  This event is triggered when: 
		/// - we collide with something; 
		/// - something collides with us; 
		/// - other Destructible collides with us.
		public void OnCollisionEnter(Collision other)  {

			/// We should do this only if we are destructible Fragment.
			if(!isIndestructible)  {

				/// If we need to react to external collisions...
				if(ContainsTagOrNameSubstringToMove(other.gameObject.tag, other.gameObject.name))  {

					/// We are moving now. :D
					DestroyFragment(true);

				}
					
				/// We may hit something with enough velocity to break ourselves while falling.
				if(GetComponentInParent<IRootDestructible>().deltaMagnitude > 40f)  {
					
					DestroyFragment();
					GetComponentInParent<Rigidbody>().velocity *= 0.8f;

				}

			}

			/// Or something may hit us in the same conditions.
			/// If it was another Destructible...
			if(other.collider.GetComponentInParent<IRootDestructible>() != null)  {
				if(other.collider.GetComponentInParent<IRootDestructible>().deltaMagnitude > 40f)  {

					other.collider.GetComponent<Fragment>().DestroyFragment();
					other.collider.GetComponentInParent<Rigidbody>().velocity *= 0.8f;

				}
			}

		}
			

		///  User entry point for destroying individual fragments.
		/// - may be called without parameters when a projectile hits this Fragment to perform
		/// destruction; you need to manually spawn debris burst;
		/// - may be called with a 'true' parameter when you want to move Fragments out of the Element because
		/// of some interaction that doesn't trigger 'OnCollisionEnter' event; debris will be spawned automatically.
		public void DestroyFragment(bool moveInsteadOfBreak = false)  {
			
			/// Mark us as destroyed.
			isDestroyed = true;

			/// If we supposed to move out of Element instead of breaking into debris...
			if(moveInsteadOfBreak)  {

				if(damagedGO != null)  {
					GetComponent<MeshCollider>().sharedMesh = damagedGO.GetComponent<MeshFilter>().sharedMesh;
					GetComponent<MeshFilter>().sharedMesh = damagedGO.GetComponent<MeshFilter>().sharedMesh;
					GetComponent<MeshRenderer>().sharedMaterials = damagedGO.GetComponent<MeshRenderer>().sharedMaterials;
				}
				Rigidbody rb = gameObject.AddComponent<Rigidbody>();
				rb.velocity = GetComponentInParent<Rigidbody>().velocity;
				transform.parent = null;
				SpawnGoDownDebris();
				
			}
			else  {
				
				gameObject.SetActive(false);
				transform.SetAsLastSibling();

			}

			/// Send message to parent element that we need to change renderers (if we really need, element will decide it).
			element.SwitchRenderers();

			/// Correct adjacency matrix and other variables that describe our integrity.
			element.MarkFragmentAsDestroyed(index);

			/// Send message to parent element that we need to check connections.
			element.connectionsFlag = true;

		}


		///  <summary>
		///  Spawns directional debris burst. Call this in your projectile script when it collides with a Destructible.
		///  </summary>
		///  <param name="dir">Direction of the projectile flight at the moment of collision.</param>
		///  <param name="speed">Speed of the projectile at the moment of collision.</param>
		public void SpawnDirectionalDebris(Vector3 dir, float speed)  {

			if(settings.directionalDebris != null)  {
				
				ParticleSystem debris = Instantiate(
					settings.directionalDebris, 
					transform.position, 
					Quaternion.Euler(dir)
				).GetComponent<ParticleSystem>();
				ParticleSystem.MainModule m = debris.main;
				m.startSpeedMultiplier = speed;

				ParticleSystemRenderer debrisRenderer = debris.GetComponent<ParticleSystemRenderer>();
				debrisRenderer.mesh = GetComponent<MeshFilter>().sharedMesh;
				debrisRenderer.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;

				debris.Play(true);

			}

		}


		///  Spawns bits of debris that just fall down. Useful for fragments moving out of the Element.
		public void SpawnGoDownDebris()  {

			if(settings.goDownDebris != null)  {

				ParticleSystem debris = Instantiate(
					settings.goDownDebris, 
					transform.position, 
					Quaternion.identity
				).GetComponent<ParticleSystem>();

				ParticleSystemRenderer debrisRenderer = debris.GetComponent<ParticleSystemRenderer>();
				debrisRenderer.mesh = GetComponent<MeshFilter>().sharedMesh;
				debrisRenderer.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;

				debris.Play(true);

			}

		}
		
	
	}

}