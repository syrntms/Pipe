using UnityEngine;
using Pipe.Core;

namespace Pipe.View
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseSprite; // Can keep for debug or remove
        // Replace Dot Sprite with Mesh
        private GameObject _dotObject;
        private MeshRenderer _dotRenderer;

        private Cell _cell;

        public void Init(Cell cell)
        {
            _cell = cell;
            transform.localPosition = new Vector3(cell.X, cell.Y, 0);
            _cell.OnChanged += UpdateVisuals;
            
            // Create 3D Dot Sphere
            _dotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _dotObject.transform.SetParent(transform);
            _dotObject.transform.localPosition = new Vector3(0, 0, -0.5f); // Pop out
            _dotObject.transform.localScale = Vector3.one * 0.6f;
            
            // Remove Collider
            Destroy(_dotObject.GetComponent<Collider>());
            
            _dotRenderer = _dotObject.GetComponent<MeshRenderer>();
            _dotRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit")); // PBR
            _dotRenderer.material.SetFloat("_Smoothness", 0.95f); // High gloss
            _dotRenderer.material.SetFloat("_Metallic", 0.1f); // Plastic/Glass look

            _dotRenderer.material.SetFloat("_Smoothness", 0.95f); // High gloss
            _dotRenderer.material.SetFloat("_Metallic", 0.1f); // Plastic/Glass look
            
            // Add BoxCollider for Input Raycasting (covers the cell area)
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.1f); // 1x1 flat box
            box.center = Vector3.zero;

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
                _dotObject.SetActive(true);
                // Set Albedo and Emission
                _dotRenderer.material.color = _cell.VisualColor;
                _dotRenderer.material.SetColor("_EmissionColor", _cell.VisualColor * 2f); // HDR Intensity
                _dotRenderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                _dotObject.SetActive(false);
            }
        }
    }
}
