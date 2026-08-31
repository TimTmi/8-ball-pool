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

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Texture2D CreateBallTexture(int size, Color32 baseColor, int number, bool isStripe)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Fill(tex, Color.clear);

            int cx = size / 2, cy = size / 2, r = size / 2 - 1;

            DrawFilledCircle(tex, cx, cy, r, isStripe ? Color.white : (Color)baseColor);

            if (isStripe)
            {
                int bandHalf = size / 5;
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

            // Highlight
            DrawFilledCircle(tex, cx - r / 4, cy + r / 4, r / 5, new Color(1f, 1f, 1f, 0.55f));
            // Center dot (white circle for number area)
            DrawFilledCircle(tex, cx, cy, r / 5, new Color(1f, 1f, 1f, 0.9f));

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
