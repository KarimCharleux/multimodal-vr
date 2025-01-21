using UnityEngine;
using UnityEngine.UI;

public class VRCameraController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform xrRig;
    [SerializeField] private Transform cameraOffset;
    [SerializeField] private Camera mainCamera;

    [Header("Control Type")]
    [SerializeField] private bool useJoystick = true;
    [SerializeField] private bool useGyroscope = false;
    [SerializeField] private bool useTouchControls = false;

    [Header("Joystick Controls")]
    [SerializeField] private DynamicJoystick moveJoystick;
    [SerializeField] private DynamicJoystick rotateJoystick;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Mobile Controls")]
    [SerializeField] private float gyroSmoothing = 0.1f;
    [SerializeField] private float touchRotationSpeed = 2f;
    [SerializeField] private float touchMoveSpeed = 0.01f;

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
    [SerializeField] private string targetSceneName = "SampleScene";
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip birdSound;
    
    [Header("Cursor Settings")]
    [SerializeField] private Image cursorImage; // Reference to UI cursor image
    [SerializeField] private Color cursorNormalColor = Color.white;
    [SerializeField] private Color cursorHoverColor = Color.green;
    [SerializeField] private float cursorNormalSize = 1f;
    [SerializeField] private float cursorHoverSize = 1.2f;
    [SerializeField] private bool showCursor = true;

    // Mobile control variables
    private bool gyroInitialized = false;
    private Quaternion gyroInitialRotation;
    private Vector2 touchStart;
    private float rotationX = 0f;
    private float rotationY = 0f;

    // Object manipulation variables
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

    // Preset values
    private readonly float[] rotationPresets = { 0f, 90f, 180f, 270f };
    private readonly float[] scalePresets = { 1f, 1.5f, 2f, 0.5f };
    private int currentRotationIndex = 0;
    private int currentScaleIndex = 0;

    private void OnEnable()
    {
        InitializeControls();
    }

    private void Start()
    {
        SetupComponents();
        SetupButtons();
        InitializeControls();
        UpdateControlUI();
    }

    private void InitializeControls()
    {
        if (useGyroscope)
        {
            InitializeGyro();
        }

        // Lock screen orientation and keep screen active for mobile
        if (useGyroscope || useTouchControls)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }

    private void InitializeGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroInitialRotation = Quaternion.Euler(90f, 90f, 0f);
            gyroInitialized = true;
            Debug.Log("Gyroscope initialized");
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device");
            useGyroscope = false;
            useTouchControls = true; // Fallback to touch controls
            UpdateControlUI();
        }
    }

    private void SetupComponents()
    {
        if (xrRig == null) xrRig = transform;
        if (cameraOffset == null) cameraOffset = transform.Find("Camera Offset");
        if (mainCamera == null) mainCamera = GetComponentInChildren<Camera>();
        
        if (cursorImage != null)
        {
            cursorImage.color = cursorNormalColor;
            cursorImage.transform.localScale = Vector3.one * cursorNormalSize;
        }

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
        if (grabButton) 
        {
            grabButton.onClick.RemoveAllListeners();
            grabButton.onClick.AddListener(ToggleGrabMode);
        }
        if (rotateButton) rotateButton.onClick.AddListener(() => SetMode(ManipulationMode.Rotate));
        if (scaleButton) scaleButton.onClick.AddListener(() => SetMode(ManipulationMode.Scale));
        if (infoButton) infoButton.onClick.AddListener(ShowObjectInfo);
        
        if (transitionButton)
        {
            transitionButton.onClick.AddListener(TriggerPortalTransition);
            transitionButton.gameObject.SetActive(false);
        }
    }
    
    private void ToggleGrabMode()
    {
        Debug.Log($"ToggleGrabMode - Current mode: {currentMode}, Selected: {selectedObject}, Hovered: {hoveredObject}");

        if (currentMode == ManipulationMode.Grab && selectedObject != null)
        {
            // Deselect current object
            DeselectObject();
            currentMode = ManipulationMode.None;
            UpdateButtonVisuals();
        }
        else if (hoveredObject != null)
        {
            // Select new object
            SelectObject(hoveredObject);
            currentMode = ManipulationMode.Grab;
            UpdateButtonVisuals();
        }
    }

    private void Update()
    {
        if (useGyroscope && gyroInitialized)
        {
            HandleGyroRotation();
            HandleTouchMovement(); // Allow touch movement with gyro rotation
        }
        else if (useTouchControls)
        {
            HandleTouchControls();
        }
        else if (useJoystick)
        {
            HandleCameraMovement();
            HandleCameraRotation();
        }

        CheckHover();
        
        if (selectedObject != null)
        {
            HandleObjectManipulation();
        }
    }

    private void HandleGyroRotation()
    {
        Quaternion gyroRotation = Input.gyro.attitude;
        Quaternion rotFix = new Quaternion(gyroRotation.x, gyroRotation.y, -gyroRotation.z, -gyroRotation.w);
        Quaternion targetRotation = gyroInitialRotation * rotFix;

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetRotation,
            gyroSmoothing
        );
    }

    private void HandleTouchControls()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStart = touch.position;
                    break;

                case TouchPhase.Moved:
                    rotationY += touch.deltaPosition.x * touchRotationSpeed * Time.deltaTime;
                    rotationX -= touch.deltaPosition.y * touchRotationSpeed * Time.deltaTime;
                    rotationX = Mathf.Clamp(rotationX, -80f, 80f);

                    cameraOffset.rotation = Quaternion.Euler(rotationX, rotationY, 0);
                    break;
            }
        }
        
        HandleTouchMovement();
    }

    private void HandleTouchMovement()
    {
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                Vector2 deltaPosition = (touch0.deltaPosition + touch1.deltaPosition) / 2f;
                Vector3 moveDirection = mainCamera.transform.forward * deltaPosition.y +
                                     mainCamera.transform.right * deltaPosition.x;
                moveDirection.y = 0;
                xrRig.position += moveDirection * moveSpeed * touchMoveSpeed;
            }
        }
    }

    private void HandleCameraMovement()
    {
        if (moveJoystick == null || xrRig == null) return;

        Vector3 movement = new Vector3(moveJoystick.Horizontal, 0, moveJoystick.Vertical);
        if (movement.magnitude > 0.1f)
        {
            movement = mainCamera.transform.TransformDirection(movement);
            movement.y = 0;
            movement.Normalize();
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
            cameraOffset.Rotate(Vector3.up, rotationY, Space.World);

            Vector3 currentRotation = mainCamera.transform.localEulerAngles;
            float newXRotation = currentRotation.x - rotationX;
            
            if (newXRotation > 180f) newXRotation -= 360f;
            newXRotation = Mathf.Clamp(newXRotation, -80f, 80f);
            
            mainCamera.transform.localEulerAngles = new Vector3(newXRotation, currentRotation.y, 0);
        }
    }

    private void CheckHover()
    {
        // Only check for hover when not grabbing an object
        if (currentMode == ManipulationMode.Grab) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxSelectionDistance, selectableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (cursorImage != null && showCursor)
            {
                cursorImage.color = cursorHoverColor;
                cursorImage.transform.localScale = Vector3.one * cursorHoverSize;
            }
        
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

                // Check for portal zone
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
            if (cursorImage != null && showCursor)
            {
                cursorImage.color = cursorNormalColor;
                cursorImage.transform.localScale = Vector3.one * cursorNormalSize;
            }
            
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
            currentMode = mode;
            if (hoveredObject != null && selectedObject == null)
            {
                SelectObject(hoveredObject);
            }

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
        if (grabButton)
        {
            grabButton.GetComponent<Image>().color = 
                (currentMode == ManipulationMode.Grab && selectedObject != null) ? Color.yellow : Color.white;
        }
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

        float targetAngle = rotationPresets[currentRotationIndex];
        Vector3 currentRotation = selectedObject.transform.eulerAngles;
        float newYRotation = Mathf.LerpAngle(currentRotation.y, targetAngle, Time.deltaTime * 10f);
        selectedObject.transform.rotation = Quaternion.Euler(0, newYRotation, 0);

        if (Mathf.Abs(newYRotation - targetAngle) < 0.1f)
        {
            selectedObject.transform.rotation = Quaternion.Euler(0, targetAngle, 0);
        }
    }

    private void HandleScale()
    {
        if (selectedObject == null) return;

        float targetScale = scalePresets[currentScaleIndex];
        float currentScale = selectedObject.transform.localScale.x;
        float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * 5f);
        selectedObject.transform.localScale = Vector3.one * newScale;

        if (Mathf.Abs(newScale - targetScale) < 0.01f)
        {
            selectedObject.transform.localScale = Vector3.one * targetScale;
        }
    }

    private void ShowObjectInfo()
    {
        GameObject targetObject = selectedObject != null ? selectedObject : hoveredObject;
    
        if (targetObject != null)
        {
            ObjectInfo objectInfo = targetObject.GetComponent<ObjectInfo>();
            if (objectInfo != null && objectInfo.details != null)
            {
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
        if (obj == null) return;
    
        Debug.Log($"Selecting object: {obj.name}");
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
        Debug.Log("Deselecting object");
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
    
    public void ToggleCursor()
    {
        showCursor = !showCursor;
        if (cursorImage != null)
        {
            cursorImage.gameObject.SetActive(showCursor);
        }
    }
    
    public void SetCursorVisible(bool visible)
    {
        showCursor = visible;
        if (cursorImage != null)
        {
            cursorImage.gameObject.SetActive(visible);
        }
    }

    // Control mode methods
    public void SetControlMode(string mode)
    {
        switch (mode.ToLower())
        {
            case "joystick":
                useJoystick = true;
                useGyroscope = false;
                useTouchControls = false;
                break;
            case "gyroscope":
                useJoystick = false;
                useGyroscope = true;
                useTouchControls = false;
                InitializeGyro();
                break;
            case "touch":
                useJoystick = false;
                useGyroscope = false;
                useTouchControls = true;
                break;
        }

        UpdateControlUI();
    }

    private void UpdateControlUI()
    {
        // Update UI visibility based on control mode
        if (moveJoystick != null)
        {
            moveJoystick.gameObject.SetActive(useJoystick);
        }
        if (rotateJoystick != null)
        {
            rotateJoystick.gameObject.SetActive(useJoystick);
        }
    }

    public void ToggleGyroscope()
    {
        if (!useGyroscope)
        {
            SetControlMode("gyroscope");
        }
        else
        {
            SetControlMode("touch");
        }
    }

    private void OnDisable()
    {
        // Clean up
        if (Input.gyro != null && Input.gyro.enabled)
        {
            Input.gyro.enabled = false;
        }
        
        UnhoverCurrentObject();
        DeselectObject();
    }
}