using System.Collections.Generic;
using UnityEngine;
using Pipe.Core;

namespace Pipe.View
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineController : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private Color _color;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true; 
            _lineRenderer.numCapVertices = 5;
            _lineRenderer.numCornerVertices = 5;
            _lineRenderer.sortingOrder = 2; // Above Dots (1) and Cells (0)
            _lineRenderer.widthMultiplier = 0.3f; // Ensure visible width
        }

        public void Init(Color color)
        {
            _color = color;
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
        }

        public void UpdateLine(List<Cell> path)
        {
            if (path == null || path.Count < 1)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            _lineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                // Assuming Cell coordinates map directly to World coordinates (offset might be needed later)
                // z = -0.1f to be above the grid
                Vector3 pos = new Vector3(path[i].X, path[i].Y, -0.1f);
                _lineRenderer.SetPosition(i, pos);
            }
        }
    }
}
