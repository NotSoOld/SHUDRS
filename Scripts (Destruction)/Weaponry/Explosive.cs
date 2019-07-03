using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SHUDRS.Destructibles;

namespace SHUDRS.Weaponry  {

	///  Base class for every Weaponry that's not exploding because of collision with something (unlike projectiles).
	/// It can be just activated at every time.
	public abstract class Explosive : MonoBehaviour  {

		/// Defines how this type of Explosive will behave on activation.
		public ImpactType impactType;

		/// Radius of inner sphere (must be smaller than outer radius).
		public float innerRadius;

		/// Radius of outer sphere (must be greater than inner radius).
		public float outerRadius;

		/// Explosion force of this Explosive.
		public float explosionForce;


		///  Main method to perform explosion of this Explosion. You must give your own implementation to this
		/// in your custom Explosion classes.
		public abstract void ActivateExplosion();


		///  Default method for Explosives to perform destruction based on impactType in my Destruction system.
		/// You can use it or override, and so on - everything is permitted. ;)
		public void PerformDestruction()  {

			switch(impactType)  {

			/// Even though it makes no sense...
			case ImpactType.NoImpact:  {

					break;

				}

			/// And also this...
			case ImpactType.Point:  {

					break;

				}

			case ImpactType.InnerOnly:  {

					Collider[] frags;
					Fragment frag;

					/// Inner radius - Fragments turn into debris.
					frags = Physics.OverlapSphere(transform.position, innerRadius);
					for(int i = 0; i < frags.Length; i++)  {
						frag = frags[i].GetComponent<Fragment>();
						if(frag == null) {
								continue;
							}

							frag.SpawnDirectionalDebris(
							frag.transform.position - transform.position, 
							(frag.transform.position - transform.position).sqrMagnitude
						);
						frag.DestroyFragment();
					}

					break;

				}

			case ImpactType.OuterOnly:  {

					Collider[] frags;
					Fragment frag;

					/// Outer radius - Fragments go out of the Element.
					frags = Physics.OverlapSphere(transform.position, outerRadius);
					for(int i = 0; i < frags.Length; i++)  {
						frag = frags[i].GetComponent<Fragment>();
						if(frag == null) {
								continue;
							}

							frag.DestroyFragment(true);

						/// Add some explosion force to Fragment.
						frag.GetComponent<Rigidbody>().AddExplosionForce(
							explosionForce,
							transform.position,
							outerRadius
						);
					}

					break;

				}

			case ImpactType.Bispherical:  {

					Collider[] frags;
					Fragment frag;

					/// Inner radius - Fragments turn into debris.
					frags = Physics.OverlapSphere(transform.position, innerRadius);
					for(int i = 0; i < frags.Length; i++)  {
						frag = frags[i].GetComponent<Fragment>();
						if(frag == null) {
								continue;
							}

							frag.SpawnDirectionalDebris(
							frag.transform.position - transform.position, 
							(frag.transform.position - transform.position).sqrMagnitude
						);
						frag.DestroyFragment();
					}

					/// Outer radius - Fragments go out of the Element.
					frags = Physics.OverlapSphere(transform.position, outerRadius);
					for(int i = 0; i < frags.Length; i++)  {
						/// Note: we don't want to manage inner radius Fragments here 
						/// (they will be already marked as destroyed).
						frag = frags[i].GetComponent<Fragment>();
						if(frag == null || frag.isDestroyed) {
								continue;
							}

							frag.DestroyFragment(true);

						/// Add some explosion force to Fragment.
						frag.GetComponent<Rigidbody>().AddExplosionForce(
							explosionForce,
							transform.position,
							outerRadius
						);
					}

					break;

				}

			}

		}

	}

}