using Fusion;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.Battle
{
    /// Development overlay (F3): block hitboxes of every fighter and, during attacks, the traced blade segment
    /// that actually deals damage plus the arc it sweeps in the active phase.
    public sealed class CombatDebugView : MonoBehaviour
    {
        [SerializeField]
        private KeyCode _toggleKey = KeyCode.F3;

        [SerializeField]
        private Color _blockActiveColor = new(0.2f, 0.9f, 1f, 1f);

        [SerializeField]
        private Color _blockIdleColor = new(0.2f, 0.9f, 1f, 0.3f);

        [SerializeField]
        private Color _bladeActiveColor = new(1f, 0.15f, 0.1f, 1f);

        [SerializeField]
        private Color _bladeIdleColor = new(1f, 0.85f, 0.2f, 0.8f);

        [SerializeField]
        private Color _arcColor = new(1f, 0.5f, 0.1f, 0.35f);

        [SerializeField]
        private Color _projectileColor = new(0.4f, 1f, 0.3f, 0.9f);

        private const int ArcSamples = 10;
        private const int ProjectileSamples = 16;

        private static readonly Vector3[] s_corners =
        {
            new(-1f, -1f, -1f), new(1f, -1f, -1f), new(1f, 1f, -1f), new(-1f, 1f, -1f),
            new(-1f, -1f, 1f), new(1f, -1f, 1f), new(1f, 1f, 1f), new(-1f, 1f, 1f)
        };

        private static readonly int[] s_edges = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

        private Material _material;

        private void Awake()
        {
            _material = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            _material.SetInt("_ZTest", (int)CompareFunction.Always);
            _material.SetInt("_ZWrite", 0);
            _material.SetInt("_Cull", (int)CullMode.Off);
            _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                BattleDebugSettings.Toggle();
        }

        /// The local player's own block box surrounds the first-person camera, so it is skipped.
        private void Draw(CombatComponent combat, bool isLocal)
        {
            WeaponConfig weapon = combat.Weapon;
            Hitbox[] blocks = combat.BlockHitboxes;
            int weaponIndex = combat.WeaponIndex;

            if (!isLocal && weapon.Block.CanBlock && weaponIndex < blocks.Length && blocks[weaponIndex] != null)
            {
                Hitbox block = blocks[weaponIndex];
                Color color = combat.State == CombatState.Block ? _blockActiveColor : _blockIdleColor;

                if (block is ZoneHitbox { IsRound: true })
                    DrawDisc(block.Position, block.transform.rotation, Mathf.Min(block.BoxExtents.x, block.BoxExtents.y), color);
                else
                    DrawBox(block.Position, block.transform.rotation, block.BoxExtents, color);
            }

            if (combat.State != CombatState.Attack)
                return;

            MeleeAttackConfig attack = combat.Attack;
            FighterBodyComponent body = combat.Body;
            bool isMirrored = weapon.IsMirrored;
            Vector3 previousTip = default;

            GL.Color(_arcColor);

            for (int i = 0; i < ArcSamples; i++)
            {
                float time = Mathf.Lerp(attack.ActiveStart, attack.ActiveEnd, i / (ArcSamples - 1f));

                if (!attack.EvaluateTrace(time, isMirrored, out Vector3 basePoint, out Vector3 tipPoint))
                    return;

                Vector3 tip = body.UpperToWorld(tipPoint);
                Line(body.UpperToWorld(basePoint), tip);

                if (i > 0)
                    Line(previousTip, tip);

                previousTip = tip;
            }

            if (!attack.EvaluateTrace(combat.StateTime, isMirrored, out Vector3 currentBase, out Vector3 currentTip))
                return;

            GL.Color(combat.Phase == AttackPhase.Active ? _bladeActiveColor : _bladeIdleColor);
            Vector3 from = body.UpperToWorld(currentBase);
            Vector3 to = body.UpperToWorld(currentTip);
            Vector3 offset = Vector3.up * 0.005f;
            Line(from, to);
            Line(from + offset, to + offset);
            Line(from - offset, to - offset);
        }

        /// Flight path from the muzzle to the current position (or the impact point once landed, for one second).
        private void DrawProjectiles(ProjectileComponent projectiles)
        {
            if (projectiles == null || projectiles.Object == null || !projectiles.Object.IsValid)
                return;

            NetworkRunner runner = projectiles.Runner;

            for (int i = 0; i < ProjectileComponent.Capacity; i++)
            {
                ProjectileData data = projectiles.Projectiles[i];

                if (data.FireTick <= 0 || (!data.IsFlying && runner.Tick - data.FinishTick > 1f / runner.DeltaTime))
                    continue;

                float end = data.IsFlying ? runner.SecondsSince(data.FireTick) : (data.FinishTick - data.FireTick) * runner.DeltaTime;
                Vector3 previous = data.Origin;
                GL.Color(_projectileColor);

                for (int step = 1; step <= ProjectileSamples; step++)
                {
                    Vector3 point = data.GetPosition(end * step / ProjectileSamples);
                    Line(previous, point);
                    previous = point;
                }

                if (!data.IsFlying)
                    DrawDisc(data.HitPoint, Quaternion.identity, Mathf.Max(data.Radius, 0.05f), _projectileColor);
            }
        }

        private void DrawBox(Vector3 center, Quaternion rotation, Vector3 extents, Color color)
        {
            GL.Color(color);

            for (int i = 0; i < s_edges.Length; i += 2)
            {
                Line(center + rotation * Vector3.Scale(s_corners[s_edges[i]], extents),
                    center + rotation * Vector3.Scale(s_corners[s_edges[i + 1]], extents));
            }
        }

        private void DrawDisc(Vector3 center, Quaternion rotation, float radius, Color color)
        {
            const int segments = 24;
            GL.Color(color);

            for (int i = 0; i < segments; i++)
            {
                Vector3 from = center + rotation * (Quaternion.Euler(0f, 0f, i * 360f / segments) * Vector3.right * radius);
                Vector3 to = center + rotation * (Quaternion.Euler(0f, 0f, (i + 1) * 360f / segments) * Vector3.right * radius);
                Line(from, to);
                Line(center, from);
            }
        }

        private static void Line(Vector3 from, Vector3 to)
        {
            GL.Vertex(from);
            GL.Vertex(to);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!BattleDebugSettings.IsEnabled || FighterComponent.All.Count == 0 || camera.cameraType != CameraType.Game ||
                (camera.TryGetComponent(out UniversalAdditionalCameraData data) && data.renderType == CameraRenderType.Overlay))
                return;

            _material.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            GL.Begin(GL.LINES);

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (fighter != null && fighter.Object != null && fighter.Object.IsValid)
                {
                    Draw(fighter.Combat, fighter.Object.HasInputAuthority);
                    DrawProjectiles(fighter.Combat.Projectiles);
                }
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}
