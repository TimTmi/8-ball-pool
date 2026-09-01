using System.IO;
using UnityEditor;
using UnityEngine;

namespace EightBall.Editor
{
    /// <summary>
    /// Generates all pool-game sprite PNGs programmatically.
    /// Run via: Tools > 8 Ball Pool > Generate Sprites
    /// </summary>
    public static class SpriteGenerator
    {
        private const string OutputPath = "Assets/Resources/Sprites";
        private const int PxPerUnit = 64;

        [MenuItem("Tools/8 Ball Pool/Generate Sprites")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/Sprites"));

            GenerateTableFelt();
            GenerateRail();
            GenerateRailCorner();
            GenerateRailCushion();
            GeneratePocket();
            GenerateCueBall();
            GenerateBalls();
            GenerateCueStick();
            GenerateHudIcons();

            AssetDatabase.Refresh();
            ConfigureSprites();

            Debug.Log("[SpriteGenerator] All sprites generated and imported.");
        }

        // ── Table felt: solid green rectangle ──────────────────────────────────
        private static void GenerateTableFelt()
        {
            // 9u x 4.5u → 576 x 288 px
            int w = 576, h = 288;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var green = new Color32(26, 107, 60, 255);
            Fill(tex, green);
            SavePng(tex, "TableFelt");
        }

        // ── Rail: brownish wood strip ──────────────────────────────────────────
        private static void GenerateRail()
        {
            int w = 64, h = 26;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var railColor = new Color32(150, 102, 56, 255);
            Fill(tex, railColor);
            tex.Apply();
            SavePng(tex, "Rail");
        }

