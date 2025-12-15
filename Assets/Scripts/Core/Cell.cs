using System;
using UnityEngine;

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
        
        private Color _color = Color.clear;
        public Color Color {
            get => _color;
            set {
                if (_color != value) {
                    _color = value;
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
