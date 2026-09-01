using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// Spawns and positions all visual and physics objects for the pool table:
    /// felt surface, rails (×4), pockets (×6), balls (×16), and cue stick.
    /// Attach to the Table GameObject in the Gameplay scene.
    /// Sprites are loaded from Resources/Sprites at runtime.
    /// </summary>
    public class TableSetup : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject _ballPrefab;
        [SerializeField] private GameObject _pocketPrefab;
        [SerializeField] private GameObject _cushionPrefab;

        [Header("Sprites — drag-assign from Assets/Textures or leave blank for runtime load")]
        [SerializeField] private Sprite _feltSprite;
        [SerializeField] private Sprite _railSprite;
        [SerializeField] private Sprite _railCornerSprite;
        [SerializeField] private Sprite _railCushionSprite;
        [SerializeField] private Sprite _pocketSprite;
        [SerializeField] private Sprite _cueStickSprite;
        [SerializeField] private Sprite[] _ballSprites; // Index 0=cue, 1–15 = balls 1–15

        [Header("Physics Materials")]
        [SerializeField] private PhysicsMaterial2D _ballPhysicsMaterial;
        [SerializeField] private PhysicsMaterial2D _cushionPhysicsMaterial;

        // References to spawned objects (accessible by gameplay systems)
        public GameObject CueBall    { get; private set; }
        public GameObject CueStick   { get; private set; }
        public GameObject PowerBar   { get; private set; }
        public GameObject AimLine    { get; private set; }
        public GameObject[] Balls    { get; private set; } // [0]=cue, [1–15]=object balls

        private void Start()
        {
            BuildTable();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Rebuilds the entire table layout (useful for re-rack).</summary>
        public void BuildTable()
        {
            SetupFelt();
            SetupRails();
            SetupPockets();
            SetupBalls();
            SetupCueStick();
            SetupPowerBar();
            SetupAimLine();
        }

        // ── Felt ──────────────────────────────────────────────────────────────

        private void SetupFelt()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

            if (_feltSprite == null)
                _feltSprite = LoadSprite("TableFelt");

            sr.sprite = _feltSprite;
            sr.sortingOrder = -10;

            // Scale so the sprite exactly covers the felt area
            if (_feltSprite != null)
            {
                float targetW = TableLayout.FeltWidth;
                float targetH = TableLayout.FeltHeight;
                float spriteW = _feltSprite.bounds.size.x;
                float spriteH = _feltSprite.bounds.size.y;

                if (spriteW > 0f && spriteH > 0f)
                    transform.localScale = new Vector3(targetW / spriteW, targetH / spriteH, 1f);
            }
        }

        // ── Rails ─────────────────────────────────────────────────────────────

        private void SetupRails()
        {
            if (_railSprite == null)
                _railSprite = LoadSprite("Rail");

            // Six runs of wood that join past the pockets into one closed frame;
            // the collider keeps the mouths open so balls can enter.
            foreach (TableLayout.RailSegment segment in TableLayout.GetRailSegments())
            {
                SpawnRail(segment);
            }

            SetupRailCorners();
        }

        /// <summary>
        /// Caps the four square corners where the rail runs meet with a rounded wood cap.
        /// Visual only — the pocket colliders stay open, and balls never reach the outer corners.
        /// </summary>
        private void SetupRailCorners()
        {
            if (_railCornerSprite == null)
                _railCornerSprite = LoadSprite("RailCorner");
            if (_railCornerSprite == null) return;

            float x = TableLayout.HalfFeltWidth + TableLayout.RailThickness / 2f;
            float y = TableLayout.HalfFeltHeight + TableLayout.RailThickness / 2f;

            // The sprite is rounded at its top-right; rotate it to face each outer corner.
            SpawnRailCorner("Rail_Corner_TL", new Vector2(-x,  y), 90f);
            SpawnRailCorner("Rail_Corner_TR", new Vector2( x,  y), 0f);
            SpawnRailCorner("Rail_Corner_BL", new Vector2(-x, -y), 180f);
            SpawnRailCorner("Rail_Corner_BR", new Vector2( x, -y), 270f);
        }

        private void SpawnRailCorner(string name, Vector2 localPosition, float zRotation)
        {
            var existing = transform.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _railCornerSprite;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingOrder = -7; // above the rails and pads, below the pocket holes

            // The cap is RailThickness square; the sprite is 26px wide at PxPerUnit=64
            float spriteSize = _railCornerSprite.bounds.size.x;
            if (spriteSize > 0f)
            {
                float scale = TableLayout.RailThickness / spriteSize;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void SpawnRail(TableLayout.RailSegment segment)
        {
            // Reuse existing child if rebuilding
            var existing = transform.Find(segment.Name);
            var go = existing != null ? existing.gameObject : new GameObject(segment.Name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = segment.VisualCenter;
            go.transform.localRotation = Quaternion.identity;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _railSprite;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingOrder = -9;

            // The pad runs the full wooden span so it reaches into every pocket;
            // the pocket sprites drawn above it cap its ends.
            SpawnCushionPad(segment.Name, segment.VisualCenter, segment.VisualSize);

            // Scale GO to the full wooden run, pockets included (Rail sprite is ~1u wide at PxPerUnit=64)
            Vector2 spriteSize = _railSprite != null ? (Vector2)_railSprite.bounds.size : Vector2.one;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
                go.transform.localScale = new Vector3(segment.VisualSize.x / spriteSize.x, segment.VisualSize.y / spriteSize.y, 1f);

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            // The transform scale maps the sprite's bounds onto the visual size, so the box has to be
            // authored in sprite-bounds units to come out exactly the cushion span in world space.
            // It is offset from the sprite centre by the run's extension over the pockets.
            Vector3 railScale = go.transform.localScale;
            col.size = new Vector2(ToLocal(segment.Size.x, railScale.x), ToLocal(segment.Size.y, railScale.y));
            col.offset = new Vector2(
                ToLocal(segment.Center.x - segment.VisualCenter.x, railScale.x),
                ToLocal(segment.Center.y - segment.VisualCenter.y, railScale.y));
            col.isTrigger = false;

            if (_cushionPhysicsMaterial != null) col.sharedMaterial = _cushionPhysicsMaterial;

            // Rails have to damp the rebound themselves — see Cushion for why the material can't.
            if (go.GetComponent<Cushion>() == null) go.AddComponent<Cushion>();
        }

        /// <summary>
        /// Spawns the darker green cushion pad hugging the felt-facing edge of a rail run,
        /// like the rubber cushion under the wooden rail on a real table.
        /// Visual only — the rail's BoxCollider2D stays the full RailThickness.
        /// Callers pass the visual run so the pad reaches the pocket holes.
        /// </summary>
        private void SpawnCushionPad(string railName, Vector2 railCenter, Vector2 railSize)
        {
            if (_railCushionSprite == null)
                _railCushionSprite = LoadSprite("RailCushion");
            if (_railCushionSprite == null) return;

            bool isLongRail = railSize.x >= railSize.y;

            float padThickness = TableLayout.CushionPadThickness;
            Vector2 padSize = isLongRail
                ? new Vector2(railSize.x, padThickness)
                : new Vector2(padThickness, railSize.y);

            // Inset the pad into the rail band so it sits flush against the felt edge,
            // where the ball actually rebounds
            Vector2 inward = isLongRail
                ? new Vector2(0f, -Mathf.Sign(railCenter.y))
                : new Vector2(-Mathf.Sign(railCenter.x), 0f);
            Vector2 padCenter = railCenter + inward * ((TableLayout.RailThickness - padThickness) * 0.5f);

            string padName = railName + "_Pad";
            var existing = transform.Find(padName);
            var go = existing != null ? existing.gameObject : new GameObject(padName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = padCenter;
            go.transform.localRotation = Quaternion.identity;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _railCushionSprite;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingOrder = -8; // above the rail, below pockets/balls

            Vector2 spriteSize = _railCushionSprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
                go.transform.localScale = new Vector3(padSize.x / spriteSize.x, padSize.y / spriteSize.y, 1f);
        }

        // ── Pockets ───────────────────────────────────────────────────────────

        private void SetupPockets()
        {
            if (_pocketSprite == null)
                _pocketSprite = LoadSprite("Pocket");

            Vector2[] positions = TableLayout.PocketPositions;
            for (int i = 0; i < positions.Length; i++)
            {
                SpawnPocket(i, positions[i]);
            }
        }

        private void SpawnPocket(int index, Vector2 localPosition)
        {
            string pocketName = $"Pocket_{index}";
            var existing = transform.Find(pocketName);

            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else if (_pocketPrefab != null)
            {
                go = Instantiate(_pocketPrefab, transform);
                go.name = pocketName;
            }
            else
            {
                go = new GameObject(pocketName);
                go.transform.SetParent(transform, false);
            }

            go.transform.localPosition = localPosition;

            // Visual
            float pocketScale = ScaleToFit(_pocketSprite, TableLayout.PocketRadius * 2f);
            go.transform.localScale = new Vector3(pocketScale, pocketScale, 1f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _pocketSprite;
            sr.sortingOrder = -6; // above the corner caps, so the holes stay cut into the frame

            // Trigger collider
            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.radius = ToLocal(TableLayout.PocketRadius, pocketScale);
            col.isTrigger = true;

            // Pocket decides when a ball has rolled far enough in to drop.
            if (go.GetComponent<Pocket>() == null) go.AddComponent<Pocket>();

            go.tag = "Pocket";
        }

        // ── Balls ─────────────────────────────────────────────────────────────

        private void SetupBalls()
        {
            Balls = new GameObject[16]; // 0 = cue, 1–15 = object balls

            // Cue ball
            CueBall = SpawnBall(0, TableLayout.HeadSpot);
            Balls[0] = CueBall;

            // Rack balls
            Vector2[] rackPositions = TableLayout.GetRackPositions();
            int[] rackOrder = TableLayout.GetRackOrder();

            for (int i = 0; i < 15; i++)
            {
                int ballNumber = rackOrder[i];
                Balls[ballNumber] = SpawnBall(ballNumber, rackPositions[i]);
            }
        }

        private GameObject SpawnBall(int ballNumber, Vector2 localPosition)
        {
            string ballName = ballNumber == 0 ? "Ball_Cue" : $"Ball_{ballNumber:00}";
            var existing = transform.Find(ballName);

            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else if (_ballPrefab != null)
            {
                go = Instantiate(_ballPrefab, transform);
                go.name = ballName;
            }
            else
            {
                go = new GameObject(ballName);
                go.transform.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>();
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.linearDamping = 0.8f;
                rb.angularDamping = 1f;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            go.transform.localPosition = localPosition;

            Sprite sprite = GetBallSprite(ballNumber);
            float ballScale = ScaleToFit(sprite, TableLayout.BallDiameter);
            go.transform.localScale = new Vector3(ballScale, ballScale, 1f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.sortingOrder = 0;
            }

            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.radius = ToLocal(TableLayout.BallRadius, ballScale);
            col.isTrigger = false;

            if (_ballPhysicsMaterial != null) col.sharedMaterial = _ballPhysicsMaterial;

            // Ball drives the shot launch and reports when this ball has stopped rolling.
            var ball = go.GetComponent<Ball>();
            if (ball == null) ball = go.AddComponent<Ball>();
            ball.Restore(); // A rebuild puts any pocketed ball back in play

            go.tag = ballNumber == 0 ? "CueBall" : "Ball";

            return go;
        }

        private Sprite GetBallSprite(int ballNumber)
        {
            // Try serialized array first
            if (_ballSprites != null && ballNumber < _ballSprites.Length && _ballSprites[ballNumber] != null)
                return _ballSprites[ballNumber];

            // Fall back to runtime load
            string spriteName = ballNumber == 0 ? "Ball_Cue" : $"Ball_{ballNumber:00}";
            return LoadSprite(spriteName);
        }

        // ── Cue Stick ─────────────────────────────────────────────────────────

        private void SetupCueStick()
        {
            var existing = transform.Find("CueStick");
            CueStick = existing != null ? existing.gameObject : new GameObject("CueStick");
            CueStick.transform.SetParent(transform, false);

            if (_cueStickSprite == null)
                _cueStickSprite = LoadSprite("CueStick");

            var sr = CueStick.GetComponent<SpriteRenderer>();
            if (sr == null) sr = CueStick.AddComponent<SpriteRenderer>();
            sr.sprite = _cueStickSprite;
            sr.sortingOrder = 5;

            // Position behind cue ball, pointing right by default
            if (CueBall != null)
            {
                Vector2 cueBallPos = CueBall.transform.localPosition;
                // Offset behind cue ball along -X axis (cue points right, tip toward ball)
                const float cueLength = 8f;
                CueStick.transform.localPosition = new Vector3(cueBallPos.x - cueLength * 0.5f, cueBallPos.y, 0f);
                CueStick.transform.localRotation = Quaternion.identity;
            }

            // Match cue stick sprite to its natural size (8u x 0.125u)
            if (_cueStickSprite != null)
            {
                float spriteW = _cueStickSprite.bounds.size.x;
                if (spriteW > 0f)
                    CueStick.transform.localScale = new Vector3(8f / spriteW, 1f, 1f);
            }
        }

        // ── Power Bar ─────────────────────────────────────────────────────────

        private void SetupPowerBar()
        {
            var existing = transform.Find("PowerBar");
            if (existing == null)
            {
                existing = new GameObject("PowerBar").transform;
                existing.SetParent(transform, false);
                existing.gameObject.AddComponent<PowerBar>();
            }
            PowerBar = existing.gameObject;
        }

        // ── Aim Line ──────────────────────────────────────────────────────────

        private void SetupAimLine()
        {
            var existing = transform.Find("AimLine");
            if (existing == null)
            {
                existing = new GameObject("AimLine").transform;
                existing.SetParent(transform, false);
                existing.gameObject.AddComponent<AimLine>();
            }
            AimLine = existing.gameObject;
        }

        // ── Sprite Loading ────────────────────────────────────────────────────

        private static Sprite LoadSprite(string name)
        {
            // Try Resources first, then Textures folder via direct name
            var sprite = Resources.Load<Sprite>($"Sprites/{name}");
            if (sprite != null) return sprite;

            // Fallback: load via Resources root
            sprite = Resources.Load<Sprite>(name);
            return sprite;
        }

        /// <summary>Uniform scale that renders <paramref name="sprite"/> at <paramref name="targetSize"/> units wide.</summary>
        private static float ScaleToFit(Sprite sprite, float targetSize)
        {
            if (sprite == null) return 1f;

            float spriteSize = sprite.bounds.size.x;
            return spriteSize > 0f ? targetSize / spriteSize : 1f;
        }

        /// <summary>
        /// Converts a world-space collider dimension into the local space that the transform scale
        /// re-expands. Colliders are authored in local units, so skipping this leaves the physics
        /// shape a different size from the sprite it belongs to.
        /// </summary>
        private static float ToLocal(float worldSize, float scale)
        {
            return Mathf.Approximately(scale, 0f) ? worldSize : worldSize / scale;
        }
    }
}
