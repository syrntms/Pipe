using UnityEngine;
using Pipe.Data;
using Pipe.View;

namespace Pipe.Core
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _boardContainer;
        [SerializeField] private SpriteRenderer _backgroundPrefab; 
        
        private ThemeData _themeData;

        private Cell[,] _grid;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void InitializeGrid(LevelData levelData, ThemeData themeData)
        {
            _themeData = themeData;
            Width = levelData.Width;
            Height = levelData.Height;
            _grid = new Cell[Width, Height];

            // Initialize empty cells
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = new Cell(x, y);
                    
                    var cellView = Instantiate(_cellPrefab, _boardContainer);
                    cellView.Init(_grid[x, y]);
                }
            }

            // Place dots
            foreach (var dot in levelData.Dots)
            {
                if (IsWithinBounds(dot.Position.x, dot.Position.y))
                {
                    var cell = _grid[dot.Position.x, dot.Position.y];
                    cell.Type = CellType.Dot;
                    cell.ColorId = dot.Color;
                    
                    // Resolve Visual Color
                    if (_themeData != null)
                    {
                        cell.VisualColor = _themeData.GetColor(dot.Color);
                    }
                    
                    cell.IsOccupied = true;
                }
            }
            
            Debug.Log($"Grid initialized with {Width}x{Height}. Checking camera position...");
            
            CreateBoardBackground();
            CenterCamera();
        }

        [Header("Background Settings")]
        [SerializeField] private Color _bgBaseColor = new Color(0, 0.02f, 0.06f, 0.3f);
        [SerializeField] private Color _bgRimColor = new Color(0f, 0.8f, 1f, 1f);
        [SerializeField] private float _bgRimPower = 2.0f;
        [SerializeField] private float _bgSmoothness = 0.95f;

        private void CreateBoardBackground()
        {
            if (_backgroundPrefab != null)
            {
                var bg = Instantiate(_backgroundPrefab, _boardContainer);
                
                // Remove SpriteRenderer if present
                if (bg.GetComponent<SpriteRenderer>() != null) Destroy(bg.GetComponent<SpriteRenderer>());
                
                // Create Floor Plane
                 GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                 floor.transform.SetParent(bg.transform);
                 floor.transform.localPosition = Vector3.zero;
                 floor.transform.localScale = new Vector3((Width + 2) / 10f, 1f, (Height + 2) / 10f); // +2 for padding
                 
                 floor.transform.rotation = Quaternion.Euler(-90, 0, 0); // Face Camera (XY)
                 floor.transform.localPosition = new Vector3(0, 0, 0.5f); // Behind dots
                 
                 var renderer = floor.GetComponent<MeshRenderer>();
                 renderer.material = new Material(Shader.Find("Pipe/FrostedGlass"));
                 
                 // Make OPAQUE WHITE for visibility check
                 renderer.material.SetColor("_BaseColor", Color.white); 
                 renderer.material.SetColor("_RimColor", _bgRimColor);
                 renderer.material.SetFloat("_RimPower", _bgRimPower);
                 renderer.material.SetFloat("_Smoothness", _bgSmoothness);
                 
                 // Generate High contrast checkerboard to test transparency
                 Texture2D boardTex = GenerateCheckerboard(512, 8);
                 renderer.material.SetTexture("_BaseMap", boardTex);
                 
                // Position at center of grid
                float cx = (Width - 1) / 2f;
                float cy = (Height - 1) / 2f;
                bg.transform.position = new Vector3(cx, cy, 1f); 
            }
        }



        private Texture2D GenerateCheckerboard(int size, int divisions)
        {
            Texture2D tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point; // Sharp edges
            Color[] pixels = new Color[size * size];
            
            float cellSize = size / (float)divisions;
            
            Color col1 = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark Grey
            Color col2 = new Color(0.4f, 0.4f, 0.4f, 1f); // Lighter Grey
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = Mathf.FloorToInt(x / cellSize);
                    int cy = Mathf.FloorToInt(y / cellSize);
                    
                    if ((cx + cy) % 2 == 0) pixels[y*size+x] = col1;
                    else pixels[y*size+x] = col2;
                }
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void CenterCamera()
        {
            var cam = Camera.main;
            if (cam == null) cam = GameObject.FindObjectOfType<Camera>();
            if (cam == null)
            {
                 Debug.LogError("Main Camera not found!");
                 return;
            }

            float cx = (Width - 1) / 2f;
            float cy = (Height - 1) / 2f;

            // Setup Straight-On Orthographic View (Top-Down/Front-Facing)
            cam.orthographic = true;
            
            // Calculate Size
            float targetHeight = Height + 4f; 
            float targetWidth = Width + 2f;
            float screenAspect = (float)Screen.width / Screen.height;
            float neededSizeByHeight = targetHeight / 2f;
            float neededSizeByWidth = (targetWidth / screenAspect) / 2f;
            
            cam.orthographicSize = Mathf.Max(neededSizeByHeight, neededSizeByWidth);
            
            // Position Camera: Directly centered, looking down Z axis
            cam.transform.position = new Vector3(cx, cy, -10f);
            cam.transform.rotation = Quaternion.identity; // No rotation (0,0,0)
            
            // Ensure Light exists and is angled (for 3D shading on spheres/tubes)
            if (GameObject.FindObjectOfType<Light>() == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                // Angle like a sun from top-left
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        public Color GetVisualColor(PuzzleColor id)
        {
            if (_themeData != null) return _themeData.GetColor(id);
            return Color.white;
        }

        public Cell GetCell(int x, int y)
        {
            if (!IsWithinBounds(x, y)) return null;
            return _grid[x, y];
        }

        public bool IsWithinBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}
