using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Animates the pseudo-3D ball texture. The flat sprite stays on the SpriteRenderer as the
    /// quad; this component tracks the ball's 3D orientation — rolling from the rigidbody's
    /// velocity, plus the cue ball's side spin turning it about the vertical axis — and feeds
    /// it to the <see cref="BallRollShader"/> material so the surface map slides across the
    /// sphere instead of the sprite spinning flat. Added by <see cref="TableSetup"/> to every
    /// spawned ball; the physics rotation itself is frozen because this owns all rotation.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class BallRoll : MonoBehaviour
    {
        /// <summary>How fast the ball turns about the vertical axis at full side spin (rad/s).</summary>
        private const float MaxVerticalSpinRate = 5f;

        /// <summary>Spawn pose: a quarter turn about Y stands a number patch (ball-local -X on
        /// the map's equator) up facing the camera instead of lying on the ball's side.</summary>
        private static readonly Quaternion SpawnOrientation = Quaternion.Euler(0f, 90f, 0f);

        private Quaternion _orientation = Quaternion.identity;
        private Rigidbody2D _body;
        private SpriteRenderer _renderer;
        private CueBallSpin _spin;
        private MaterialPropertyBlock _block;

        private static readonly int RotationId = Shader.PropertyToID("_Rotation");
        private static readonly int MapId = Shader.PropertyToID("_MapTex");

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _spin = GetComponent<CueBallSpin>();
        }

        /// <summary>
        /// Gives the ball its surface map and its spawn orientation (a number patch facing
        /// up). Called by <see cref="TableSetup"/> right after the ball is (re)built.
        /// </summary>
        public void Setup(Texture2D surfaceMap)
        {
            if (_renderer == null) return;

            if (_block == null) _block = new MaterialPropertyBlock();
            _block.SetTexture(MapId, surfaceMap);
            _renderer.SetPropertyBlock(_block);

            _orientation = SpawnOrientation;
            PushOrientation();
        }

        private void FixedUpdate()
        {
            if (_body == null || _renderer == null) return;

            // Contact with the cloth turns travel into roll at one radian per radius covered;
            // side spin pivots the ball about the vertical axis, matching the curve the
            // spin model applies to the velocity.
            Vector2 velocity = _body.linearVelocity;
            Vector3 angularVelocity = new Vector3(-velocity.y, velocity.x, 0f) / TableLayout.BallRadius;
            if (_spin != null)
                angularVelocity += Vector3.forward * (_spin.SideSpin * MaxVerticalSpinRate);

            float rate = angularVelocity.magnitude;
            if (rate < 0.01f) return;

            float angle = rate * Time.fixedDeltaTime * Mathf.Rad2Deg;
            _orientation = Quaternion.AngleAxis(angle, angularVelocity / rate) * _orientation;
        }

        private void LateUpdate() => PushOrientation();

        private void PushOrientation()
        {
            if (_renderer == null) return;

            if (_block == null) _block = new MaterialPropertyBlock();

            // The shader maps the view normal into the ball's frame, so it needs the
            // conjugate of the ball's orientation.
            Quaternion inverse = Quaternion.Inverse(_orientation);
            _block.SetVector(RotationId, new Vector4(inverse.x, inverse.y, inverse.z, inverse.w));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
