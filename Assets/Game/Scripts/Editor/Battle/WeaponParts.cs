using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Building blocks shared by the weapon models. Weapon space: +Z up the weapon, +Y the leading edge, X across the flat.
    internal static class WeaponParts
    {
        /// Double-edged blade through stations (z, half width, half thickness). The ridge is where the bevels start, as a
        /// fraction of the width; the fuller is a groove down the middle of the flat on the first part of the length.
        public static void Blade(WeaponMesh mesh, Material material, IReadOnlyList<Vector3> stations, float ridge = 0.05f,
            float fuller = 0f, float fullerLength = 0.6f)
        {
            List<Vector3[]> rings = new(stations.Count);
            float from = stations[0].x;
            float length = stations[^1].x - from;

            foreach (Vector3 station in stations)
            {
                float w = station.y;
                float t = station.z;
                float z = station.x;
                float hollow = fuller * (1f - Mathf.Clamp01(((z - from) / length - fullerLength + 0.12f) / 0.12f));
                float bed = t * (1f - hollow);
                float r = w * ridge;
                float g = w * Mathf.Min(0.3f, ridge * 0.8f);

                rings.Add(new[]
                {
                    new Vector3(0f, w, z), new Vector3(t, r, z), new Vector3(t, g, z), new Vector3(bed, g * 0.5f, z), new Vector3(bed, -g * 0.5f, z),
                    new Vector3(t, -g, z), new Vector3(t, -r, z), new Vector3(0f, -w, z), new Vector3(-t, -r, z), new Vector3(-t, -g, z),
                    new Vector3(-bed, -g * 0.5f, z), new Vector3(-bed, g * 0.5f, z), new Vector3(-t, g, z), new Vector3(-t, r, z)
                });
            }

            mesh.Loft(material, rings, WeaponMesh.Hard);
        }

        /// Stations of a sword blade: an even taper to the start of the point, then an ogive to the tip.
        public static List<Vector3> SwordStations(float from, float to, float halfWidth, float halfThickness, float taper, float point)
        {
            List<Vector3> stations = new();
            float length = to - from;
            float pointStart = to - length * point;

            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                stations.Add(new Vector3(Mathf.Lerp(from, pointStart, t), halfWidth * Mathf.Lerp(1f, taper, t), halfThickness * Mathf.Lerp(1f, 0.6f, t)));
            }

            for (int i = 1; i <= 5; i++)
            {
                float s = i / 5f;
                float shape = 1f - Mathf.Pow(s, 1.7f);
                stations.Add(new Vector3(Mathf.Lerp(pointStart, to, s), halfWidth * taper * shape, halfThickness * 0.6f * Mathf.Lerp(shape, 1f, 0.25f) * (i == 5 ? 0f : 1f)));
            }

            return stations;
        }

        /// Single-edged blade: a straight thick spine on -Y and an edge that swells toward a clipped point.
        public static void Cleaver(WeaponMesh mesh, Material material, float from, float to, float width, float halfThickness)
        {
            const int steps = 14;
            List<Vector3[]> rings = new(steps + 1);
            float spine = -width * 0.42f;
            float tip = -width * 0.2f;

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float z = Mathf.Lerp(from, to, t);
                float edge = width * (0.58f + 0.42f * Mathf.Clamp01(t / 0.72f));
                float back = spine;
                float thickness = halfThickness * Mathf.Lerp(1f, 0.7f, t);

                if (t > 0.72f)
                {
                    float s = (t - 0.72f) / 0.28f;
                    edge = tip + (edge - tip) * Mathf.Sqrt(1f - s * s);
                    back = Mathf.Lerp(spine, tip, s * s * s);
                    thickness *= 1f - s * 0.85f;
                }

                float bevel = Mathf.Lerp(back, edge, 0.4f);
                rings.Add(new[]
                {
                    new Vector3(0f, edge, z), new Vector3(thickness, bevel, z), new Vector3(thickness, back, z),
                    new Vector3(-thickness, back, z), new Vector3(-thickness, bevel, z)
                });
            }

            mesh.Loft(material, rings, WeaponMesh.Hard);
        }

        /// Profile of a ball for WeaponMesh.Revolve.
        public static List<Vector2> Ball(float z, float radius, float stretch = 1f, int steps = 8)
        {
            List<Vector2> profile = new(steps + 1);

            for (int i = 0; i <= steps; i++)
            {
                float angle = i * Mathf.PI / steps;
                profile.Add(new Vector2(z - Mathf.Cos(angle) * radius * stretch, Mathf.Sin(angle) * radius));
            }

            return profile;
        }

        /// Handle swelling in the middle, oval in section so it sits in the fist.
        public static void Grip(WeaponMesh mesh, Material material, float from, float to, float radius, float swell = 0.12f, float squash = 0.82f)
        {
            const int steps = 6;
            List<Vector2> profile = new(steps + 1);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                profile.Add(new Vector2(Mathf.Lerp(from, to, t), radius * (1f + swell * Mathf.Sin(t * Mathf.PI))));
            }

            mesh.Revolve(material, profile, 10, squash);
        }

        /// Straight shaft along Z with its own radius at both ends.
        public static void Shaft(WeaponMesh mesh, Material material, float from, float to, float radiusFrom, float radiusTo, int sides = 10, float squash = 1f)
        {
            mesh.Revolve(material, new[] { new Vector2(from, radiusFrom), new Vector2((from + to) * 0.5f, (radiusFrom + radiusTo) * 0.5f), new Vector2(to, radiusTo) }, sides, squash);
        }

        /// Ring or collar around the Z axis.
        public static void Band(WeaponMesh mesh, Material material, float z, float radius, float halfLength, float bulge = 0.002f, float squash = 1f)
        {
            mesh.Revolve(material, new[]
            {
                new Vector2(z - halfLength, radius - bulge), new Vector2(z - halfLength * 0.5f, radius),
                new Vector2(z + halfLength * 0.5f, radius), new Vector2(z + halfLength, radius - bulge)
            }, 10, squash);
        }

        /// Bar along Y through (0, 0, z): thick in the middle, drawn out to the ends, optionally bent toward +Z.
        public static void Crossbar(WeaponMesh mesh, Material material, float z, float halfLength, float thickness, float bend = 0f, float flare = 1f)
        {
            const int steps = 8;
            List<Vector3> path = new(steps + 1);
            List<Vector2> radii = new(steps + 1);

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps * 2f - 1f;
                float taper = Mathf.Lerp(1f, 0.55f, Mathf.Abs(t));

                if (i == 0 || i == steps)
                    taper *= flare;

                path.Add(new Vector3(0f, t * halfLength, z + bend * t * t));
                radii.Add(new Vector2(thickness * taper, thickness * 0.8f * taper));
            }

            mesh.Tube(material, path, radii, 6, Vector3.forward, WeaponMesh.Hard);
        }

        /// Box with chamfered long edges, lofted along Z between two rectangles (centre x/y, half sizes).
        public static void Box(WeaponMesh mesh, Material material, float zFrom, float zTo, Vector2 halfFrom, Vector2 halfTo,
            Vector2 centerFrom = default, Vector2 centerTo = default, float chamfer = 0.2f)
        {
            mesh.Loft(material, new[] { BoxRing(zFrom, halfFrom, centerFrom, chamfer), BoxRing(zTo, halfTo, centerTo, chamfer) }, WeaponMesh.Hard);
        }

        public static Vector3[] BoxRing(float z, Vector2 half, Vector2 center, float chamfer = 0.2f)
        {
            float c = Mathf.Min(half.x, half.y) * chamfer;

            return new[]
            {
                new Vector3(center.x + half.x, center.y + half.y - c, z), new Vector3(center.x + half.x - c, center.y + half.y, z),
                new Vector3(center.x - half.x + c, center.y + half.y, z), new Vector3(center.x - half.x, center.y + half.y - c, z),
                new Vector3(center.x - half.x, center.y - half.y + c, z), new Vector3(center.x - half.x + c, center.y - half.y, z),
                new Vector3(center.x + half.x - c, center.y - half.y, z), new Vector3(center.x + half.x, center.y - half.y + c, z)
            };
        }

        /// Cone with its base at 'from', for spikes and studs.
        public static void Spike(WeaponMesh mesh, Material material, Vector3 from, Vector3 to, float radius, int sides = 5)
        {
            mesh.Rod(material, from, to, radius, 0f, sides, WeaponMesh.Hard);
        }

        /// Domed rivet head sitting on a surface point and facing along the normal.
        public static void Rivet(WeaponMesh mesh, Material material, Vector3 position, Vector3 normal, float radius)
        {
            Matrix4x4 matrix = mesh.Matrix;
            mesh.Matrix = matrix * Matrix4x4.TRS(position, Quaternion.LookRotation(normal), Vector3.one);
            mesh.Revolve(material, new[] { new Vector2(-radius * 0.3f, radius), new Vector2(radius * 0.25f, radius * 0.85f), new Vector2(radius * 0.55f, radius * 0.45f), new Vector2(radius * 0.62f, 0f) }, 6);
            mesh.Matrix = matrix;
        }
    }
}
