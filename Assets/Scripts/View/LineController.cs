
using System.Collections.Generic;
using UnityEngine;
using Pipe.Core;

namespace Pipe.View
{
    // [RequireComponent(typeof(LineRenderer))] // No longer needed
    public class LineController : MonoBehaviour
    {
        // OLD: private LineRenderer _lineRenderer;
        
        // Revert 3D logic, back to LineRenderer
        private LineRenderer _lineRenderer;
        
        private Color _color;
        private Material _liquidMaterial;

        // Runtime Noise Texture
        private Texture2D _noiseTexture;

        [Header("Neon Settings")]
        [SerializeField] private float _flowSpeed = 0.5f;
        [SerializeField] private float _noiseScale = 2.0f;
        [SerializeField] private float _coreIntensity = 4.0f;
        [Header("Trail Settings")]
        [SerializeField] private float _trailDensity = 0.6f; // Lower = More drops
        [SerializeField] private float _trailSpeed = 2.0f;
        [SerializeField] private float _trailScale = 0.2f; // Scale UV.x (Stretching)
        
        [Header("Line Settings")]
        [SerializeField] private float _widthMultiplier = 0.3f;

        private void Awake()
        {
            // Ensure LineRenderer exists
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null) _lineRenderer = gameObject.AddComponent<LineRenderer>();
            
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.numCapVertices = 5;
            _lineRenderer.numCornerVertices = 5;
            _lineRenderer.sortingOrder = 2;
            _lineRenderer.widthMultiplier = _widthMultiplier;
            _lineRenderer.textureMode = LineTextureMode.Tile; // Essential for flow
        }

        [Header("Shader")]
        [SerializeField] private Shader _shaderOverride;

        public void Init(Color color)
        {
            _color = color;
            
            Shader shader = _shaderOverride;
            if (shader == null) shader = Shader.Find("Pipe/NeonLiquid");
            
            if (shader == null)
            {
                Debug.LogError("CRITICAL: Shader 'Pipe/NeonLiquid' NOT found and no override provided. Falling back to URP/Lit.");
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            
            if (shader != null)
            {
                _liquidMaterial = new Material(shader);
                // Use new Bubble noise
                if (_noiseTexture == null) _noiseTexture = GenerateBubbleNoise(256); // 256x256 texture
            
                _liquidMaterial.SetTexture("_NoiseTex", _noiseTexture);
                _liquidMaterial.SetColor("_MainColor", color);
                // Initial set
                UpdateMaterialProperties();
            
                _lineRenderer.material = _liquidMaterial;
                _lineRenderer.startColor = color; 
                _lineRenderer.endColor = color;
            }
            else
            {
                 Debug.LogError("CRITICAL: Failed to create ANY material for Line.");
            }
        }
        
        private void Update()
        {
            // Allow realtime tuning in Editor
            if (_liquidMaterial != null)
            {
                UpdateMaterialProperties();
            }
        }

        private void UpdateMaterialProperties()
        {
             _liquidMaterial.SetFloat("_FlowSpeed", _flowSpeed);
             _liquidMaterial.SetFloat("_NoiseScale", _noiseScale);
                _liquidMaterial.SetFloat("_CoreIntensity", _coreIntensity);
                _liquidMaterial.SetFloat("_TrailDensity", _trailDensity);
                _liquidMaterial.SetFloat("_TrailSpeed", _trailSpeed);
                _liquidMaterial.SetFloat("_TrailScale", _trailScale);
            
                _lineRenderer.material = _liquidMaterial; // This line was already present in Init, but the instruction implies adding it here.
                _liquidMaterial.SetFloat("_TrailScale", _trailScale); // This line was already present above.
        }

        public void UpdateLine(List<Cell> path)
        {

            
            if (path == null || path.Count < 1)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            _lineRenderer.positionCount = path.Count;
            float zPos = -0.5f; // Align with CellView spheres

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 pos = new Vector3(path[i].X, path[i].Y, zPos);
                _lineRenderer.SetPosition(i, pos);
            }
        }
        
        private Texture2D GenerateBubbleNoise(int size)
        {
             Texture2D tex = new Texture2D(size, size);
             tex.wrapMode = TextureWrapMode.Repeat;
             tex.filterMode = FilterMode.Bilinear;
             Color[] pixels = new Color[size * size];
             
             // Number of "drops" or blobs
             int numSeeds = 20; 
             List<Vector2> seeds = new List<Vector2>();
             for(int i=0; i<numSeeds; i++) {
                 seeds.Add(new Vector2(Random.Range(0f, size), Random.Range(0f, size)));
             }
             
             float maxDist = size / 4.0f; // Radius of largest blob influence
             
             for(int y=0; y<size; y++)
             {
                 for(int x=0; x<size; x++)
                 {
                     float minDist = maxDist;
                     
                     // Find distance to nearest seed (with Tiling support)
                     foreach(var seed in seeds)
                     {
                         // Check 9 neighbor tiles for seamless wrapping
                         for(int ox = -1; ox <= 1; ox++)
                         {
                             for(int oy = -1; oy <= 1; oy++)
                             {
                                 Vector2 offset = new Vector2(ox * size, oy * size);
                                 float dist = Vector2.Distance(new Vector2(x, y), seed + offset);
                                 if(dist < minDist) minDist = dist;
                             }
                         }
                     }
                     
                     // Invert distance: Close = 1 (White), Far = 0 (Black)
                     // Using smoothstep for rounder tops
                     float normDist = minDist / maxDist;
                     float val = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, normDist);
                     
                     pixels[y*size+x] = new Color(val, val, val, 1f);
                 }
             }
             tex.SetPixels(pixels);
             tex.Apply();
             return tex;
        }
    }
}
