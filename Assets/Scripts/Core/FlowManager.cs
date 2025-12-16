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
        private PuzzleColor _currentPathColorId;
        private List<Cell> _currentPath;
        private Cell _startCell;

        // Store paths per color. Key: PuzzleColor, Value: List of Cells in the path
        private Dictionary<PuzzleColor, List<Cell>> _activePaths = new Dictionary<PuzzleColor, List<Cell>>();
        private Dictionary<PuzzleColor, LineController> _lineControllers = new Dictionary<PuzzleColor, LineController>();

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
                 if (_activePaths.ContainsKey(cell.ColorId))
                 {
                     // Check if this cell is the end of the existing path
                     var path = _activePaths[cell.ColorId];
                     if (path[path.Count - 1] == cell)
                     {
                         ContinuePath(cell, cell.ColorId);
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
            _currentPathColorId = startCell.ColorId;

            // Update or Create path list
            if (!_activePaths.ContainsKey(_currentPathColorId))
            {
                _activePaths[_currentPathColorId] = new List<Cell>();
            }
            
            // Manage LineController
            if (!_lineControllers.ContainsKey(_currentPathColorId))
            {
                var line = Instantiate(_linePrefab, _linesContainer);
                // Resolve visual color
                Color visualColor = _gridManager.GetVisualColor(_currentPathColorId);
                line.Init(visualColor);
                _lineControllers[_currentPathColorId] = line;
            }
            
            // If starting from a dot, clear previous path for this color usually?
            // In Flow, if you drag from a dot, you overwrite the old path.
            ClearPath(_currentPathColorId);
            
            _currentPath = _activePaths[_currentPathColorId];
            _currentPath.Add(startCell);
            
            UpdateLineVisuals(_currentPathColorId);
        }
        
        private void ContinuePath(Cell currentEnd, PuzzleColor colorId)
        {
             _isDragging = true;
             _currentPathColorId = colorId;
             _currentPath = _activePaths[colorId];
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
                if (cell.ColorId != _currentPathColorId) return;
                
                // Add and Finish
                AddToPath(cell);
                _isDragging = false; // Auto stop dragging when connected
                return;
            }

            if (cell.IsOccupied)
            {
                // If it's occupied by SAME color, we might be backtracking?
                if (cell.ColorId == _currentPathColorId)
                {
                    // Backtracking logic: truncate path to here
                    int index = _currentPath.IndexOf(cell);
                    if (index != -1 && index < _currentPath.Count - 1)
                    {
                        // Remove everything after this index
                        TruncatePath(_currentPathColorId, index);
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
            cell.ColorId = _currentPathColorId;
            // Also update visual color to show which pipe is here
            cell.VisualColor = _gridManager.GetVisualColor(_currentPathColorId);
            
            // Visual update: If it's not a dot, we mark it as pipe roughly?
            // Actually Type should probably update to Pipe if it was Empty
            if (cell.Type == CellType.Empty) cell.Type = CellType.Pipe;
            
            _currentPath.Add(cell);
            
            UpdateLineVisuals(_currentPathColorId);
        }

        public void ClearPath(PuzzleColor colorId)
        {
             if (!_activePaths.ContainsKey(colorId)) return;
             
             var path = _activePaths[colorId];
             foreach (var c in path)
             {
                 if (c.Type == CellType.Pipe)
                 {
                     c.Type = CellType.Empty;
                     c.IsOccupied = false;
                     // Reset visuals
                     c.VisualColor = Color.clear;
                 }
                 // Keep Dots as Dots
             }
             path.Clear();
             
             UpdateLineVisuals(colorId);
        }
        
        private void TruncatePath(PuzzleColor colorId, int newEndIndex)
        {
            var path = _activePaths[colorId];
            for (int i = path.Count - 1; i > newEndIndex; i--)
            {
                var c = path[i];
                 if (c.Type == CellType.Pipe)
                 {
                     c.Type = CellType.Empty;
                     c.IsOccupied = false;
                     c.VisualColor = Color.clear;
                 }
                path.RemoveAt(i);
            }
            
            UpdateLineVisuals(colorId);
        }

        private void UpdateLineVisuals(PuzzleColor colorId)
        {
            if (_lineControllers.ContainsKey(colorId))
            {
                _lineControllers[colorId].UpdateLine(_activePaths[colorId]);
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
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) _mainCamera = FindObjectOfType<Camera>();
            if (_mainCamera == null) return null;
            
            // Create Ray from camera
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            
            // Raycast against 3D Colliders (CellViews now have BoxColliders)
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // We hit a cell!
                CellView view = hit.collider.GetComponent<CellView>();
                if (view != null) 
                {
                    // If we could access the cell directly from view, that would be best.
                    // But CellView doesn't expose public Cell? It does via field but maybe not public property?
                    // Let's assume Grid coordinate based on Position is safer if we know grid logic.
                    // CellView Init sets transform.localPosition to (Cell.X, Cell.Y, 0).
                    // So we can assume hit.transform.localPosition indicates X,Y.
                    
                    int x = Mathf.RoundToInt(hit.transform.localPosition.x);
                    int y = Mathf.RoundToInt(hit.transform.localPosition.y);
                    return _gridManager.GetCell(x, y);
                }
            }
            
            return null;
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
        public List<Cell> GetPath(PuzzleColor colorId)
        {
             if (_activePaths.ContainsKey(colorId)) return _activePaths[colorId];
             return null;
        }
    }
}
