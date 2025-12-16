using UnityEngine;
using Pipe.Core;
using Pipe.Data;

namespace Pipe
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LevelData _defaultLevel;
        [SerializeField] private ThemeData _themeData;

        private void Start()
        {
            if (_gridManager != null && _defaultLevel != null && _themeData != null)
            {
                _gridManager.InitializeGrid(_defaultLevel, _themeData);
            }
            else
            {
                Debug.LogError("GameInitializer: GridManager, LevelData, or ThemeData is missing!");
            }
        }
    }
}
