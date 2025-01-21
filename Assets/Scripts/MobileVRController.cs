using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileVRController : MonoBehaviour
{
    [Header("VR Settings")]
    [SerializeField] private bool enableGyro = true;
    [SerializeField] private float gyroSmoothing = 0.1f;
    
    [Header("Movement Controls")]
    [SerializeField] private Button forwardButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private float moveSpeed = 5f;

    [Header("Object Manipulation")]
    [SerializeField] private Button grabButton;
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button scaleButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private float objectRotationSpeed = 100f;
    [SerializeField] private float objectScaleSpeed = 0.5f;
    [SerializeField] private float objectHoldDistance = 2f;
    
    [Header("Selection Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private float maxSelectionDistance = 10f;
    [SerializeField] private Material selectedObjectMaterial;
    [SerializeField] private Material hoveredObjectMaterial;

    private enum ManipulationMode { None, Grab, Rotate, Scale }
    private ManipulationMode currentMode = ManipulationMode.None;
    
    // Movement flags
    private bool isMovingForward;
    private bool isMovingBack;
    private bool isMovingLeft;
    private bool isMovingRight;
    
    private Camera mainCamera;
    private Transform xrOrigin;
    private GameObject selectedObject;
    private GameObject hoveredObject;
    private Material originalMaterial;
    private Material originalHoverMaterial;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector2 lastTouchPosition;
    
    // Gyroscope variables
    private Quaternion gyroInitialRotation;
    private bool gyroInitialized = false;

    private void Start()
    {
        InitializeComponents();
        SetupUI();
        InitializeGyroscope();
    }

    private void InitializeComponents()
    {
        mainCamera = Camera.main;
        xrOrigin = transform;
        
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

        if (selectedObjectMaterial == null)
        {
            selectedObjectMaterial = new Material(Shader.Find("Standard"));
            selectedObjectMaterial.color = new Color(1f, 0.6f, 0f, 0.8f);
        }

        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void SetupUI()
    {
        // Setup movement buttons
        if (forwardButton)
        {
            EventTrigger forwardTrigger = forwardButton.gameObject.AddComponent<EventTrigger>();
            
            EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => { isMovingForward = true; });
            forwardTrigger.triggers.Add(pointerDown);
            
            EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => { isMovingForward = false; });
            forwardTrigger.triggers.Add(pointerUp);
        }
        
        if (backButton)
        {
            EventTrigger backTrigger = backButton.gameObject.AddComponent<EventTrigger>();
            
            EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => { isMovingBack = true; });
            backTrigger.triggers.Add(pointerDown);
            
            EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => { isMovingBack = false; });
            backTrigger.triggers.Add(pointerUp);
        }
        
        if (leftButton)
        {
            EventTrigger leftTrigger = leftButton.gameObject.AddComponent<EventTrigger>();
            
            EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => { isMovingLeft = true; });
            leftTrigger.triggers.Add(pointerDown);
            
            EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => { isMovingLeft = false; });
            leftTrigger.triggers.Add(pointerUp);
        }
        
        if (rightButton)
        {
            EventTrigger rightTrigger = rightButton.gameObject.AddComponent<EventTrigger>();
            
            EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => { isMovingRight = true; });
            rightTrigger.triggers.Add(pointerDown);
            
            EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => { isMovingRight = false; });
            rightTrigger.triggers.Add(pointerUp);
        }

        // Setup manipulation buttons
        if (grabButton) grabButton.onClick.AddListener(() => SetManipulationMode(ManipulationMode.Grab));
        if (rotateButton) rotateButton.onClick.AddListener(() => SetManipulationMode(ManipulationMode.Rotate));
        if (scaleButton) scaleButton.onClick.AddListener(() => SetManipulationMode(ManipulationMode.Scale));
        if (infoButton) infoButton.onClick.AddListener(ShowObjectInfo);
    }

    private void InitializeGyroscope()
    {
        if (enableGyro && SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroInitialRotation = Quaternion.Euler(90f, 90f, 0f);
            gyroInitialized = true;
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported or disabled");
        }
    }

    private void Update()
    {
        if (gyroInitialized)
        {
            UpdateGyroscope();
        }

        UpdateMovement();
        HandleObjectManipulation();
        
        if (currentMode == ManipulationMode.None)
        {
            CheckHover();
        }
    }

    private void UpdateGyroscope()
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

    private void UpdateMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        // Get camera forward and right, but remove vertical component
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement based on active buttons
        if (isMovingForward) moveDirection += forward;
        if (isMovingBack) moveDirection -= forward;
        if (isMovingRight) moveDirection += right;
        if (isMovingLeft) moveDirection -= right;

        // Apply movement if there is any
        if (moveDirection != Vector3.zero)
        {
            moveDirection.Normalize();
            xrOrigin.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleObjectManipulation()
    {
        if (!selectedObject || currentMode == ManipulationMode.None) return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                continue;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    lastTouchPosition = touch.position;
                    break;

                case TouchPhase.Moved:
                    Vector2 delta = touch.position - lastTouchPosition;
                    
                    switch (currentMode)
                    {
                        case ManipulationMode.Grab:
                            UpdateObjectPosition(delta);
                            break;
                            
                        case ManipulationMode.Rotate:
                            UpdateObjectRotation(delta);
                            break;
                            
                        case ManipulationMode.Scale:
                            UpdateObjectScale(delta);
                            break;
                    }
                    
                    lastTouchPosition = touch.position;
                    break;
            }
        }
    }

    private void UpdateObjectPosition(Vector2 delta)
    {
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        Vector3 up = mainCamera.transform.up;

        Vector3 movement = (right * delta.x + up * delta.y) * Time.deltaTime * 0.01f;
        selectedObject.transform.position += movement;
    }

    private void UpdateObjectRotation(Vector2 delta)
    {
        float rotationX = delta.y * objectRotationSpeed * Time.deltaTime;
        float rotationY = delta.x * objectRotationSpeed * Time.deltaTime;
        
        selectedObject.transform.Rotate(Vector3.right, rotationX, Space.World);
        selectedObject.transform.Rotate(Vector3.up, rotationY, Space.World);
    }

    private void UpdateObjectScale(Vector2 delta)
    {
        float scaleFactor = 1f + (delta.y * objectScaleSpeed * Time.deltaTime);
        selectedObject.transform.localScale *= scaleFactor;
    }

    private void ShowObjectInfo()
    {
        if (hoveredObject != null)
        {
            ObjectInfo info = hoveredObject.GetComponent<ObjectInfo>();
            if (info != null)
            {
                InfoPopupManager.Instance.ShowPopup(info.details);
            }
        }
    }

    private void SetManipulationMode(ManipulationMode mode)
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
            
            // If we have a hovered object, select it
            if (hoveredObject != null && selectedObject == null)
            {
                SelectObject(hoveredObject);
            }
        }

        // Update button visuals
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (grabButton) grabButton.GetComponent<Image>().color = currentMode == ManipulationMode.Grab ? Color.yellow : Color.white;
        if (rotateButton) rotateButton.GetComponent<Image>().color = currentMode == ManipulationMode.Rotate ? Color.yellow : Color.white;
        if (scaleButton) scaleButton.GetComponent<Image>().color = currentMode == ManipulationMode.Scale ? Color.yellow : Color.white;
    }

    private void CheckHover()
    {
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

    private void SelectObject(GameObject obj)
    {
        UnhoverCurrentObject();
        
        selectedObject = obj;
        var renderer = selectedObject.GetComponent<Renderer>();
        if (renderer != null && selectedObjectMaterial != null)
        {
            originalMaterial = renderer.material;
            renderer.material = selectedObjectMaterial;
        }
        
        originalScale = selectedObject.transform.localScale;
        originalRotation = selectedObject.transform.rotation;
    }

    private void DeselectObject()
    {
        if (selectedObject != null)
        {
            var renderer = selectedObject.GetComponent<Renderer>();
            if (renderer != null && originalMaterial != null)
            {
                renderer.material = originalMaterial;
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

    private void OnGUI()
    {
        if (!isMovingForward)
        {
            // Draw crosshair
            float size = 20f;
            float centerX = Screen.width / 2;
            float centerY = Screen.height / 2;
            
            GUI.color = hoveredObject != null ? Color.yellow : Color.white;
            GUI.DrawTexture(
                new Rect(centerX - size/2, centerY - 1, size, 2), 
                Texture2D.whiteTexture
            );
            GUI.DrawTexture(
                new Rect(centerX - 1, centerY - size/2, 2, size), 
                Texture2D.whiteTexture
            );
        }
    }

    private void OnDisable()
    {
        UnhoverCurrentObject();
        DeselectObject();
        if (Input.gyro != null)
        {
            Input.gyro.enabled = false;
        }
    }
}