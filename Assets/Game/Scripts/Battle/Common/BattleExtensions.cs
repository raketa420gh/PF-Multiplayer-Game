using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public static class BattleExtensions
    {
        private static readonly Comparison<LagCompensatedHit> s_byDistance = (a, b) => a.Distance.CompareTo(b.Distance);

        /// Lag compensated for the given player; bots (no input authority) query the current server state.
        public static void RaycastAllSorted(this NetworkRunner runner, Vector3 from, Vector3 to, PlayerRef player,
            List<LagCompensatedHit> hits, int layerMask, HitOptions options)
        {
            hits.Clear();

            Vector3 delta = to - from;
            float length = delta.magnitude;

            if (length < 0.0001f)
                return;

            Vector3 direction = delta / length;

            if (player.IsNone)
                runner.LagCompensation.RaycastAll(from, direction, length, runner.Tick, null, null, hits, layerMask, true, options);
            else
                runner.LagCompensation.RaycastAll(from, direction, length, player, hits, layerMask, true, options);

            hits.Sort(s_byDistance);
        }

        public static float SecondsSince(this NetworkRunner runner, int tick)
        {
            return (runner.Tick - tick) * runner.DeltaTime;
        }
    }
}
