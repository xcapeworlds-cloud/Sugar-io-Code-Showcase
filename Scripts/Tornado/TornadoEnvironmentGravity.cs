using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tornado))]
public sealed class TornadoEnvironmentGravity : MonoBehaviour
{
    // =========================================================
    // INTENTIONALLY EMPTY
    // =========================================================
    //
    // Broken environment pieces are now handled entirely by
    // TornadoEnvironmentSuction.
    //
    // Flow:
    //
    // 1. Tornado gets close to an object.
    // 2. If almost strong enough, the object wiggles.
    // 3. If strong enough, the object breaks.
    // 4. Its edible pieces are claimed by that tornado.
    // 5. Pieces spiral inward and travel UP the funnel.
    // 6. They disappear at the top and give sugar.
    // 7. TornadoDestructible handles the later respawn.
    //
    // We keep this component/script so existing Player and AI
    // prefabs do NOT need any Inspector changes.
    //
    // No Rigidbody debris.
    // No falling-piece raycasts.
    // No runtime colliders.
    // No physics fighting the suction animation.
    //
    // This is also considerably cheaper for mobile.
    // =========================================================
}