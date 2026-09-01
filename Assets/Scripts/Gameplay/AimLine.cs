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

        [Tooltip("How far the struck-ball direction line runs (world units).")]
        [SerializeField] private float _struckBallLineLength = TableLayout.BallDiameter * 4f;

        [Header("Colors")]
        [SerializeField] private Color _pathColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color _ghostColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color _struckBallColor = new Color(1f, 0.82f, 0.25f, 0.8f);

        /// <summary>Enough for the longest path across the table plus the struck-ball line.</summary>
        private const int MaxDots = 96;

        private readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>(MaxDots);
        private SpriteRenderer _ghostBall;

        private static Sprite _circleSprite;

        /// <summary>A soft-edged white disc, 1 world unit across. Built once, shared by every dot.</summary>
        private static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null) _circleSprite = CreateCircleSprite();
                return _circleSprite;
            }
        }

        private void Awake()
        {
            _ghostBall = CreateRenderer("GhostBall", 3);
            _ghostBall.color = _ghostColor;
            _ghostBall.transform.localScale = new Vector3(TableLayout.BallDiameter, TableLayout.BallDiameter, 1f);

            Hide();
        }

        /// <summary>Draws the guide for a shot leaving <paramref name="from"/>.</summary>
        public void Show(Vector2 from, in ShotPrediction.Result prediction)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            // Both runs start at the rim of the ball they leave, not its centre, so the first dot
            // does not sit on top of the ball.
            Vector2 towardsContact = (prediction.ContactPoint - from).normalized;
            Vector2 pathStart = from + towardsContact * TableLayout.BallRadius;
            int dotsUsed = PlaceDots(pathStart, prediction.ContactPoint, _pathColor, 0);

            if (prediction.StruckBall != null)
            {
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
                SpriteRenderer dot = CreateRenderer($"Dot_{_dots.Count:00}", 4);
                dot.transform.localScale = new Vector3(_dotSize, _dotSize, 1f);
                _dots.Add(dot);
            }

            return _dots[index];
        }

        private SpriteRenderer CreateRenderer(string childName, int sortingOrder)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(transform, false);
            }

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) sr = child.gameObject.AddComponent<SpriteRenderer>();

            sr.sprite = CircleSprite;
            sr.sortingOrder = sortingOrder; // above the balls (0), below the cue stick (5)
            return sr;
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            float radius = size * 0.5f;
            var centre = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // One pixel of feather at the rim, so small dots do not look ragged
                    float edgeDistance = radius - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                    var alpha = (byte)(Mathf.Clamp01(edgeDistance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
