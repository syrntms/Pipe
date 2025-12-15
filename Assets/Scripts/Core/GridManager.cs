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

        private Cell[,] _grid;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void InitializeGrid(LevelData levelData)
        {
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
                    
                    // Allow user to set color in Inspector, but force Alpha to 1 if it's 0 (common mistake)
                    Color color = dot.Color;
                    if (color.a <= 0.01f) color.a = 1f;
                    
                    cell.Color = color;
                    cell.IsOccupied = true;
                }
            }
            
            Debug.Log($"Grid initialized with {Width}x{Height}. Checking camera position...");
            CenterCamera();
        }

        private void CenterCamera()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                // Center point
                float cx = (Width - 1) / 2f;
                float cy = (Height - 1) / 2f;
                camera.transform.position = new Vector3(cx, cy, -10f);
                
                // Adjust size to fit height with some padding
                camera.orthographic = true;
                camera.orthographicSize = (Height / 2f) + 1f;
                
                Debug.Log($"Camera moved to {camera.transform.position} with size {camera.orthographicSize}");
            }
            else
            {
                Debug.LogError("Main Camera not found!");
            }
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
