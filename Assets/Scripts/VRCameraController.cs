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
    [SerializeField] private Button transitionButton; 

    [Header("Selection Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private float maxSelectionDistance = 10f;
    [SerializeField] private Material hoveredObjectMaterial;
    [SerializeField] private Material selectedObjectMaterial;

    [Header("Selection Settings")]
    [SerializeField] private string targetSceneName = "SampleScene";
    
    private GameObject hoveredObject;
    private GameObject selectedObject;
    private Material originalHoverMaterial;
    private Material originalSelectedMaterial;
    private ManipulationMode currentMode = ManipulationMode.None;
    private Vector3 originalScale;
    private SceneTriggerZone currentPortal;

    private enum ManipulationMode
    {
        None,
        Grab,
        Rotate,
        Scale
    }

    // Preset rotation angles (in degrees)
    private readonly float[] rotationPresets = { 0f, 90f, 180f, 270f };
    private int currentRotationIndex = 0;

    // Preset scale values
    private readonly float[] scalePresets = { 1f, 1.5f, 2f, 0.5f };
    private int currentScaleIndex = 0;

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
        
        // Setup transition button
        if (transitionButton)
        {
            transitionButton.onClick.AddListener(TriggerPortalTransition);
            transitionButton.gameObject.SetActive(false);
        }
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

                // Check if this is a portal
                SceneTriggerZone portalZone = hitObject.GetComponent<SceneTriggerZone>();
                if (portalZone != null)
                {
                    currentPortal = portalZone;
                    if (transitionButton != null)
                    {
                        transitionButton.gameObject.SetActive(true);
                    }
                }
                else
                {
                    currentPortal = null;
                    if (transitionButton != null)
                    {
                        transitionButton.gameObject.SetActive(false);
                    }
                }
            }
        }
        else
        {
            UnhoverCurrentObject();
            currentPortal = null;
            if (transitionButton != null)
            {
                transitionButton.gameObject.SetActive(false);
            }
        }
    }

    private void SetMode(ManipulationMode mode)
    {
        if (mode == currentMode)
        {
            // If already in this mode, cycle through presets
            switch (mode)
            {
                case ManipulationMode.Rotate:
                    currentRotationIndex = (currentRotationIndex + 1) % rotationPresets.Length;
                    break;
                case ManipulationMode.Scale:
                    currentScaleIndex = (currentScaleIndex + 1) % scalePresets.Length;
                    break;
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

            // Reset indices when changing modes
            if (mode == ManipulationMode.Rotate)
            {
                currentRotationIndex = 0;
            }
            else if (mode == ManipulationMode.Scale)
            {
                currentScaleIndex = 0;
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

        // Get target rotation based on current preset
        float targetAngle = rotationPresets[currentRotationIndex];
        
        // Smoothly rotate to target angle
        Vector3 currentRotation = selectedObject.transform.eulerAngles;
        float newYRotation = Mathf.LerpAngle(currentRotation.y, targetAngle, Time.deltaTime * 10f);
        selectedObject.transform.rotation = Quaternion.Euler(0, newYRotation, 0);

        // Check if we've reached the target angle (with some tolerance)
        if (Mathf.Abs(newYRotation - targetAngle) < 0.1f)
        {
            selectedObject.transform.rotation = Quaternion.Euler(0, targetAngle, 0);
        }
    }

    private void HandleScale()
    {
        if (selectedObject == null) return;

        // Get target scale based on current preset
        float targetScale = scalePresets[currentScaleIndex];
        
        // Smoothly scale to target size
        float currentScale = selectedObject.transform.localScale.x;
        float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * 5f);
        selectedObject.transform.localScale = Vector3.one * newScale;

        // Check if we've reached the target scale (with some tolerance)
        if (Mathf.Abs(newScale - targetScale) < 0.01f)
        {
            selectedObject.transform.localScale = Vector3.one * targetScale;
        }
    }

    private void ShowObjectInfo()
    {
        // Check if we have a hovered or selected object to show info for
        GameObject targetObject = selectedObject != null ? selectedObject : hoveredObject;
    
        if (targetObject != null)
        {
            ObjectInfo objectInfo = targetObject.GetComponent<ObjectInfo>();
            if (objectInfo != null && objectInfo.details != null)
            {
                // Show the popup with object details using the InfoPopupManager
                InfoPopupManager.Instance?.ShowPopup(objectInfo.details);
            }
            else
            {
                Debug.LogWarning("No ObjectInfo component or details found on the target object");
            }
        }
        else
        {
            InfoPopupManager.Instance?.HidePopup();
        }
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

    private void TriggerPortalTransition()
    {
        if (currentPortal != null)
        {
            currentPortal.TryTriggerTransition(targetSceneName);
        }
    }

    private void OnDisable()
    {
        UnhoverCurrentObject();
        DeselectObject();
    }
}