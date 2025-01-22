using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SceneTriggerZone : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Material portalMaterial;
    [SerializeField] private float glowIntensity = 1.5f;
    [SerializeField] private Color baseColor = new Color(0, 0.7f, 1f, 0.5f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    [Header("Trigger Settings")]
    [SerializeField] private string targetSceneName = "SampleScene";
    [SerializeField] private float transitionDelay = 0.5f;
    [SerializeField] private bool persistPosition = true;
    
    private Material instanceMaterial;
    private bool isTransitioning = false;
    private MeshRenderer meshRenderer;
    private static Vector3 lastPosition;
    private static Quaternion lastRotation;
    private static string lastScene;
    private static bool hasStoredPosition;

    private void Start()
    {
        SetupVisuals();
        TryRestorePosition();
    }

    private void SetupVisuals()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && portalMaterial != null)
        {
            instanceMaterial = new Material(portalMaterial);
            meshRenderer.material = instanceMaterial;
            
            instanceMaterial.SetColor("_EmissionColor", baseColor * glowIntensity);
            instanceMaterial.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f));
        }
    }

    private void TryRestorePosition()
    {
        if (!persistPosition || !hasStoredPosition) return;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == lastScene)
        {
            var xrRig = FindObjectOfType<VRCameraController>()?.transform;
            if (xrRig != null)
            {
                xrRig.position = lastPosition;
                xrRig.rotation = lastRotation;
                Debug.Log($"Restored position: {lastPosition}, rotation: {lastRotation} in scene: {currentScene}");
            }
            hasStoredPosition = false; 
        }
    }

    private void Update()
    {
        UpdatePortalVisuals();
    }

    private void UpdatePortalVisuals()
    {
        if (meshRenderer != null && instanceMaterial != null)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * pulseAmount;
            Color emissionColor = baseColor * (glowIntensity + pulse);
            instanceMaterial.SetColor("_EmissionColor", emissionColor);
        }
    }

    public void TryTriggerTransition(string targetSceneName)
    {
        if (!isTransitioning)
        {
            if (persistPosition)
            {
                StoreCurrentPosition();
            }
            
            this.targetSceneName = targetSceneName;
            StartCoroutine(HandleSceneTransition());
        }
    }

    private void StoreCurrentPosition()
    {
        var xrRig = FindObjectOfType<VRCameraController>()?.transform;
        if (xrRig != null)
        {
            lastPosition = xrRig.position;
            lastRotation = xrRig.rotation;
            lastScene = SceneManager.GetActiveScene().name;
            hasStoredPosition = true;
            Debug.Log($"Stored position: {lastPosition}, rotation: {lastRotation} from scene: {lastScene}");
        }
    }

    private System.Collections.IEnumerator HandleSceneTransition()
    {
        isTransitioning = true;
        
        // Optional: Add visual feedback here
        glowIntensity *= 2f;
        
        // Wait for delay
        yield return new WaitForSeconds(transitionDelay);

        AsyncOperation asyncLoad = null;
        
        try
        {
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene '{targetSceneName}': {e.Message}");
            isTransitioning = false;
            yield break;
        }

        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }

    private void OnDestroy()
    {
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
    }
}