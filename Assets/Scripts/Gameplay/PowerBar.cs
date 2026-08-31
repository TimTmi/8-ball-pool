using UnityEngine;

namespace EightBall.Gameplay
{
    /// <summary>
    /// World-space power meter that floats next to the cue ball while the player
    /// drags. Shows a track and a fill that grows with the normalized shot power.
    /// Visibility is controlled by InputManager via Show/Hide.
    /// </summary>
    public class PowerBar : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Bar position relative to the cue ball (world units).")]
        [SerializeField] private Vector2 _offsetFromCueBall = new Vector2(0f, -0.9f);
        [SerializeField] private float _width = 1.6f;
        [SerializeField] private float _height = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color _fillColor = new Color(0.35f, 0.85f, 0.3f);

        private SpriteRenderer _fill;

        private static Sprite _whiteSprite;

        /// <summary>Solid 1x1 world-unit white sprite, built once from Unity's shared white texture.</summary>
        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var texture = Texture2D.whiteTexture;
                    _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), texture.width);
                }
                return _whiteSprite;
            }
        }

        private void Awake()
        {
            var track = CreateChildRenderer("Track");
            track.sortingOrder = 6;
            track.color = _backgroundColor;
            track.transform.localScale = new Vector3(_width, _height, 1f);

            _fill = CreateChildRenderer("Fill");
            _fill.sortingOrder = 7;
            _fill.color = _fillColor;

            Hide();
        }

        /// <summary>Moves the bar next to the cue ball and fills it to <paramref name="normalizedPower"/>.</summary>
        public void Show(Vector2 cueBallPosition, float normalizedPower)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            transform.position = cueBallPosition + _offsetFromCueBall;

            float fillWidth = _width * Mathf.Clamp01(normalizedPower);
            _fill.transform.localScale = new Vector3(fillWidth, _height, 1f);
            // Anchor the fill to the left edge of the track so it grows to the right
            _fill.transform.localPosition = new Vector3(-_width * 0.5f + fillWidth * 0.5f, 0f, 0f);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private SpriteRenderer CreateChildRenderer(string childName)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(transform, false);
            }

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) sr = child.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite;
            return sr;
        }
    }
}
