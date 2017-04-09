using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SHUDRS.Destructibles;

namespace SHUDRS.Weaponry  {

	/// Manages type of impact at Projectile's collision or Explosive's activation (or other Weaponry).
	public enum ImpactType  {

		/// Weaponry will have no impact at the object it hits 
		/// (example: bullet hits solid wall).
        NoImpact,
		/// Weaponry will have impact only at object it hits directly 
		/// (example: APFSDS shell hits weak wall and goes through).
		Point,
		/// Weaponry will cast sphere of 'innerRadius' radius, and you can manage objects inside this sphere.
		InnerOnly,
		/// Weaponry will cast sphere of 'outerRadius' radius, and you can manage objects inside this sphere
		/// (difference to InnerOnly is only inside default PerformDestruction method, so, maybe, it is excess).
		OuterOnly,
		/// Weaponry will cast both spheres: inner and outer (imagine shell explosion that turns some wall fragments
		/// into nothing, and some just detach from the wall).
		Bispherical

	}


	///  Base class for every Weaponry that is flying and does something (explodes, goes through objects, etc...)
	/// on collision with objects. It casts a ray with direction of Projectile's flying and length of a distance
	/// this Projectile will fly during current frame.
	[RequireComponent(typeof(Rigidbody))]
	public abstract class Projectile : MonoBehaviour  {

		/// Our cached Rigidbody.
		protected Rigidbody rb;

		/// Hit variable, containing information about raycast hit (if there was a hit during this frame).
		protected RaycastHit hit;

		/// Our current speed.
		protected float velocity;

		/// Should we hit triggers?
		public bool hitTriggers;

		/// Collision detection mask for Raycast.
		public LayerMask hitMask = -1;

		/// Defines how this type of Projectile will behave on impact.
		public ImpactType impactType;

		/// Radius of inner sphere (must be smaller than outer radius).
		public float innerRadius;

		/// Radius of outer sphere (must be greater than inner radius).
		public float outerRadius;


		///  We need to cache Rigidbody component.
		public void Awake()  {

			rb = GetComponent<Rigidbody>();

		}


		///  Main function of the Projectile, manages collision detection in front of the Projectile.
		public void FixedUpdate()  {

			/// For changing direction of flying (simulating realistic drag):
			transform.forward = rb.velocity.normalized;

			/// For detecting collisions (collider is not needed, because raycast is more accurate and flexible):
			velocity = rb.velocity.magnitude;
			if(Physics.Raycast(
				transform.position, 
				transform.forward, 
				out hit, 
				(velocity / Time.fixedDeltaTime) * 1.1f,
				hitMask,
				((hitTriggers) ? (QueryTriggerInteraction.Collide) : (QueryTriggerInteraction.Ignore))
			))  {  

				HandleCollision();

			}

		}


		///  This method is going to execute automatically every time a collision has been detected.
		/// You must implement this in your own projectile types to handle collisions as you like.
		public abstract void HandleCollision();


		///  Default method for Projectiles to perform destruction based on impactType in my Destruction system.
		/// You can use it or override, and so on - everything is permitted. ;)
		public void PerformDestruction()  {

			if(hit.collider.GetComponent<Fragment>() != null)  {

				switch(impactType)  {

				case ImpactType.NoImpact:  {

						break;

					}

				case ImpactType.Point:  {

						Fragment frag = hit.collider.GetComponent<Fragment>();
						if(frag.isIndestructible)
							return;
						
						frag.SpawnDirectionalDebris(frag.transform.position - transform.position, velocity);
						frag.DestroyFragment();

						break;

					}

				case ImpactType.InnerOnly:  {

						Collider[] frags;
						Fragment frag;

						/// Inner radius - Fragments turn into debris.
						frags = Physics.OverlapSphere(hit.point, innerRadius);
						for(int i = 0; i < frags.Length; i++)  {
							frag = frags[i].GetComponent<Fragment>();
							if(frag == null || frag.isIndestructible)
								continue;
							frag.SpawnDirectionalDebris(frag.transform.position - transform.position, velocity);
							frag.DestroyFragment();
						}

						break;

					}

				case ImpactType.OuterOnly:  {

						Collider[] frags;
						Fragment frag;

						/// Outer radius - Fragments go out of the Element.
						frags = Physics.OverlapSphere(hit.point, outerRadius);
						for(int i = 0; i < frags.Length; i++)  {
							frag = frags[i].GetComponent<Fragment>();
							if(frag == null || frag.isIndestructible)
								continue;
							frag.DestroyFragment(true);
						}

						break;

					}

				case ImpactType.Bispherical:  {

						Collider[] frags;
						Fragment frag;

						/// Inner radius - Fragments turn into debris.
						frags = Physics.OverlapSphere(hit.point, innerRadius);
						for(int i = 0; i < frags.Length; i++)  {
							frag = frags[i].GetComponent<Fragment>();
							if(frag == null || frag.isIndestructible)
								continue;
							frag.SpawnDirectionalDebris(frag.transform.position - transform.position, velocity);
							frag.DestroyFragment();
						}

						/// Outer radius - Fragments go out of the Element.
						frags = Physics.OverlapSphere(hit.point, outerRadius);
						for(int i = 0; i < frags.Length; i++)  {
							/// Note: we don't want to manage inner radius Fragments here 
							/// (they will be already marked as destroyed).
							frag = frags[i].GetComponent<Fragment>();
							if(frag == null || frag.isDestroyed || frag.isIndestructible)
								continue;
							frag.DestroyFragment(true);
						}

						break;

					}

				}

			}

		}
		
	}

}