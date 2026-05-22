using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Games
{
    /// <summary>
    /// Procedurally generated UI sprites so game cells aren't bare squares:
    /// a soft-edged circle and a 9-sliced rounded rectangle, built once as
    /// white textures (tinted by each Image's color). No art assets needed.
    /// </summary>
    public static class Shapes
    {
        private static Sprite _circle;
        private static Sprite _rounded;

        public static void Circle(Image img)
        {
            if (img == null) return;
            Ensure();
            img.sprite = _circle;
            img.type = Image.Type.Simple;
        }

        public static void Rounded(Image img)
        {
            if (img == null) return;
            Ensure();
            img.sprite = _rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
        }

        private static void Ensure()
        {
            if (_circle == null) _circle = BuildCircle(128);
            if (_rounded == null) _rounded = BuildRounded(48, 14);
        }

        private static Sprite BuildCircle(int size)
        {
            var tex = NewTex(size);
            float r = size / 2f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d);          // ~1px antialiased edge
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildRounded(int size, int rad)
        {
            var tex = NewTex(size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    float cx = x + 0.5f, cy = y + 0.5f;
                    // Distance into a corner only when inside a corner quadrant.
                    float qx = cx < rad ? rad - cx : (cx > size - rad ? cx - (size - rad) : 0f);
                    float qy = cy < rad ? rad - cy : (cy > size - rad ? cy - (size - rad) : 0f);
                    if (qx > 0f && qy > 0f)
                    {
                        float d = Mathf.Sqrt(qx * qx + qy * qy);
                        a = Mathf.Clamp01(rad - d + 0.5f);
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(rad, rad, rad, rad));
        }

        private static Texture2D NewTex(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }
    }
}
