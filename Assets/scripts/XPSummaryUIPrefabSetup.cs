using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GeminiGauntlet.UI
{
    /// <summary>
    /// Helper script to easily set up XP Summary UI prefab in the scene.
    /// Attach this to a Canvas and it will create the UI structure automatically.
    /// </summary>
    public class XPSummaryUIPrefabSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool createExampleLine = true;
        
        [Header("Styling")]
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color accentColor = Color.yellow;
        [SerializeField] private int fontSize = 24;
        [SerializeField] private int titleFontSize = 32;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupXPSummaryUI();
            }
        }
        
        [ContextMenu("Setup XP Summary UI")]
        public void SetupXPSummaryUI()
        {
            // Get or create canvas
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("XPSummaryUIPrefabSetup: Must be attached to a Canvas!");
                return;
            }
            
            // Create main panel
            GameObject summaryPanel = CreatePanel("XP_SummaryPanel", canvas.transform);
            
            // Create background
            Image panelImage = summaryPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = backgroundColor;
            }
            
            // Create vertical layout group
            VerticalLayoutGroup layoutGroup = summaryPanel.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(40, 40, 40, 40);
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            
            // Create title text
            GameObject titleText = CreateText("XP_Title", summaryPanel.transform, "XP SUMMARY", titleFontSize);
            titleText.GetComponent<TextMeshProUGUI>().color = accentColor;
            titleText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            // Create category container
            GameObject categoryContainer = CreatePanel("XP_CategoryContainer", summaryPanel.transform);
            categoryContainer.GetComponent<Image>().color = Color.clear; // Transparent
            
            VerticalLayoutGroup categoryLayout = categoryContainer.AddComponent<VerticalLayoutGroup>();
            categoryLayout.spacing = 5f;
            categoryLayout.childControlHeight = false;
            categoryLayout.childControlWidth = false;
            categoryLayout.childForceExpandHeight = false;
            categoryLayout.childForceExpandWidth = true;
            
            // Create divider line
            GameObject dividerLine = CreatePanel("XP_DividerLine", summaryPanel.transform);
            dividerLine.GetComponent<Image>().color = accentColor;
            RectTransform dividerRect = dividerLine.GetComponent<RectTransform>();
            dividerRect.sizeDelta = new Vector2(0, 2); // Thin horizontal line
            
            // Create total XP text
            GameObject totalXPText = CreateText("XP_TotalText", summaryPanel.transform, "Total XP: 0xp", titleFontSize);
            totalXPText.GetComponent<TextMeshProUGUI>().color = accentColor;
            totalXPText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            // Create category line prefab
            GameObject categoryLinePrefab = CreateCategoryLinePrefab(summaryPanel.transform);
            
            // Set up XPSummaryUI component
            XPSummaryUI xpSummaryUI = summaryPanel.GetComponent<XPSummaryUI>();
            if (xpSummaryUI == null)
            {
                xpSummaryUI = summaryPanel.AddComponent<XPSummaryUI>();
            }
            
            // Assign references using reflection to access private fields
            var summaryPanelField = typeof(XPSummaryUI).GetField("summaryPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryContainerField = typeof(XPSummaryUI).GetField("categoryContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryLinePrefabField = typeof(XPSummaryUI).GetField("categoryLinePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var totalXPTextField = typeof(XPSummaryUI).GetField("totalXPText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dividerLineField = typeof(XPSummaryUI).GetField("dividerLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            summaryPanelField?.SetValue(xpSummaryUI, summaryPanel);
            categoryContainerField?.SetValue(xpSummaryUI, categoryContainer.transform);
            categoryLinePrefabField?.SetValue(xpSummaryUI, categoryLinePrefab);
            totalXPTextField?.SetValue(xpSummaryUI, totalXPText.GetComponent<TextMeshProUGUI>());
            dividerLineField?.SetValue(xpSummaryUI, dividerLine);
            
            Debug.Log("XPSummaryUIPrefabSetup: XP Summary UI created successfully!");
            
            // Create example line if requested
            if (createExampleLine)
            {
                CreateExampleLine(categoryContainer.transform, categoryLinePrefab);
            }
        }
        
        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            return panel;
        }
        
        private GameObject CreateText(string name, Transform parent, string text, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 50);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = textColor;
            textComponent.alignment = TextAlignmentOptions.Left;
            
            return textObj;
        }
        
        private GameObject CreateCategoryLinePrefab(Transform parent)
        {
            GameObject linePrefab = new GameObject("XP_CategoryLinePrefab");
            linePrefab.transform.SetParent(parent, false);
            
            RectTransform rect = linePrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 30);
            
            TextMeshProUGUI text = linePrefab.AddComponent<TextMeshProUGUI>();
            text.text = "Category: 0 x 0xp = 0xp";
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Left;
            
            // Hide the prefab initially
            linePrefab.SetActive(false);
            
            return linePrefab;
        }
        
        private void CreateExampleLine(Transform categoryContainer, GameObject linePrefab)
        {
            GameObject exampleLine = Instantiate(linePrefab, categoryContainer);
            exampleLine.name = "ExampleLine (Delete Me)";
            exampleLine.SetActive(true);
            
            TextMeshProUGUI text = exampleLine.GetComponent<TextMeshProUGUI>();
            text.text = "Enemies Killed: 10 x 15xp = 150xp";
            text.color = Color.gray;
        }
    }
}