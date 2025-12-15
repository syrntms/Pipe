using UnityEngine;
using Pipe.Core;

namespace Pipe.View
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseSprite;
        [SerializeField] private SpriteRenderer _dotSprite;

        private Cell _cell;

        public void Init(Cell cell)
        {
            _cell = cell;
            transform.localPosition = new Vector3(cell.X, cell.Y, 0);
            
            // Safety Check: Ensure Dot is correctly positioned and identified
            if (_dotSprite != null)
            {
                _dotSprite.transform.localPosition = Vector3.zero;
                _dotSprite.transform.localScale = new Vector3(0.5f, 0.5f, 1f); // Make dot slightly smaller than cell
                _dotSprite.gameObject.SetActive(true);
            }

            _cell.OnChanged += UpdateVisuals;
            UpdateVisuals();
        }

        private void OnDestroy()
        {
            if (_cell != null)
            {
                _cell.OnChanged -= UpdateVisuals;
            }
        }

        public void UpdateVisuals()
        {
            if (_cell.Type == CellType.Dot)
            {
                _dotSprite.enabled = true;
                _dotSprite.color = _cell.Color;
                // Ensure dot is drawn above the base
                _dotSprite.sortingOrder = 1; 
            }
            else
            {
                _dotSprite.enabled = false;
            }
        }
    }
}
