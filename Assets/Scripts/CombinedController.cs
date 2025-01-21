using UnityEngine;

public class CombinedController : MonoBehaviour
{
    [Header("Joystick Settings")]
    [SerializeField] private DynamicJoystick moveJoystick;
    [SerializeField] private DynamicJoystick rotateJoystick;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Selection Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private float maxSelectionDistance = 10f;
    [SerializeField] private Material selectedObjectMaterial;
    [SerializeField] private Material hoveredObjectMaterial;

    private Camera mainCamera;
    private CharacterController characterController;
    private GameObject selectedObject;
    private GameObject hoveredObject;
    private Material originalMaterial;
    private Material originalHoverMaterial;
    private bool isHoldingObject = false;

    private void Start()
    {
        // Get required components
        mainCamera = Camera.main;
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        // Setup materials if not assigned
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
    }

    private void Update()
    {
        HandleMovement();
        
        if (!isHoldingObject)
        {
            CheckHover();
            HandleSelection();
        }
        else
        {
            HandleObjectManipulation();
        }
    }

    private void HandleMovement()
    {
        if (moveJoystick == null) return;

        // Get input from joystick
        Vector2 movement = new Vector2(moveJoystick.Horizontal, moveJoystick.Vertical);

        if (movement != Vector2.zero)
        {
            // Convert input to world space movement
            Vector3 moveDirection = new Vector3(movement.x, 0, movement.y);
            moveDirection = mainCamera.transform.TransformDirection(moveDirection);
            moveDirection.y = 0; // Keep movement on ground plane
            moveDirection.Normalize();

            // Apply movement
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

            // Rotate character to face movement direction
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(moveDirection),
                rotationSpeed * Time.deltaTime
            );
        }
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

    private void HandleSelection()
    {
        // Check for touch/click input
        if (Input.GetMouseButtonDown(0) && hoveredObject != null)
        {
            SelectObject(hoveredObject);
            isHoldingObject = true;
        }
    }

    private void HandleObjectManipulation()
    {
        if (rotateJoystick == null || selectedObject == null) return;

        // Get rotation input from second joystick
        Vector2 rotation = new Vector2(rotateJoystick.Horizontal, rotateJoystick.Vertical);

        if (rotation != Vector2.zero)
        {
            // Apply rotation
            selectedObject.transform.Rotate(
                Vector3.up, rotation.x * rotationSpeed * Time.deltaTime,
                Space.World
            );
            selectedObject.transform.Rotate(
                Vector3.right, -rotation.y * rotationSpeed * Time.deltaTime,
                Space.World
            );
        }

        // Release object on second click
        if (Input.GetMouseButtonDown(0))
        {
            DeselectObject();
            isHoldingObject = false;
        }
    }

    private void SelectObject(GameObject obj)
    {
        selectedObject = obj;
        var renderer = selectedObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterial = renderer.material;
            renderer.material = selectedObjectMaterial;
        }
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

    private void OnDisable()
    {
        UnhoverCurrentObject();
        DeselectObject();
    }
}