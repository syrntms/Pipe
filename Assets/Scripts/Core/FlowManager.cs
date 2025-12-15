using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Pipe.Data;
using Pipe.View;

namespace Pipe.Core
{
    public class FlowManager : MonoBehaviour
    {
        public static FlowManager Instance { get; private set; }

        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LineController _linePrefab;
        [SerializeField] private Transform _linesContainer;
        
        // Input logic
        private bool _isDragging = false;
        private Color _currentPathColor;
        private List<Cell> _currentPath;
        private Cell _startCell;

        // Store paths per color. Key: Color, Value: List of Cells in the path
        private Dictionary<Color, List<Cell>> _activePaths = new Dictionary<Color, List<Cell>>();
        private Dictionary<Color, LineController> _lineControllers = new Dictionary<Color, LineController>();

        private Camera _mainCamera;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // Simple approach using legacy input for rapid prototyping if Input System setup is complex,
            // but we will try to use Mouse/Touch.
            
            bool isPressed = Mouse.current.leftButton.isPressed || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
            bool wasPressed = Mouse.current.leftButton.wasPressedThisFrame || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            bool wasReleased = Mouse.current.leftButton.wasReleasedThisFrame || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame);
            Vector2 position = Mouse.current.position.ReadValue();
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (wasPressed)
            {
                // Verify camera presence
                if (_mainCamera == null) _mainCamera = Camera.main;
                OnInputDown(position);
            }
            else if (isPressed && _isDragging)
            {
                OnInputDrag(position);
            }
            else if (wasReleased)
            {
                OnInputUp();
            }
        }

        private void OnInputDown(Vector2 screenPosition)
        {
            Cell cell = GetCellFromScreenPos(screenPosition);
            if (cell == null) 
            {
                Debug.Log($"InputDown at {screenPosition}: No Cell found.");
                return;
            }
            
            Debug.Log($"InputDown on Cell: {cell.X}, {cell.Y} Type: {cell.Type}");

            if (cell.Type == CellType.Dot)
            {
                StartPath(cell);
            }
            else if (cell != null && cell.Type == CellType.Pipe)
            {
                 // Optional: Resume path or clear from this point (Advanced feature)
                 // For MVP, only start from Dot or existing path end? 
                 // Let's stick to simple: Start from Dot.
                 // Actually, common Flow behavior allows picking up a pipe end.
                 if (_activePaths.ContainsKey(cell.Color))
                 {
                     // Check if this cell is the end of the existing path
                     var path = _activePaths[cell.Color];
                     if (path[path.Count - 1] == cell)
                     {
                         ContinuePath(cell, cell.Color);
                     }
                 }
            }
        }

        private void OnInputDrag(Vector2 screenPosition)
        {
            Cell cell = GetCellFromScreenPos(screenPosition);
            if (cell != null && cell != _currentPath[_currentPath.Count - 1])
            {
                TryAddToPath(cell);
            }
        }

        private void OnInputUp()
        {
            _isDragging = false;
            _currentPath = null;
            _startCell = null;
            // Validate win condition here or update UI
            CheckWinCondition();
        }

        private void StartPath(Cell startCell)
        {
            _isDragging = true;
            _startCell = startCell;
            _currentPathColor = startCell.Color;

            // Update or Create path list
            if (!_activePaths.ContainsKey(_currentPathColor))
            {
                _activePaths[_currentPathColor] = new List<Cell>();
            }
            
            // Manage LineController
            if (!_lineControllers.ContainsKey(_currentPathColor))
            {
                var line = Instantiate(_linePrefab, _linesContainer);
                line.Init(_currentPathColor);
                _lineControllers[_currentPathColor] = line;
            }
            
            // If starting from a dot, clear previous path for this color usually?
            // In Flow, if you drag from a dot, you overwrite the old path.
            ClearPath(_currentPathColor);
            
            _currentPath = _activePaths[_currentPathColor];
            _currentPath.Add(startCell);
            
            UpdateLineVisuals(_currentPathColor);
        }
        
        private void ContinuePath(Cell currentEnd, Color color)
        {
             _isDragging = true;
             _currentPathColor = color;
             _currentPath = _activePaths[color];
             _startCell = _currentPath[0]; // The dot that started this path
        }

        private void TryAddToPath(Cell cell)
        {
            // Validation Logic
            // 1. Must be adjacent
            Cell lastCell = _currentPath[_currentPath.Count - 1];
            if (!IsAdjacent(lastCell, cell)) return;

            // 2. Check collisions
            if (cell.Type == CellType.Dot)
            {
                // Can only connect to the SAME color dot, and it must finish the path
                if (cell.Color != _currentPathColor) return;
                
                // Add and Finish
                AddToPath(cell);
                _isDragging = false; // Auto stop dragging when connected
                return;
            }

            if (cell.IsOccupied)
            {
                // If it's occupied by SAME color, we might be backtracking?
                if (cell.Color == _currentPathColor)
                {
                    // Backtracking logic: truncate path to here
                    int index = _currentPath.IndexOf(cell);
                    if (index != -1 && index < _currentPath.Count - 1)
                    {
                        // Remove everything after this index
                        TruncatePath(_currentPathColor, index);
                    }
                    return;
                }
                
                // If occupied by DIFFERENT color, block or cut?
                // Classic Flow: Blocks. Or if you want "Bridge" content, it cuts.
                // Easy version: Blocks.
                return;
            }

            // Valid empty cell
            AddToPath(cell);
        }

        private void AddToPath(Cell cell)
        {
            cell.IsOccupied = true;
            cell.Color = _currentPathColor;
            
            // Visual update: If it's not a dot, we mark it as pipe roughly?
            // Actually Type should probably update to Pipe if it was Empty
            if (cell.Type == CellType.Empty) cell.Type = CellType.Pipe;
            
            _currentPath.Add(cell);
            
            UpdateLineVisuals(_currentPathColor);
        }

        public void ClearPath(Color color)
        {
             if (!_activePaths.ContainsKey(color)) return;
             
             var path = _activePaths[color];
             foreach (var c in path)
             {
                 if (c.Type == CellType.Pipe)
                 {
                     c.Type = CellType.Empty;
                     c.IsOccupied = false;
                     c.Color = Color.clear;
                 }
                 // Keep Dots as Dots
             }
             path.Clear();
             
             UpdateLineVisuals(color);
        }
        
        private void TruncatePath(Color color, int newEndIndex)
        {
            var path = _activePaths[color];
            for (int i = path.Count - 1; i > newEndIndex; i--)
            {
                var c = path[i];
                 if (c.Type == CellType.Pipe)
                 {
                     c.Type = CellType.Empty;
                     c.IsOccupied = false;
                     c.Color = Color.clear;
                 }
                path.RemoveAt(i);
            }
            
            UpdateLineVisuals(color);
        }

        private void UpdateLineVisuals(Color color)
        {
            if (_lineControllers.ContainsKey(color))
            {
                _lineControllers[color].UpdateLine(_activePaths[color]);
            }
        }

        private bool IsAdjacent(Cell a, Cell b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            return (dx + dy) == 1;
        }

        private Cell GetCellFromScreenPos(Vector2 screenPos)
        {
            // Raycast or WorldPosition conversion
            if (_mainCamera == null) return null;
            
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_mainCamera.transform.position.z));
            
            // Convert world pos to grid coordinates.
            // Assuming Grid starts at 0,0 and cells are size 1x1.
            // And assuming NO Rotation or Scale on Grid for now.
            // Also need to consider cells are centered or corner pivot? 
            // Usually simply rounding works if pivot is center (0.5, 0.5) at (0,0)? 
            // Wait, in GridManager we instantiated at (x, y). 
            // So Cell(0,0) is at World(0,0,0). 
            // Which means the cell area is probably form -0.5 to 0.5? Or 0 to 1?
            // Default Sprite 100ppu usually means 1 unit size.
            // Let's assume Pivot is Center. So Cell 0,0 is centered at 0,0.
            // Range x: -0.5 to 0.5. Range y: -0.5 to 0.5.
            
            int x = Mathf.RoundToInt(worldPos.x); 
            int y = Mathf.RoundToInt(worldPos.y);
            
            // Debug check
            // Debug.Log($"Screen: {screenPos} World: {worldPos} Grid: {x},{y}");
            
            return _gridManager.GetCell(x, y);
        }
        
        private void CheckWinCondition()
        {
            // 1. Check if all paths are valid (Dot to Dot)
            // We need to know how many colors exist. 
            // Better: Iterate through all defined dots in LevelData?
            // Or assume if user has filled the board and connected everything...
            
            // Let's iterate over _gridManager to check for empty cells first
            for (int x = 0; x < _gridManager.Width; x++)
            {
                for (int y = 0; y < _gridManager.Height; y++)
                {
                    Cell cell = _gridManager.GetCell(x, y);
                    if (!cell.IsOccupied && cell.Type != CellType.Dot) 
                    {
                        // Found an empty cell that is not a dot (although dots are occupied usually)
                        // Actually dots are IsOccupied=true.
                        // So just !IsOccupied is enough
                        return;
                    }
                }
            }

            // 2. Check if all Colors have a completed path
            // We really need access to LevelData to know how many pairs exist.
            // But checking if all cells are filled is often "almost" enough in Flow, 
            // EXCEPT you could have filled cells with a partial path that doesn't reach the end.
            
            foreach (var kvp in _activePaths)
            {
                var path = kvp.Value;
                if (path.Count < 2) return; // Cannot be complete
                
                Cell start = path[0];
                Cell end = path[path.Count - 1];
                
                if (start.Type != CellType.Dot || end.Type != CellType.Dot)
                {
                    // Path is not connected dot-to-dot
                    return;
                }
            }
            
            // If we are here, grid is full and all active paths connect dot-to-dot.
            // (Assuming there are no colors without ANY path created, which is impossible if grid is full)
            
            Debug.Log("LEVEL COMPLETE!");
            // TODO: UI Event
        }
        
        // Accessor for View
        public List<Cell> GetPath(Color color)
        {
             if (_activePaths.ContainsKey(color)) return _activePaths[color];
             return null;
        }
    }
}
