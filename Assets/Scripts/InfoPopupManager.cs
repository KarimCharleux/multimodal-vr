using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoPopupManager : MonoBehaviour
{
    public static InfoPopupManager Instance { get; private set; }

    [Header("Popup References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private Button closeButton;
    
    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Setup close button
        closeButton.onClick.AddListener(HidePopup);
        
        // Ensure popup starts hidden
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    public void ShowPopup(ObjectInfo.ObjectDetails details)
    {
        // Update popup content
        titleText.text = details.objectName;
        descriptionText.text = details.description;
        categoryText.text = $"Category: {details.category}";
        dateText.text = $"Created: \n {details.creationDate}";

        // Show popup
        popupPanel.SetActive(true);
        StartCoroutine(FadeIn());
    }

    public void HidePopup()
    {
        popupPanel.SetActive(false);
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = elapsedTime / fadeInDuration;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}