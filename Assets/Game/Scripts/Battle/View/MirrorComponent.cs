using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.Battle
{
    /// Planar mirror on the visible (local -Z) face of its quad. A proper camera at the reflected eye renders the scene
    /// flipped horizontally; the shader flips it back. The local fighter's head is shown while they stand in front of it.
    public sealed class MirrorComponent : MonoBehaviour
    {
        public static bool IsLocalReflected => s_reflecting > 0;

        [SerializeField]
        private Renderer _surface;

        [SerializeField]
        private float _renderDistance = 25f;

        [SerializeField]
        private float _headDistance = 8f;

        private static readonly int s_mirrorTex = Shader.PropertyToID("_MirrorTex");
        private static int s_reflecting;
        private Camera _camera;
        private RenderTexture _texture;
        private bool _isReflecting;

        private void OnEnable()
        {
            _camera = new GameObject("Mirror Camera").AddComponent<Camera>();
            _camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            _camera.enabled = false;
            _camera.depth = -10f;
            UniversalAdditionalCameraData data = _camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
        }

        private void OnDisable()
        {
            SetReflecting(false);
            Destroy(_camera.gameObject);

            if (_texture != null)
                _texture.Release();
        }

        private void LateUpdate()
        {
            Camera eye = BattleContext.Instance != null ? BattleContext.Instance.Camera : null;
            Transform plane = _surface.transform;
            Vector3 normal = -plane.forward;
            float distance = eye != null ? Vector3.Dot(eye.transform.position - plane.position, normal) : -1f;
            bool isActive = distance > 0f && distance < _renderDistance && _surface.isVisible;

            SetReflecting(distance > 0f && Vector3.Distance(eye.transform.position, plane.position) < _headDistance);
            _camera.enabled = isActive;

            if (!isActive)
                return;

            UpdateTexture();
            Transform view = eye.transform;
            Vector3 position = view.position - 2f * distance * normal;
            Vector3 forward = Vector3.Reflect(view.forward, normal);
            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.Reflect(view.up, normal)));
            _camera.fieldOfView = eye.fieldOfView;
            _camera.aspect = eye.aspect;
            _camera.nearClipPlane = eye.nearClipPlane;
            _camera.farClipPlane = eye.farClipPlane;
            _camera.cullingMask = eye.cullingMask;
            _camera.ResetProjectionMatrix();

            Matrix4x4 toCamera = _camera.worldToCameraMatrix;
            Vector3 planeNormal = toCamera.MultiplyVector(normal).normalized;
            Vector3 planePoint = toCamera.MultiplyPoint(plane.position);
            _camera.projectionMatrix = _camera.CalculateObliqueMatrix(
                new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, -Vector3.Dot(planeNormal, planePoint)));
        }

        private void UpdateTexture()
        {
            if (_texture != null && _texture.width == Screen.width && _texture.height == Screen.height)
                return;

            if (_texture != null)
                _texture.Release();

            _texture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.DefaultHDR);
            _camera.targetTexture = _texture;
            _surface.material.SetTexture(s_mirrorTex, _texture);
        }

        private void SetReflecting(bool isReflecting)
        {
            if (_isReflecting == isReflecting)
                return;

            _isReflecting = isReflecting;
            s_reflecting += isReflecting ? 1 : -1;
        }
    }
}
