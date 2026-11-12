using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.Dungeon
{
    /// White rim around InteractOutline's renderers, as in Dark and Darker: they are drawn into a mask, then the pixels
    /// next to the mask, but outside it, are laid over the frame.
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
            if (_material == null || InteractOutline.Renderers.Count == 0 || renderingData.cameraData.cameraType != CameraType.Game)
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

            private static readonly int MaskId = Shader.PropertyToID("_InteractOutlineMask");
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
                desc.graphicsFormat = GraphicsFormat.R8_UNorm;
                desc.depthStencilFormat = GraphicsFormat.None;
                desc.msaaSamples = 1;
                TextureHandle mask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "InteractOutlineMask", true);

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Interact Outline Mask", out PassData data))
                {
                    data.Material = _material;
                    builder.SetRenderAttachment(mask, 0);
                    builder.SetGlobalTextureAfterPass(mask, MaskId);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData pass, RasterGraphContext context) => DrawMask(pass, context.cmd));
                }

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Interact Outline", out PassData data))
                {
                    data.Material = _material;
                    builder.UseTexture(mask);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0);
                    builder.SetRenderFunc((PassData pass, RasterGraphContext context) => Blitter.BlitTexture(context.cmd, new Vector4(1f, 1f, 0f, 0f), pass.Material, 1));
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
                        cmd.DrawRenderer(renderer, pass.Material, subMesh, 0);
                }
            }
        }

        private sealed class PassData
        {
            public Material Material;
        }
    }
}
