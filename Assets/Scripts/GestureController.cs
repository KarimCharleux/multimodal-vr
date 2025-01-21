using UnityEngine;

public class GestureController : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private float maxSelectionDistance = 10f;
    [SerializeField] private Material hoveredObjectMaterial;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    
    private Camera mainCamera;
    private GameObject hoveredObject;
    private Material originalHoverMaterial;
    private SceneTriggerZone currentPortal;
    
    // Crosshair settings
    private bool showCrosshair = true;
    private float crosshairSize = 20f;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No Main Camera found!");
            enabled = false;
            return;
        }

        if (hoveredObjectMaterial == null)
        {
            hoveredObjectMaterial = new Material(Shader.Find("Standard"));
            hoveredObjectMaterial.color = new Color(1f, 1f, 0f, 0.5f);
        }
    }

    private void Update()
    {
        CheckHover();
        
        // Check for portal interaction
        if (currentPortal != null && Input.GetKeyDown(KeyCode.E))
        {
            currentPortal.TryTriggerTransition();
        }
    }

    private void CheckHover()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxSelectionDistance, selectableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (hoveredObject != hitObject)
            {
                UnhoverCurrentObject();
                
                hoveredObject = hitObject;
                var renderer = hoveredObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    originalHoverMaterial = renderer.material;
                    renderer.material = hoveredObjectMaterial;
                }

                // Check if it's a portal
                currentPortal = hitObject.GetComponent<SceneTriggerZone>();
                if (currentPortal != null)
                {
                    DebugLog("Portal highlighted - Press E to activate");
                }
            }
        }
        else
        {
            UnhoverCurrentObject();
            currentPortal = null;
        }
    }

    private void UnhoverCurrentObject()
    {
        if (hoveredObject != null)
        {
            var renderer = hoveredObject.GetComponent<Renderer>();
            if (renderer != null && originalHoverMaterial != null)
            {
                renderer.material = originalHoverMaterial;
            }
            hoveredObject = null;
        }
    }

    private void OnGUI()
    {
        if (showCrosshair)
        {
            float centerX = Screen.width / 2;
            float centerY = Screen.height / 2;
            
            GUI.color = hoveredObject != null ? Color.yellow : Color.white;
            GUI.DrawTexture(
                new Rect(centerX - crosshairSize/2, centerY - 1, crosshairSize, 2), 
                Texture2D.whiteTexture
            );
            GUI.DrawTexture(
                new Rect(centerX - 1, centerY - crosshairSize/2, 2, crosshairSize), 
                Texture2D.whiteTexture
            );
        }
    }

    private void OnDisable()
    {
        UnhoverCurrentObject();
        currentPortal = null;
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[GestureController] {message}");
        }
    }
}