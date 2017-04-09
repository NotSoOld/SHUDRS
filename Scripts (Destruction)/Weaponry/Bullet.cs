///  Example of using base Projectile class to create your own custom projectiles:

/// 1. Add namespace.
using SHUDRS.Weaponry;

/// 2. Derive your custom projectile class from a Projectile class.
public class Bullet : Projectile  {

	/// 3. (optional) Add custom properties.
	public string type;

	/// 4. Provide implementation for HandleCollision method which will being called
	///    every time projectile's raycast will hit something.
	public override void HandleCollision()  {  }

	/// 5. (optional) Provide your own methods to handle different collision events
	///    (like PerformDestruction in base class), if you would like to call them
	///    in HandleCollision method.

}