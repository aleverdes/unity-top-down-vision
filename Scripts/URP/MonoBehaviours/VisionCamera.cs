using System;
using UnityEngine;

namespace GrozaGames.TopDownVision.URP.MonoBehaviours
{
    [RequireComponent(typeof(Camera))]
    public class VisionCamera : MonoBehaviour
    {
        public static readonly int VisionGlobalMaskShaderProperty = Shader.PropertyToID("_VisionGlobalMask");
        
        [SerializeField] private Camera _camera;
        [SerializeField, Range(0, 1)] private float _renderScale = 0.5f;
        [SerializeField] private int _maxRenderTextureSize = 1024;

        private RenderTexture _visionRenderTexture;
        private Vector2Int _prevScreenSize;
        private float _prevRenderScale;

        private void Reset()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            var screenSize = new Vector2(Screen.width, Screen.height) * _renderScale;
            if (screenSize.x > _maxRenderTextureSize || screenSize.y > _maxRenderTextureSize)
            {
                if (screenSize.x > screenSize.y)
                    screenSize *= _maxRenderTextureSize / screenSize.x;
                else
                    screenSize *= _maxRenderTextureSize / screenSize.y;
            }
            
            _visionRenderTexture = new RenderTexture((int) screenSize.x, (int) screenSize.y, 8);
            _camera.targetTexture = _visionRenderTexture;

            _prevScreenSize = new Vector2Int(Screen.width, Screen.height);
            _prevRenderScale = _renderScale;
            
            Shader.SetGlobalTexture(VisionGlobalMaskShaderProperty, _visionRenderTexture);
                
            _camera.aspect = screenSize.x / screenSize.y;
            _camera.enabled = true;
        }

        private void OnDisable()
        {
            _camera.targetTexture = null;
            _visionRenderTexture.Release();
            Destroy(_visionRenderTexture);
            
            Shader.SetGlobalTexture(VisionGlobalMaskShaderProperty, null);
            
            _camera.enabled = false;
        }

        private void Update()
        {
            if (_prevScreenSize.x != Screen.width || _prevScreenSize.y != Screen.height || Math.Abs(_prevRenderScale - _renderScale) > 0.0001f)
            {
                var screenSize = new Vector2(Screen.width, Screen.height) * _renderScale;
                if (screenSize.x > _maxRenderTextureSize || screenSize.y > _maxRenderTextureSize)
                {
                    if (screenSize.x > screenSize.y)
                        screenSize *= _maxRenderTextureSize / screenSize.x;
                    else
                        screenSize *= _maxRenderTextureSize / screenSize.y;
                }
                
                _visionRenderTexture.Release();
                _visionRenderTexture.width = (int) screenSize.x;
                _visionRenderTexture.height = (int) screenSize.y;
                _visionRenderTexture.Create();
                
                _camera.aspect = screenSize.x / screenSize.y;
            }

            _prevScreenSize = new Vector2Int(Screen.width, Screen.height);
            _prevRenderScale = _renderScale;
        }
    }
}