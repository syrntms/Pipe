using UnityEngine;
using Pipe.Core;
using Pipe.Data;

namespace Pipe
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LevelData _defaultLevel;

        private void Start()
        {
            if (_gridManager != null && _defaultLevel != null)
            {
                _gridManager.InitializeGrid(_defaultLevel);
            }
            else
            {
                Debug.LogError("GameInitializer: GridManager or LevelData is missing!");
            }
        }
    }
}
