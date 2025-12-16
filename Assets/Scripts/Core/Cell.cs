using System;
using UnityEngine;
using Pipe.Data;

namespace Pipe.Core
{
    public enum CellType
    {
        Empty,
        Dot,
        Pipe
    }

    [System.Serializable]
    public class Cell
    {
        public int X;
        public int Y;
        
        private CellType _type = CellType.Empty;
        public CellType Type {
            get => _type;
            set {
                if (_type != value) {
                    _type = value;
                    OnChanged?.Invoke();
                }
            }
        }
        
        // Logic Color
        public PuzzleColor ColorId;
        
        // Visual Color (Resolved from Theme)
        private Color _visualColor = Color.clear;
        public Color VisualColor {
            get => _visualColor;
            set {
                if (_visualColor != value) {
                    _visualColor = value;
                    OnChanged?.Invoke();
                }
            }
        }

        private bool _isOccupied = false;
        public bool IsOccupied {
            get => _isOccupied;
            set {
                if (_isOccupied != value) {
                    _isOccupied = value;
                    OnChanged?.Invoke();
                }
            }
        }

        public event Action OnChanged;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
