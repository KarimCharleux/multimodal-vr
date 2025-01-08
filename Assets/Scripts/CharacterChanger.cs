using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterChanger : MonoBehaviour
{
    [SerializeField] private Transform scrollContent;    
    [SerializeField] private Button applyButton;        
    [SerializeField] private GameObject buttonPrefab;   
    [SerializeField] private Texture2D[] textures; 
    
    private SkinnedMeshRenderer characterRenderer;
    private Texture2D currentTexture;
    private Texture2D selectedTexture;

    void Start()
    {
        SetupUILayout();
        // Get the character's SkinnedMeshRenderer
        characterRenderer = GameObject.Find("SimplePeople").GetComponentInChildren<SkinnedMeshRenderer>();

        if (textures != null && textures.Length > 0)
        {
            // Create a button for each texture
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                
                // Create button
                GameObject buttonObj = Instantiate(buttonPrefab, scrollContent);
                Button button = buttonObj.GetComponent<Button>();
                
                // Set button text
                TMPro.TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = texture.name;
                }

                // Add click listener
                int index = i;
                button.onClick.AddListener(() => PreviewTexture(index));
            }

            // Add listener to apply button
            applyButton.onClick.AddListener(ApplyTexture);

            // Set initial texture
            currentTexture = textures[0];
            characterRenderer.material.mainTexture = currentTexture;
        }
        else
        {
            Debug.LogError("No textures assigned to the CharacterChanger script!");
        }
    }
    
    void SetupUILayout()
    {
        
        // Add Vertical Layout Group to content if not exists
        if (!scrollContent.GetComponent<VerticalLayoutGroup>())
        {
            VerticalLayoutGroup vlg = scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(0, 0, 10, 10);
        }
        
    }

    void PreviewTexture(int index)
    {
        selectedTexture = textures[index];
        characterRenderer.material.mainTexture = selectedTexture;
    }

    void ApplyTexture()
    {
        if (selectedTexture != null)
        {
            currentTexture = selectedTexture;
            Debug.Log($"Applied texture: {currentTexture.name}");
        }
    }
}
