using System.Collections.Generic;
using System.Linq;
using Fusion;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Shares the lamps of a room between three kinds of place. The neighbourhoods of lamps go whole, so that a kind is a place and
    /// not a pattern: a share stays steady and baked, a share becomes lamps anyone can put out, a share loses its lamps and stays dark.
    internal static class CatacombRoomLighting
    {
        public const string SwitchMarker = "Trap Lamp ";

        public static readonly string[] Switchable = { "Torch", "Brazier", "Candle" };

        private const float DarkShare = 0.2f;
        /// A baked lamp and a switched one.
        private const int MinLamps = 2;
        private const float SwitchShare = 0.35f;
        private const float Neighbour = 8f;
        private const float Storey = 2.5f;
        private const float Doorway = 8.4f;
        private const float Side = DungeonMapBuilder.Pitch * 0.5f;
        private const float LampHold = 0.6f;

        private sealed class Place
        {
            public readonly List<Transform> Lamps = new();
            public Vector3 Center => Lamps.Aggregate(Vector3.zero, (sum, lamp) => sum + lamp.position) / Lamps.Count;
        }

        /// A room that has its marker kept is done: the lamps of the dark places are gone, there is nothing to share out again.
        public static void Balance(Transform room, System.Action<string, Vector3, float> addSwitch)
        {
            if (room.Cast<Transform>().Any(part => part.name.StartsWith(SwitchMarker)))
                return;

            List<Transform> lamps = room.Cast<Transform>().Where(IsLamp).ToList();
            List<Place> places = Group(lamps);
            System.Random random = new System.Random(Hash(room.name));
            int total = lamps.Count;
            int dark = Mathf.RoundToInt(total * DarkShare);
            int switching = Mathf.Max(total > 1 ? 1 : 0, Mathf.RoundToInt(total * SwitchShare));
            int darkened = 0;

            // Far from the doorways first: a dark place is a corner to walk into, not the way in.
            foreach (Place place in places.OrderByDescending(FromDoorways).ToList())
            {
                bool isAloneOnStorey = lamps.All(lamp => place.Lamps.Contains(lamp) || Mathf.Abs(lamp.position.y - place.Center.y) > Storey);

                if (isAloneOnStorey || darkened + place.Lamps.Count > Mathf.Max(dark, 1) || lamps.Count - place.Lamps.Count < MinLamps || place.Lamps.Any(lamp => !IsSwitchable(lamp)))
                    continue;

                darkened += place.Lamps.Count;
                places.Remove(place);
                lamps.RemoveAll(place.Lamps.Contains);
                place.Lamps.ForEach(lamp => Object.DestroyImmediate(lamp.gameObject));
            }

            int left = switching;

            foreach (Place place in places.OrderBy(_ => random.Next()).ToList())
            {
                if (left <= 0 || place.Lamps.Any(lamp => !IsSwitchable(lamp)) || place.Lamps.Count > left + 1)
                    continue;

                left -= place.Lamps.Count;
                places.Remove(place);
                place.Lamps.ForEach(lamp => Switch(lamp, addSwitch));
                lamps.RemoveAll(place.Lamps.Contains);
            }

            // One place all round the room (a ring of candles) is shared lamp by lamp.
            foreach (Transform lamp in lamps.Where(IsSwitchable).OrderBy(_ => random.Next()).Take(Mathf.Min(left, lamps.Count - 1)).ToList())
            {
                left--;
                lamps.Remove(lamp);
                Switch(lamp, addSwitch);
            }

            Debug.Log($"{room.name}: {total} lamps, {darkened} dark, {switching - left} switched, {lamps.Count} baked");
        }

        private static void Switch(Transform lamp, System.Action<string, Vector3, float> addSwitch)
        {
            addSwitch(SwitchMarker + Kind(lamp), lamp.localPosition, lamp.localEulerAngles.y);
            Object.DestroyImmediate(lamp.gameObject);
        }

        public static string LampPrefab(string kind)
        {
            return $"CatacombLamp{kind}";
        }

        /// The kit lamp, networked: stands where the kit piece stood and turns with the room. Reached from the floor, however high it hangs.
        public static void BuildLamp(string kind)
        {
            GameObject root = new GameObject(LampPrefab(kind));
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            CatacombLampComponent lamp = root.AddComponent<CatacombLampComponent>();
            GameObject body = CatacombRoomBuilder.Kit($"Light {kind}", root.transform, Vector3.zero, 0f);
            PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Light light = body.GetComponentInChildren<Light>(true);
            DungeonLightLayers.Set(light, DungeonLightLayers.Everything);
            float top = Mathf.Max(light.transform.position.y, 0.5f);
            DungeonContentBuilder.AddInteractCollider(root, new Vector3(0f, top * 0.5f, 0f), new Vector3(1f, top, 1f));
            SerializedObject so = new SerializedObject(lamp);
            BattleEditorUtility.Set(so, "_light", light);
            BattleEditorUtility.Set(so, "_flicker", light.GetComponent<FlickerLightComponent>());
            BattleEditorUtility.Set(so, "_flames", body.GetComponentsInChildren<ParticleSystem>(true));
            BattleEditorUtility.Set(so, "_holdTime", LampHold);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, DungeonContentBuilder.Prefab(LampPrefab(kind)));
            Object.DestroyImmediate(root);
        }

        public static bool IsLamp(Transform part)
        {
            return part.name.StartsWith("Light ") && part.GetComponentInChildren<Light>(true) != null;
        }

        private static bool IsSwitchable(Transform lamp)
        {
            return Switchable.Contains(Kind(lamp));
        }

        private static string Kind(Transform lamp)
        {
            return lamp.name.Substring("Light ".Length);
        }

        private static List<Place> Group(List<Transform> lamps)
        {
            List<Place> places = new();

            foreach (Transform lamp in lamps)
            {
                List<Place> near = places.Where(place => place.Lamps.Any(other => IsNear(lamp, other))).ToList();
                Place joined = near.FirstOrDefault();

                if (joined == null)
                    places.Add(joined = new Place());

                joined.Lamps.Add(lamp);

                foreach (Place other in near.Skip(1))
                {
                    joined.Lamps.AddRange(other.Lamps);
                    places.Remove(other);
                }
            }

            return places;
        }

        private static bool IsNear(Transform a, Transform b)
        {
            return Mathf.Abs(a.position.y - b.position.y) <= Storey && (a.position - b.position).magnitude <= Neighbour;
        }

        private static float FromDoorways(Place place)
        {
            Vector3 point = place.Center;
            float best = float.MaxValue;

            foreach (float along in new[] { -Doorway, Doorway })
            {
                foreach (Vector2 door in new[] { new Vector2(along, Side), new Vector2(along, -Side), new Vector2(Side, along), new Vector2(-Side, along) })
                    best = Mathf.Min(best, Vector2.Distance(door, new Vector2(point.x, point.z)));
            }

            return best;
        }

        private static int Hash(string text)
        {
            return text.Aggregate(17, (hash, letter) => hash * 31 + letter);
        }
    }
}