        // ── Rail corner cap: rounds one outer table corner ─────────────────────
        // Sits on top of the square corner where two rail runs meet. The sprite is
        // rounded at its top-right; TableSetup rotates it per corner.
        private static void GenerateRailCorner()
        {
            // RailThickness 0.4u → 26 x 26 px at PxPerUnit=64
            int size = 26;
            int radius = 16; // 0.25u rounding of the outer corner
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var railColor = new Color32(150, 102, 56, 255);
            Fill(tex, railColor);

            // Cut the top-right corner: outside the corner region keep the wood,
            // inside it keep only pixels within `radius` of the arc centre.
            int cx = size - radius, cy = size - radius;
            for (int y = cy; y < size; y++)
            {
                for (int x = cx; x < size; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    int distSq = dx * dx + dy * dy;
                    if (distSq > radius * radius)
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            SavePng(tex, "RailCorner");
        }

        // ── Rail cushion pad: dark green strip on the rail's felt-facing side ──
        private static void GenerateRailCushion()
        {
            int w = 64, h = 10;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            // Deliberately darker than the felt (26,107,60) so the pad reads as a separate surface
            Fill(tex, new Color32(13, 64, 35, 255));
            tex.Apply();
            SavePng(tex, "RailCushion");
        }

        // ── Pocket: dark circle ────────────────────────────────────────────────
        private static void GeneratePocket()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Fill(tex, Color.clear);
            DrawFilledCircle(tex, size / 2, size / 2, size / 2, new Color32(15, 15, 15, 255));
            DrawFilledCircle(tex, size / 2, size / 2, size / 2 - 4, new Color32(5, 5, 5, 255));
            tex.Apply();
            SavePng(tex, "Pocket");
        }

        // ── Cue ball ──────────────────────────────────────────────────────────
        private static void GenerateCueBall()
        {
            var tex = CreateBallTexture(64, Color.white, 0, false);
            SavePng(tex, "Ball_Cue");
        }

        // ── 15 pool balls ─────────────────────────────────────────────────────
        private static void GenerateBalls()
        {
            Color32[] colors = {
                new Color32(240, 180, 0,   255),
                new Color32(20,  60,  200, 255),
                new Color32(200, 30,  30,  255),
                new Color32(130, 40,  160, 255),
                new Color32(220, 100, 0,   255),
                new Color32(30,  140, 30,  255),
                new Color32(140, 30,  30,  255),
                new Color32(15,  15,  15,  255),
                new Color32(240, 180, 0,   255),
                new Color32(20,  60,  200, 255),
                new Color32(200, 30,  30,  255),
                new Color32(130, 40,  160, 255),
                new Color32(220, 100, 0,   255),
                new Color32(30,  140, 30,  255),
                new Color32(140, 30,  30,  255),
            };

            for (int i = 0; i < 15; i++)
            {
                bool isStripe = i >= 8;
                var tex = CreateBallTexture(64, colors[i], i + 1, isStripe);
                SavePng(tex, string.Format("Ball_{0:00}", i + 1));
            }
        }

        // ── Cue stick ─────────────────────────────────────────────────────────
        private static void GenerateCueStick()
        {
            int w = 512, h = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Fill(tex, Color.clear);

            // Tip at the right (+X): runtime code points the sprite's +X axis at the cue ball
            for (int x = 0; x < w; x++)
            {
                float t = 1f - (float)x / w;
                byte r = (byte)Mathf.Lerp(220, 100, t);
                byte g = (byte)Mathf.Lerp(180, 60, t);
                byte b = (byte)Mathf.Lerp(100, 30, t);
                var col = new Color32(r, g, b, 255);
                int halfH = Mathf.Max(1, (int)Mathf.Lerp(1, h / 2, t));
                int center = h / 2;
                for (int y = center - halfH; y <= center + halfH; y++)
                {
                    if (y >= 0 && y < h)
                        tex.SetPixel(x, y, col);
                }
            }

            // Blue chalk tip (right end)
            for (int x = w - 16; x < w; x++)
            {
                int center = h / 2;
                for (int y = center - 1; y <= center + 1; y++)
                    tex.SetPixel(x, y, new Color32(70, 110, 200, 255));
            }

            tex.Apply();
            SavePng(tex, "CueStick");
        }

        // ── HUD icons: white glyphs on transparent, used by GameplayUI ────────

        private const int IconSize = 96;
        private static readonly Color32 IconColor = new Color32(235, 235, 235, 255);

        private static void GenerateHudIcons()
        {
            GenerateAimIcon();
            GeneratePowerIcon();
            GenerateLockIcon();
            GenerateShootIcon();
        }

        // Aim: crosshair — ring, four tick marks, centre dot
        private static void GenerateAimIcon()
        {
            var tex = CreateIconTexture();
            int c = IconSize / 2;

            DrawRing(tex, c, c, 26, 6, IconColor);

            int tickOuter = 42, tickInner = 32;
            DrawSegment(tex, new Vector2(c, c - tickOuter), new Vector2(c, c - tickInner), 6, IconColor);
            DrawSegment(tex, new Vector2(c, c + tickOuter), new Vector2(c, c + tickInner), 6, IconColor);
            DrawSegment(tex, new Vector2(c - tickOuter, c), new Vector2(c - tickInner, c), 6, IconColor);
            DrawSegment(tex, new Vector2(c + tickOuter, c), new Vector2(c + tickInner, c), 6, IconColor);

            DrawFilledCircle(tex, c, c, 7, IconColor);
            tex.Apply();
            SavePng(tex, "Icon_Aim");
        }

        // Power: gauge — semicircular dial with a needle pointing up-right
        private static void GeneratePowerIcon()
        {
            // Icon coordinates are Unity texture space: y up, origin bottom-left
            var tex = CreateIconTexture();
            int cx = 48, cy = 34;
            float radius = 36, thickness = 8;

            // Upper half of the dial ring
            float inner = radius - thickness * 0.5f;
            float outer = radius + thickness * 0.5f;
            for (int y = cy; y <= cy + Mathf.CeilToInt(outer); y++)
            {
                for (int x = cx - Mathf.CeilToInt(outer); x <= cx + Mathf.CeilToInt(outer); x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    float dx = x - cx, dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= inner && d <= outer)
                        tex.SetPixel(x, y, IconColor);
                }
            }

            // Needle reaching the dial at the upper right, hub on top of its base
            DrawSegment(tex, new Vector2(cx, cy), new Vector2(cx + 26, cy + 26), 7, IconColor);
            DrawFilledCircle(tex, cx, cy, 9, IconColor);

            tex.Apply();
            SavePng(tex, "Icon_Power");
        }

        // Lock: padlock — shackle ring over a rounded body
        private static void GenerateLockIcon()
        {
            var tex = CreateIconTexture();

            // Shackle: ring centred on the body top; the body drawn after hides its lower half
            DrawRing(tex, 48, 50, 16, 6, IconColor);

            DrawRoundedRect(tex, 26, 12, 70, 52, 8, IconColor);
            tex.Apply();
            SavePng(tex, "Icon_Lock");
        }

