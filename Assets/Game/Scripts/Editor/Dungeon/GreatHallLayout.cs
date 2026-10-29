using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// One object of the Great Hall's approved mockup: Unity room-local metres from the middle of the hall's first cell (+Z
    /// runs into its second cell), pivot at the bottom middle, size X × Y × Z, yaw in degrees.
    [Serializable]
    internal sealed class HallItem
    {
        public string id;
        public string cat;
        public string name;
        public string kind;
        public float[] p;
        public float[] s;
        public float ry;
        public bool round;
        public int seal;
        public string color;
        public float i;
        public float r;

        public Vector3 Position => new(p[0], p[1], p[2]);
        public Vector3 Size => s is { Length: 3 } ? new Vector3(s[0], s[1], s[2]) : Vector3.zero;
        public Quaternion Rotation => Quaternion.Euler(0f, ry, 0f);
    }

    /// The Great Hall is built from its mockup page (Docs/LevelMockups/GreatHallShaft.html), the user's approved layout: the
    /// objects are the JSON of its OBJS line.
    internal static class GreatHallLayout
    {
        public const string MockupPath = "Docs/LevelMockups/GreatHallShaft.html";
        private const string Marker = "const OBJS = ";

        [Serializable]
        private sealed class Items
        {
            public HallItem[] items;
        }

        public static HallItem[] Load()
        {
            string html = File.ReadAllText(Path.Combine(Application.dataPath, "..", MockupPath));
            int start = html.IndexOf(Marker, StringComparison.Ordinal) + Marker.Length;
            int end = html.IndexOf("];", start, StringComparison.Ordinal) + 1;

            return JsonUtility.FromJson<Items>("{\"items\":" + html.Substring(start, end - start) + "}").items;
        }

        public static HallItem Find(HallItem[] items, string name)
        {
            return items.First(item => item.name == name);
        }
    }
}
