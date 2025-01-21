using UnityEngine;

public class PositionManager : MonoBehaviour
{
    private static PositionManager instance;
    public static PositionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("PositionManager");
                instance = go.AddComponent<PositionManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private string previousSceneName;
    private bool hasStoredPosition = false;

    public void StorePosition(Vector3 position, Quaternion rotation, string sceneName)
    {
        lastPosition = position;
        lastRotation = rotation;
        previousSceneName = sceneName;
        hasStoredPosition = true;
        
        Debug.Log($"Stored position: {lastPosition}, rotation: {lastRotation}, scene: {previousSceneName}");
    }

    public bool TryGetStoredPosition(string fromScene, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (hasStoredPosition && fromScene == previousSceneName)
        {
            position = lastPosition;
            rotation = lastRotation;
            return true;
        }

        return false;
    }

    public void ClearStoredPosition()
    {
        hasStoredPosition = false;
        previousSceneName = null;
    }
}