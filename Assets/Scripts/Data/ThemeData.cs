using System.Collections.Generic;
using UnityEngine;

namespace Pipe.Data
{
    [CreateAssetMenu(fileName = "ThemeData", menuName = "Pipe/ThemeData")]
    public class ThemeData : ScriptableObject
    {
        [System.Serializable]
        public class ColorMapping
        {
            public PuzzleColor Id;
            [ColorUsage(true, true)] // Support HDR
            public Color Color = Color.white;
        }

        public List<ColorMapping> Colors;

        private Dictionary<PuzzleColor, Color> _lookup;

        public Color GetColor(PuzzleColor id)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<PuzzleColor, Color>();
                foreach (var mapping in Colors)
                {
                    if (!_lookup.ContainsKey(mapping.Id))
                    {
                        _lookup.Add(mapping.Id, mapping.Color);
                    }
                }
            }

            if (_lookup.TryGetValue(id, out Color color))
            {
                return color;
            }
            return Color.white; // Default fallback
        }
    }
}
