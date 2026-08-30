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
        [SerializeField] private Sprite _pocketSprite;
        [SerializeField] private Sprite _cueStickSprite;
        [SerializeField] private Sprite[] _ballSprites; // Index 0=cue, 1–15 = balls 1–15

        [Header("Physics Materials")]
        [SerializeField] private PhysicsMaterial2D _cushionPhysicsMaterial;

        // References to spawned objects (accessible by gameplay systems)
        public GameObject CueBall    { get; private set; }
        public GameObject CueStick   { get; private set; }
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

            float fw = TableLayout.FeltWidth;
            float fh = TableLayout.FeltHeight;
            float rt = TableLayout.RailThickness;

            // Bottom, Top, Left, Right
            SpawnRail("Rail_Bottom", new Vector2(0f, -(fh + rt) / 2f), new Vector2(fw + rt * 2f, rt), 0f);
            SpawnRail("Rail_Top",    new Vector2(0f,  (fh + rt) / 2f), new Vector2(fw + rt * 2f, rt), 0f);
            SpawnRail("Rail_Left",   new Vector2(-(fw + rt) / 2f, 0f), new Vector2(rt, fh), 0f);
            SpawnRail("Rail_Right",  new Vector2( (fw + rt) / 2f, 0f), new Vector2(rt, fh), 0f);
        }

        private void SpawnRail(string railName, Vector2 position, Vector2 size, float angle)
        {
            // Reuse existing child if rebuilding
            var existing = transform.Find(railName);
            var go = existing != null ? existing.gameObject : new GameObject(railName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _railSprite;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingOrder = -9;
            sr.color = new Color(0.36f, 0.22f, 0.09f);

            // Scale GO to match required rail size (Rail sprite is ~1u wide at PxPerUnit=64)
            if (_railSprite != null)
            {
                float sw = _railSprite.bounds.size.x;
                float sh = _railSprite.bounds.size.y;
                if (sw > 0f && sh > 0f)
                    go.transform.localScale = new Vector3(size.x / sw, size.y / sh, 1f);
            }

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            // Collider size 1×1 in local space; transform scale handles world size
            col.size = Vector2.one;
            col.isTrigger = false;

            if (_cushionPhysicsMaterial != null) col.sharedMaterial = _cushionPhysicsMaterial;
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
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _pocketSprite;
            sr.sortingOrder = -8;

            float pocketDiameter = TableLayout.PocketRadius * 2f;
            if (_pocketSprite != null)
            {
                float spriteSize = _pocketSprite.bounds.size.x;
                if (spriteSize > 0f)
                {
                    float scale = pocketDiameter / spriteSize;
                    go.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }

            // Trigger collider
            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.radius = TableLayout.PocketRadius;
            col.isTrigger = true;

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
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = TableLayout.BallRadius;
            }

            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one;

            // Assign sprite
            Sprite sprite = GetBallSprite(ballNumber);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.sortingOrder = 0;

                // Scale sprite to match ball diameter
                if (sprite != null)
                {
                    float spriteSize = sprite.bounds.size.x;
                    if (spriteSize > 0f)
                    {
                        float scale = TableLayout.BallDiameter / spriteSize;
                        go.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                }
            }

            // Tag
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

        // Fix missing field reference in SetupCueStick
        
    }
}
