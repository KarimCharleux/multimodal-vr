using UnityEngine;
using UnityEngine.UI;

public class VRCameraController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform xrRig;
    [SerializeField] private Transform cameraOffset;
    [SerializeField] private Camera mainCamera;

    [Header("Joystick Controls")]
    [SerializeField] private DynamicJoystick moveJoystick;
    [SerializeField] private DynamicJoystick rotateJoystick;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("UI Controls")]
    [SerializeField] private Button grabButton;
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button scaleButton;
    [SerializeField] private Button infoButton;

    [Header("Selection Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private float maxSelectionDistance = 10f;
    [SerializeField] private Material hoveredObjectMaterial;
    [SerializeField] private Material selectedObjectMaterial;

    private GameObject hoveredObject;
    private GameObject selectedObject;
    private Material originalHoverMaterial;
    private Material originalSelectedMaterial;
    private ManipulationMode currentMode = ManipulationMode.None;
    private Vector3 originalScale;

    private enum ManipulationMode
    {
        None,
        Grab,
        Rotate,
        Scale
    }

    private void Start()
    {
        SetupComponents();
        SetupButtons();
    }

    private void SetupComponents()
    {
        // Find camera components if not assigned
        if (xrRig == null) xrRig = transform;
        if (cameraOffset == null) cameraOffset = transform.Find("Camera Offset");
        if (mainCamera == null) mainCamera = GetComponentInChildren<Camera>();

        // Setup default materials
        if (hoveredObjectMaterial == null)
        {
            hoveredObjectMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(1f, 1f, 0f, 0.5f)
            };
        }

        if (selectedObjectMaterial == null)
        {
            selectedObjectMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(1f, 0.6f, 0f, 0.8f)
            };
        }
    }

    private void SetupButtons()
    {
        if (grabButton) grabButton.onClick.AddListener(() => SetMode(ManipulationMode.Grab));
        if (rotateButton) rotateButton.onClick.AddListener(() => SetMode(ManipulationMode.Rotate));
        if (scaleButton) scaleButton.onClick.AddListener(() => SetMode(ManipulationMode.Scale));
        if (infoButton) infoButton.onClick.AddListener(ShowObjectInfo);
    }

    private void Update()
    {
        HandleCameraMovement();
        HandleCameraRotation();
        CheckHover();
        
        if (selectedObject != null)
        {
            HandleObjectManipulation();
        }
    }

    private void HandleCameraMovement()
    {
        if (moveJoystick == null || xrRig == null) return;

        Vector3 movement = new Vector3(moveJoystick.Horizontal, 0, moveJoystick.Vertical);
        if (movement.magnitude > 0.1f)
        {
            // Transform movement direction relative to camera orientation
            movement = mainCamera.transform.TransformDirection(movement);
            movement.y = 0; // Keep movement horizontal
            movement.Normalize();

            // Move the XR Rig
            xrRig.position += movement * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleCameraRotation()
    {
        if (rotateJoystick == null || cameraOffset == null) return;

        float rotationX = rotateJoystick.Vertical * rotationSpeed;
        float rotationY = rotateJoystick.Horizontal * rotationSpeed;

        if (Mathf.Abs(rotationX) > 0.1f || Mathf.Abs(rotationY) > 0.1f)
        {
            // Rotate camera offset (handles Y rotation)
            cameraOffset.Rotate(Vector3.up, rotationY, Space.World);

            // Rotate camera (handles X rotation)
            Vector3 currentRotation = mainCamera.transform.localEulerAngles;
            float newXRotation = currentRotation.x - rotationX;
            
            // Clamp vertical rotation
            if (newXRotation > 180f) newXRotation -= 360f;
            newXRotation = Mathf.Clamp(newXRotation, -80f, 80f);
            
            mainCamera.transform.localEulerAngles = new Vector3(newXRotation, currentRotation.y, 0);
        }
    }

    private void CheckHover()
    {
        if (currentMode != ManipulationMode.None) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxSelectionDistance, selectableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (hitObject != hoveredObject)
            {
                UnhoverCurrentObject();
                hoveredObject = hitObject;
                var renderer = hoveredObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    originalHoverMaterial = renderer.material;
                    renderer.material = hoveredObjectMaterial;
                }
            }
        }
        else
        {
            UnhoverCurrentObject();
        }
    }

    private void SetMode(ManipulationMode mode)
    {
        if (mode == currentMode)
        {
            // Toggle off
            currentMode = ManipulationMode.None;
            if (selectedObject != null)
            {
                DeselectObject();
            }
        }
        else
        {
            // Change mode
            currentMode = mode;
            if (hoveredObject != null && selectedObject == null)
            {
                SelectObject(hoveredObject);
            }
        }

        UpdateButtonVisuals();
    }

    private void UpdateButtonVisuals()
    {
        if (grabButton) grabButton.GetComponent<Image>().color = currentMode == ManipulationMode.Grab ? Color.yellow : Color.white;
        if (rotateButton) rotateButton.GetComponent<Image>().color = currentMode == ManipulationMode.Rotate ? Color.yellow : Color.white;
        if (scaleButton) scaleButton.GetComponent<Image>().color = currentMode == ManipulationMode.Scale ? Color.yellow : Color.white;
    }

    private void HandleObjectManipulation()
    {
        switch (currentMode)
        {
            case ManipulationMode.Grab:
                HandleGrab();
                break;
            case ManipulationMode.Rotate:
                HandleRotation();
                break;
            case ManipulationMode.Scale:
                HandleScale();
                break;
        }
    }

    private void HandleGrab()
    {
        if (selectedObject == null) return;

        // Position object in front of camera
        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * 2f;
        selectedObject.transform.position = Vector3.Lerp(
            selectedObject.transform.position,
            targetPosition,
            Time.deltaTime * 10f
        );
    }

    private void HandleRotation()
    {
        if (selectedObject == null) return;

        // Use move joystick for rotation when in rotate mode
        float rotationX = moveJoystick.Vertical * rotationSpeed * 50f;
        float rotationY = moveJoystick.Horizontal * rotationSpeed * 50f;

        selectedObject.transform.Rotate(Vector3.right, rotationX * Time.deltaTime, Space.World);
        selectedObject.transform.Rotate(Vector3.up, rotationY * Time.deltaTime, Space.World);
    }

    private void HandleScale()
    {
        if (selectedObject == null) return;

        // Use move joystick's vertical axis for scaling
        float scaleFactor = 1f + (moveJoystick.Vertical * Time.deltaTime);
        selectedObject.transform.localScale *= scaleFactor;
    }

    private void ShowObjectInfo()
    {
        // Implement object info display logic here
        Debug.Log("Show object info");
    }

    private void SelectObject(GameObject obj)
    {
        UnhoverCurrentObject();
        
        selectedObject = obj;
        var renderer = selectedObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalSelectedMaterial = renderer.material;
            renderer.material = selectedObjectMaterial;
        }
        
        originalScale = selectedObject.transform.localScale;
    }

    private void DeselectObject()
    {
        if (selectedObject != null)
        {
            var renderer = selectedObject.GetComponent<Renderer>();
            if (renderer != null && originalSelectedMaterial != null)
            {
                renderer.material = originalSelectedMaterial;
            }
            selectedObject = null;
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

    private void OnDisable()
    {
        UnhoverCurrentObject();
        DeselectObject();
    }
}