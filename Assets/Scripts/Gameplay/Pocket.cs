using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Pocket mouth. The trigger is deliberately wider than the capture test: overlap alone would
    /// swallow any ball merely rolling past along the rail, so a ball only drops once its centre
    /// is actually over the hole.
    /// Added by <see cref="TableSetup"/> to every pocket.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class Pocket : MonoBehaviour
    {
        private void OnTriggerStay2D(Collider2D other)
        {
            var ball = other.GetComponent<Ball>();
            if (ball == null || ball.IsPocketed) return;

            Vector2 offset = other.transform.position - transform.position;
            if (offset.sqrMagnitude > TableLayout.PocketRadius * TableLayout.PocketRadius) return;

            ball.Drop();
        }
    }
}
