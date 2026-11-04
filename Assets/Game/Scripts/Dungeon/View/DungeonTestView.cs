using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Dungeon Test panel (F2): calls the chosen monster in front of the adventurer, or takes every monster away.
    public sealed class DungeonTestView : DisplayableView
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private Rect _area = new(20f, 120f, 260f, 600f);

        private void OnGUI()
        {
            PlayerSessionComponent session = _context.LocalSession;

            if (session == null || _context.Director == null)
                return;

            IReadOnlyList<NetworkObject> monsters = _context.Director.TestMonsters;
            GUILayout.BeginArea(_area, "Dungeon Test · F2", GUI.skin.window);

            for (int i = 0; i < monsters.Count; i++)
            {
                if (GUILayout.Button(monsters[i].name))
                    session.RpcSpawnTestMonster((byte)i);
            }

            if (GUILayout.Button("Clear monsters"))
                session.RpcSpawnTestMonster(byte.MaxValue);

            GUILayout.EndArea();
        }
    }
}
