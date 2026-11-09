using System;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Start menu of the editor scene: one button per section, and back to the menu from any of them (button bottom-left or F10).
    public sealed class EditorMenuView : MonoBehaviour
    {
        public const float ButtonHeight = 28f;

        [Serializable]
        private struct Section
        {
            public string Name;
            public DisplayableView View;
        }

        [SerializeField]
        private Section[] _sections;

        private const float GuiHeight = AnimationTestView.GuiHeight;
        private const float Margin = AnimationTestView.Margin;
        private const float MenuWidth = 360f;
        private const float BackWidth = 120f;

        private AnimationEditorSkin _skin;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
                Open(-1);
        }

        private void OnGUI()
        {
            _skin ??= new AnimationEditorSkin();
            float scale = Screen.height / GuiHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (Array.Exists(_sections, section => section.View.IsShown))
            {
                if (GUI.Button(new Rect(Margin, GuiHeight - Margin - ButtonHeight, BackWidth, ButtonHeight), "◂ Menu  F10"))
                    Open(-1);

                return;
            }

            float width = Screen.width / scale;
            float height = 60f + _sections.Length * (ButtonHeight + 12f);
            GUILayout.BeginArea(new Rect((width - MenuWidth) * 0.5f, (GuiHeight - height) * 0.5f, MenuWidth, height), _skin.Panel);
            GUILayout.Label("Editor", _skin.Header);

            for (int i = 0; i < _sections.Length; i++)
            {
                GUILayout.Space(8f);

                if (_skin.Button(_sections[i].Name))
                    Open(i);
            }

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            _skin?.Dispose();
        }

        /// -1 closes every section and shows the menu.
        private void Open(int index)
        {
            for (int i = 0; i < _sections.Length; i++)
                _sections[i].View.SetShown(i == index);
        }
    }
}
