using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Dark flat look of the animation editor: framed panels, tool buttons that light up while on, selectable list rows with
    /// a coloured tag on the right and compact labelled sliders. Built inside OnGUI, where GUI.skin is available.
    public sealed class AnimationEditorSkin : IDisposable
    {
        public const float RowHeight = 20f;

        public static readonly Color Accent = new(1f, 0.72f, 0.22f);
        public static readonly Color Text = new(0.88f, 0.89f, 0.91f);
        public static readonly Color Dim = new(0.58f, 0.6f, 0.64f);
        public static readonly Color Back = new(0.1f, 0.11f, 0.13f, 0.94f);
        public static readonly Color Line = new(0.26f, 0.28f, 0.32f);

        public GUIStyle Panel => _panel;
        public GUIStyle Header => _header;
        public GUIStyle Label => _label;
        public GUIStyle Hint => _hint;
        public GUIStyle Tag => _tag;
        public GUIStyle Badge => _badge;

        private const float SliderLabelWidth = 64f;
        private const float SliderValueWidth = 48f;

        private readonly List<Texture2D> _textures = new();
        private readonly GUIStyle _panel;
        private readonly GUIStyle _header;
        private readonly GUIStyle _label;
        private readonly GUIStyle _hint;
        private readonly GUIStyle _tag;
        private readonly GUIStyle _badge;
        private readonly GUIStyle _value;
        private readonly GUIStyle _button;
        private readonly GUIStyle _buttonOn;
        private readonly GUIStyle _row;
        private readonly GUIStyle _rowOn;
        private readonly GUIStyle _rowText;
        private readonly GUIStyle _rowTextOn;

        public AnimationEditorSkin()
        {
            _panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6), margin = new RectOffset() };
            SetBackground(_panel, Back, Line);

            _label = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(2, 2, 3, 3), margin = new RectOffset(2, 2, 2, 2) };
            _label.normal.textColor = Text;
            _header = new GUIStyle(_label) { fontSize = 13, fontStyle = FontStyle.Bold };
            _header.normal.textColor = Color.white;
            _hint = new GUIStyle(_label) { fontSize = 11, wordWrap = true };
            _hint.normal.textColor = Dim;
            _tag = new GUIStyle(_hint) { alignment = TextAnchor.MiddleRight, wordWrap = false, fontStyle = FontStyle.Bold };
            _badge = new GUIStyle(_tag) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset() };
            _badge.normal.textColor = Color.white;
            _value = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
            _value.normal.textColor = Accent;

            _button = CreateButton(new Color(0.2f, 0.21f, 0.24f), new Color(0.28f, 0.3f, 0.34f), Text);
            _buttonOn = CreateButton(Accent, new Color(1f, 0.8f, 0.4f), new Color(0.1f, 0.08f, 0.04f));
            _row = new GUIStyle(_button) { alignment = TextAnchor.MiddleLeft, fixedHeight = RowHeight, margin = new RectOffset(0, 0, 1, 0) };
            SetBackground(_row, Color.clear, Color.clear, new Color(1f, 1f, 1f, 0.07f));
            _rowOn = new GUIStyle(_row);
            SetBackground(_rowOn, new Color(Accent.r, Accent.g, Accent.b, 0.22f), Accent, new Color(Accent.r, Accent.g, Accent.b, 0.3f));
            _rowText = new GUIStyle(_label) { alignment = TextAnchor.MiddleLeft, margin = new RectOffset(), padding = new RectOffset() };
            _rowTextOn = new GUIStyle(_rowText) { fontStyle = FontStyle.Bold };
            _rowTextOn.normal.textColor = Accent;
        }

        public void Dispose()
        {
            foreach (Texture2D texture in _textures)
                UnityEngine.Object.Destroy(texture);

            _textures.Clear();
        }

        public bool Button(string text, bool isOn = false, float width = 0f)
        {
            return width > 0f ? GUILayout.Button(text, isOn ? _buttonOn : _button, GUILayout.Width(width)) : GUILayout.Button(text, isOn ? _buttonOn : _button);
        }

        public bool Toggle(bool value, string text, float width = 0f)
        {
            return Button(text, value, width) ? !value : value;
        }

        /// List entry: indented text, a coloured tag on the right, the whole row is the button.
        public bool Row(string text, bool isOn, float indent = 0f, string tag = null, Color tagColor = default)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, _row, GUILayout.ExpandWidth(true));
            bool isPressed = GUI.Button(rect, GUIContent.none, isOn ? _rowOn : _row);
            GUI.Label(new Rect(rect.x + 4f + indent, rect.y, rect.width - indent, rect.height), text, isOn ? _rowTextOn : _rowText);

            if (string.IsNullOrEmpty(tag))
                return isPressed;

            GUI.color = tagColor;
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 6f, rect.height), tag, _tag);
            GUI.color = Color.white;

            return isPressed;
        }

        public float Slider(string text, float value, float min, float max, string format = "F1")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text, _label, GUILayout.Width(SliderLabelWidth));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            GUILayout.Label(value.ToString(format), _value, GUILayout.Width(SliderValueWidth));
            GUILayout.EndHorizontal();

            return value;
        }

        public void Separator()
        {
            GUILayout.Space(4f);
            Fill(GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true)), Line);
            GUILayout.Space(4f);
        }

        public static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private GUIStyle CreateButton(Color normal, Color hover, Color text)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12, fixedHeight = 24f, padding = new RectOffset(8, 8, 3, 3), margin = new RectOffset(2, 2, 2, 2),
                border = new RectOffset(1, 1, 1, 1)
            };

            SetBackground(style, normal, Line, hover);
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = text;
            style.onNormal = style.normal;
            style.onHover = style.hover;

            return style;
        }

        private void SetBackground(GUIStyle style, Color fill, Color border, Color? hover = null)
        {
            style.border = new RectOffset(1, 1, 1, 1);
            style.normal.background = style.focused.background = CreateTexture(fill, border);
            style.hover.background = CreateTexture(hover ?? fill, border);
            style.active.background = CreateTexture(Color.Lerp(hover ?? fill, Color.white, 0.15f), border);
        }

        /// 3×3 texture: a one-pixel frame around the fill, stretched by the style border.
        private Texture2D CreateTexture(Color fill, Color border)
        {
            Texture2D texture = new Texture2D(3, 3) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                    texture.SetPixel(x, y, x == 1 && y == 1 ? fill : border);
            }

            texture.Apply();
            _textures.Add(texture);

            return texture;
        }
    }
}
