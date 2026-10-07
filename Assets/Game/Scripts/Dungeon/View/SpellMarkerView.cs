using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Circle on the ground that shows where an area spell will land while it is being cast.
    public sealed class SpellMarkerView : DisplayableView
    {
        private const int TextureSize = 128;
        private const float RingWidth = 0.04f;
        private const float Lift = 0.08f;
        private const float FillAlpha = 0.18f;

        private static Sprite s_sprite;

        private SpriteRenderer _renderer;

        public static SpellMarkerView Create()
        {
            SpellMarkerView marker = new GameObject("SpellMarker").AddComponent<SpellMarkerView>();
            marker._renderer = marker.gameObject.AddComponent<SpriteRenderer>();
            marker._renderer.sprite = GetSprite();
            marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            marker.Hide();

            return marker;
        }

        /// A charged spell pulses, an unfinished one is dim.
        public void Place(Vector3 center, float radius, Color color, bool isCharged)
        {
            Show();
            transform.position = center + Vector3.up * Lift;
            transform.localScale = Vector3.one * radius * 2f;
            color.a = isCharged ? 0.65f + 0.35f * Mathf.Sin(Time.time * 8f) : 0.45f;
            _renderer.color = color;
        }

        private static Sprite GetSprite()
        {
            if (s_sprite != null)
                return s_sprite;

            Texture2D texture = new(TextureSize, TextureSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color32[] pixels = new Color32[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y) / (TextureSize - 1f), Vector2.one * 0.5f) * 2f;
                    float alpha = distance > 1f ? 0f : distance > 1f - RingWidth ? 1f : FillAlpha * distance;
                    pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            s_sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), Vector2.one * 0.5f, TextureSize);

            return s_sprite;
        }
    }
}