        // Shoot: cue stick striking the cue ball, with impact sparks
        private static void GenerateShootIcon()
        {
            var tex = CreateIconTexture();

            // Ball at lower right, cue comes in from the upper left
            DrawFilledCircle(tex, 62, 54, 16, IconColor);
            DrawSegment(tex, new Vector2(12, 8), new Vector2(50, 42), 9, IconColor);

            // Sparks flying off the contact point, away from the ball
            DrawSegment(tex, new Vector2(48, 34), new Vector2(40, 22), 5, IconColor);
            DrawSegment(tex, new Vector2(40, 50), new Vector2(26, 46), 5, IconColor);
            DrawSegment(tex, new Vector2(42, 64), new Vector2(32, 74), 5, IconColor);

            tex.Apply();
            SavePng(tex, "Icon_Shoot");
        }

        private static Texture2D CreateIconTexture()
        {
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            Fill(tex, Color.clear);
            return tex;
        }

        // ── Icon drawing helpers ─────────────────────────────────────────────

        private static void DrawRing(Texture2D tex, int cx, int cy, int radius, int thickness, Color color)
        {
            float inner = radius - thickness * 0.5f;
            float outer = radius + thickness * 0.5f;
            int reach = Mathf.CeilToInt(outer);

            for (int y = cy - reach; y <= cy + reach; y++)
            {
                for (int x = cx - reach; x <= cx + reach; x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d >= inner && d <= outer)
                        tex.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawSegment(Texture2D tex, Vector2 a, Vector2 b, float thickness, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - thickness));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + thickness));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - thickness));
            int maxY = Mathf.Min(tex.height - 1, Mathf.FloorToInt(Mathf.Max(a.y, b.y) + thickness));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d = DistanceToSegment(new Vector2(x + 0.5f, y + 0.5f), a, b);
                    if (d <= thickness * 0.5f)
                        tex.SetPixel(x, y, color);
                }
            }
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp(Vector2.Dot(p - a, ab) / ab.sqrMagnitude, 0f, 1f);
            return Vector2.Distance(p, a + t * ab);
        }

        private static void DrawRoundedRect(Texture2D tex, int x0, int y0, int x1, int y1, int radius, Color color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    // In a corner box, keep only pixels within `radius` of the arc centre
                    int cx = Mathf.Clamp(x, x0 + radius, x1 - radius);
                    int cy = Mathf.Clamp(y, y0 + radius, y1 - radius);
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= radius * radius)
                        tex.SetPixel(x, y, color);
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Texture2D CreateBallTexture(int size, Color32 baseColor, int number, bool isStripe)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Fill(tex, Color.clear);

            int cx = size / 2, cy = size / 2, r = size / 2 - 1;

            DrawFilledCircle(tex, cx, cy, r, isStripe ? Color.white : (Color)baseColor);

            if (isStripe)
            {
                // Real stripe balls: white poles with a coloured equator band
                // covering about half the ball diameter.
                int bandHalf = Mathf.RoundToInt(r * 0.5f);
                for (int x = 0; x < size; x++)
                {
                    for (int y = cy - bandHalf; y <= cy + bandHalf; y++)
                    {
                        int dx = x - cx, dy = y - cy;
                        if (dx * dx + dy * dy <= r * r)
                            tex.SetPixel(x, y, baseColor);
                    }
                }
            }

            // Number patch: small white circle, about one third of the ball
            // diameter, as on real balls.
            int numberRadius = Mathf.Max(3, r / 3);
            DrawFilledCircle(tex, cx, cy, numberRadius, Color.white);

            tex.Apply();
            return tex;
        }

        private static void DrawFilledCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            int rSq = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= rSq)
                        tex.SetPixel(x, y, color);
                }
            }
        }

        private static void Fill(Texture2D tex, Color color)
        {
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
        }

        private static void SavePng(Texture2D tex, string name)
        {
            byte[] bytes = tex.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, "Resources/Sprites", name + ".png");
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { OutputPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;

                if (path.Contains("TableFelt"))
                    importer.spritePixelsPerUnit = PxPerUnit;
                else if (path.Contains("Rail"))
                    importer.spritePixelsPerUnit = PxPerUnit;
                else if (path.Contains("Pocket"))
                    importer.spritePixelsPerUnit = PxPerUnit;
                else if (path.Contains("Ball"))
                    importer.spritePixelsPerUnit = PxPerUnit;
                else if (path.Contains("CueStick"))
                    importer.spritePixelsPerUnit = PxPerUnit;

                importer.SaveAndReimport();
            }
        }
    }
}
