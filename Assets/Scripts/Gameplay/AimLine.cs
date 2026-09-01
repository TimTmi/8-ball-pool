using System.Collections.Generic;
using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// World-space aim guide: a dotted line from the cue ball to its first contact, a ghost ball
    /// marking where the cue ball will be at that moment, and a second dotted line for the
    /// direction the struck ball is sent. Visibility is controlled by InputManager via Show/Hide.
    /// </summary>
    public class AimLine : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Gap between dots. Derived from ball size so the guide rescales with the table.")]
        [SerializeField] private float _dotSpacing = TableLayout.BallDiameter * 0.9f;

        [SerializeField] private float _dotSize = TableLayout.BallDiameter * 0.22f;

        [Tooltip("Outline thickness in world units, matched on dots and ghost however they are scaled.")]
        [SerializeField] private float _outlineWidth = TableLayout.BallDiameter * 0.03f;

        [Tooltip("How far the struck-ball direction line runs (world units).")]
        [SerializeField] private float _struckBallLineLength = TableLayout.BallDiameter * 4f;

        [Header("Colors")]
        [SerializeField] private Color _pathColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color _ghostColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color _struckBallColor = new Color(1f, 0.82f, 0.25f, 0.8f);

        [Tooltip("Where the cue ball goes after contact — the top/back spin readout.")]
        [SerializeField] private Color _cueAfterColor = new Color(0.45f, 0.85f, 1f, 0.8f);

        /// <summary>Enough for the longest path across the table plus the struck-ball line.</summary>
        private const int MaxDots = 96;

        private readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>(MaxDots);
        private readonly List<Vector3> _approachPath = new List<Vector3>(96);
        private readonly List<Vector3> _cueAfterPath = new List<Vector3>(32);
        private SpriteRenderer _ghostBall;

        // Two discs rather than one shared sprite: the ghost ball is several times a dot wide, so
        // it needs a proportionally thinner rim to end up the same thickness on the table.
        private Sprite _dotSprite;
        private Sprite _ghostSprite;

        private void Awake()
        {
            _dotSprite = CreateDiscSprite(RimFraction(_dotSize));
            _ghostSprite = CreateDiscSprite(RimFraction(TableLayout.BallDiameter));

            _ghostBall = CreateRenderer("GhostBall", 3, _ghostSprite);
            _ghostBall.color = _ghostColor;
            _ghostBall.transform.localScale = new Vector3(TableLayout.BallDiameter, TableLayout.BallDiameter, 1f);

            Hide();
        }

        /// <summary>Draws the guide for a shot leaving <paramref name="from"/>.</summary>
        /// <summary>Predicts the shot described by <paramref name="request"/> and draws the guide.</summary>
        public void Show(in ShotPrediction.Request request)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            ShotPrediction.Result prediction = ShotPrediction.Predict(request, _approachPath, _cueAfterPath);

            int dotsUsed = PlaceDotsAlong(_approachPath, _pathColor, 0);
            dotsUsed = PlaceDotsAlong(_cueAfterPath, _cueAfterColor, dotsUsed);

            if (prediction.StruckBall != null)
            {
                // Straight run from the struck ball's rim, so the first dot clears the ball
                Vector2 struckBallCentre = prediction.StruckBall.transform.position;
                Vector2 struckBallStart = struckBallCentre + prediction.StruckBallDirection * TableLayout.BallRadius;
                Vector2 struckBallEnd = struckBallStart + prediction.StruckBallDirection * _struckBallLineLength;
                dotsUsed = PlaceDots(struckBallStart, struckBallEnd, _struckBallColor, dotsUsed);
            }

            HideDotsFrom(dotsUsed);

            _ghostBall.gameObject.SetActive(prediction.HasContact);
            if (prediction.HasContact) _ghostBall.transform.position = prediction.ContactPoint;
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        /// <summary>Lays dots along a segment and returns the next free dot index.</summary>
        /// <summary>
        /// Lays dots at even spacing along a polyline, carrying the leftover distance across
        /// vertices so a curved path is dotted as evenly as a straight one.
        /// </summary>
        private int PlaceDotsAlong(List<Vector3> path, Color color, int nextDot)
        {
            if (path.Count < 2) return nextDot;

            float sinceLastDot = 0f;

            for (int i = 1; i < path.Count && nextDot < MaxDots; i++)
            {
                Vector2 from = path[i - 1];
                Vector2 to = path[i];

                float segment = Vector2.Distance(from, to);
                if (segment <= 0f) continue;

                Vector2 step = (to - from) / segment;
                float alongSegment = _dotSpacing - sinceLastDot;

                while (alongSegment <= segment && nextDot < MaxDots)
                {
                    SpriteRenderer dot = GetDot(nextDot++);
                    dot.color = color;
                    dot.transform.position = from + step * alongSegment;
                    if (!dot.gameObject.activeSelf) dot.gameObject.SetActive(true);

                    alongSegment += _dotSpacing;
                }

                sinceLastDot = segment - (alongSegment - _dotSpacing);
            }

            return nextDot;
        }

        private int PlaceDots(Vector2 from, Vector2 to, Color color, int nextDot)
        {
            Vector2 span = to - from;
            float distance = span.magnitude;
            if (distance < _dotSpacing) return nextDot;

            Vector2 step = span / distance * _dotSpacing;
            int count = Mathf.FloorToInt(distance / _dotSpacing);

            // Start at 1: a dot sitting on the ball's own centre would just look like a smudge.
            for (int i = 1; i <= count && nextDot < MaxDots; i++, nextDot++)
            {
                SpriteRenderer dot = GetDot(nextDot);
                dot.color = color;
                dot.transform.position = from + step * i;
                if (!dot.gameObject.activeSelf) dot.gameObject.SetActive(true);
            }

            return nextDot;
        }

        private void HideDotsFrom(int firstUnused)
        {
            for (int i = firstUnused; i < _dots.Count; i++)
            {
                if (_dots[i].gameObject.activeSelf) _dots[i].gameObject.SetActive(false);
            }
        }

        private SpriteRenderer GetDot(int index)
        {
            while (_dots.Count <= index)
            {
                SpriteRenderer dot = CreateRenderer($"Dot_{_dots.Count:00}", 4, _dotSprite);
                dot.transform.localScale = new Vector3(_dotSize, _dotSize, 1f);
                _dots.Add(dot);
            }

            return _dots[index];
        }

        private SpriteRenderer CreateRenderer(string childName, int sortingOrder, Sprite sprite)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(transform, false);
            }

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) sr = child.gameObject.AddComponent<SpriteRenderer>();

            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder; // above the balls (0), below the cue stick (5)
            return sr;
        }

        /// <summary>Rim thickness as a fraction of the sprite's radius for a disc drawn at
        /// <paramref name="worldDiameter"/>, so every disc ends up the same thickness on the table.</summary>
        private float RimFraction(float worldDiameter)
        {
            if (worldDiameter <= 0f) return 0f;
            return Mathf.Clamp01(_outlineWidth / (worldDiameter * 0.5f));
        }

        /// <summary>
        /// A soft-edged disc, 1 world unit across, with a rim <paramref name="rimFraction"/> of the
        /// radius thick. The rim is baked black while the middle is left white, so the renderer's
        /// colour tints only the fill — multiplying by black leaves the outline black whatever
        /// colour the dot is given.
        /// </summary>
        private static Sprite CreateDiscSprite(float rimFraction)
        {
            const int size = 64;
            // Mip chain on: a dot is a tenth of the texture on screen, and minifying that far
            // without mips makes the line sparkle as it sweeps.
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var pixels = new Color32[size * size];

            float radius = size * 0.5f;
            float innerRadius = radius * (1f - Mathf.Clamp01(rimFraction));
            var centre = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);

                    // One pixel of feather at the outer edge, so small dots do not look ragged
                    var alpha = (byte)(Mathf.Clamp01(radius - distance) * 255f);

                    // and one more crossing into the rim, so the outline does not stair-step
                    float rim = Mathf.Clamp01(distance - innerRadius);
                    var fill = (byte)((1f - rim) * 255f);

                    pixels[y * size + x] = new Color32(fill, fill, fill, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
