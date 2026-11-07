using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// How each shield sits in the off hand: the shield socket's offset on the hand from where BattlePoseRig.CreateSocket puts it, in
    /// that socket's space. Every clip with the shield is solved for it, so the board keeps its authored path and the hand takes the
    /// grip. Set in Tools/Game/Battle/Shield Grip, applied by an animation rebuild.
    internal sealed class ShieldGripConfig : ScriptableObject
    {
        public const string RoundShield = "Shield";
        public const string Ecu = "Ecu";
        public const string Path = BattleEditorUtility.ConfigsFolder + "/ShieldGrips.asset";

        [Serializable]
        private sealed class Entry
        {
            public string Shield;
            public Vector3 Position;
            public Quaternion Rotation = Quaternion.identity;
        }

        [SerializeField]
        private List<Entry> _entries = new();

        public static Pose Get(string shield)
        {
            ShieldGripConfig config = shield == null ? null : AssetDatabase.LoadAssetAtPath<ShieldGripConfig>(Path);
            Entry entry = config != null ? config._entries.Find(item => item.Shield == shield) : null;

            return entry != null ? new Pose(entry.Position, entry.Rotation.normalized) : Pose.identity;
        }

        public static void Set(string shield, Pose grip)
        {
            ShieldGripConfig config = BattleEditorUtility.LoadOrCreate<ShieldGripConfig>(Path);
            Entry entry = config._entries.Find(item => item.Shield == shield);

            if (entry == null)
                config._entries.Add(entry = new Entry { Shield = shield });

            Undo.RecordObject(config, "Shield Grip");
            entry.Position = grip.position;
            entry.Rotation = grip.rotation;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
