using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.Dungeon
{
    /// White rim around InteractOutline's renderers, as in Dark and Darker: they are drawn into a mask, then the pixels
    /// next to the mask, but outside it, are laid over the frame. The bodies of fighters (the own first-person hands among them) go into
    /// the mask too, depth-tested: a body in front of the outlined thing covers it and its rim.
    public sealed class InteractOutlineFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private Shader _shader;

        [SerializeField]
        private Color _color = Color.white;

        [SerializeField]
        private float _width = 2f;

        private Material _material;
        private OutlinePass _pass;

        public override void Create()
        {
            if (_shader == null)
                return;

            _material = CoreUtils.CreateEngineMaterial(_shader);
            _pass = new OutlinePass(_material) { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // The UI overlay camera in the stack has its own projection: a second pass there lays a second, shifted rim.
            if (_material == null || InteractOutline.Renderers.Count == 0 || renderingData.cameraData.cameraType != CameraType.Game ||
                renderingData.cameraData.renderType == CameraRenderType.Overlay)
                return;

            _material.SetColor(OutlinePass.ColorId, _color);
            _material.SetFloat(OutlinePass.WidthId, _width);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
        }

        private sealed class OutlinePass : ScriptableRenderPass
        {
            public static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
            public static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");

            private const int MaskPass = 0;
            private const int OccluderPass = 1;
            private const int RimPass = 2;

            private static readonly int MaskId = Shader.PropertyToID("_InteractOutlineMask");
            private static readonly List<Renderer> s_occluders = new List<Renderer>();
            private static readonly List<Material> s_materials = new List<Material>();
            private readonly Material _material;

            public OutlinePass(Material material)
            {
                _material = material;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                RenderTextureDescriptor desc = frameData.Get<UniversalCameraData>().cameraTargetDescriptor;
                desc.graphicsFormat = GraphicsFormat.R16G16_SFloat;
                desc.depthStencilFormat = GraphicsFormat.None;
                desc.msaaSamples = 1;
                TextureHandle mask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "InteractOutlineMask", true);
                desc.graphicsFormat = GraphicsFormat.None;
                desc.depthStencilFormat = GraphicsFormat.D32_SFloat;
                TextureHandle depth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "InteractOutlineDepth", true);

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Interact Outline Mask", out PassData data))
                {
                    data.Material = _material;
                    builder.SetRenderAttachment(mask, 0);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(mask, MaskId);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData pass, RasterGraphContext context) => DrawMask(pass, context.cmd));
                }

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Interact Outline", out PassData data))
                {
                    data.Material = _material;
                    builder.UseTexture(mask);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0);
                    builder.SetRenderFunc((PassData pass, RasterGraphContext context) => Blitter.BlitTexture(context.cmd, new Vector4(1f, 1f, 0f, 0f), pass.Material, RimPass));
                }
            }

            private static void DrawMask(PassData pass, RasterCommandBuffer cmd)
            {
                IReadOnlyList<Renderer> renderers = InteractOutline.Renderers;

                for (int i = 0; i < renderers.Count; i++)
                {
                    Renderer renderer = renderers[i];

                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;

                    for (int subMesh = 0; subMesh < InteractOutline.SubMeshes[i]; subMesh++)
                        cmd.DrawRenderer(renderer, pass.Material, subMesh, MaskPass);
                }

                InteractOutline.CollectOccluders(s_occluders);

                foreach (Renderer renderer in s_occluders)
                {
                    renderer.GetSharedMaterials(s_materials);

                    for (int subMesh = 0; subMesh < s_materials.Count; subMesh++)
                        cmd.DrawRenderer(renderer, pass.Material, subMesh, OccluderPass);
                }
            }
        }

        private sealed class PassData
        {
            public Material Material;
        }
    }
}
