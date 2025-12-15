using System.Collections.Generic;
using UnityEngine;
using Pipe.Core;

namespace Pipe.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Pipe/LevelData")]
    public class LevelData : ScriptableObject
    {
        public int Width = 5;
        public int Height = 5;
        public List<DotDefinition> Dots;
    }

    [System.Serializable]
    public class DotDefinition
    {
        public Vector2Int Position;
        public Color Color;
    }
}
