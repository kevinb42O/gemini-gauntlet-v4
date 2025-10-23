using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PowerupSlot
{
    [Header("UI References")]
    public GameObject slotObject;
    public Image backgroundImage;
    public Image iconImage;
    public TextMeshProUGUI displayText;
    public GameObject selectionBorder;
    
    [Header("Slot Settings")]
    public bool isEmpty = true;
    public PowerUpType currentPowerupType;
    
    public void SetPowerup(PowerUpType powerupType, Sprite icon, string text)
    {
        isEmpty = false;
        currentPowerupType = powerupType;
        
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }
        
        if (displayText != null)
        {
            displayText.text = text;
        }
        
        if (slotObject != null)
        {
            slotObject.SetActive(true);
        }
    }
    
    public void ClearSlot()
    {
        isEmpty = true;
        currentPowerupType = PowerUpType.None;
        
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.gameObject.SetActive(false);
        }
        
        if (displayText != null)
        {
            displayText.text = "";
        }
    }
    
    public void SetSelected(bool selected, Color normalColor, Color selectedColor, Vector3 normalScale, Vector3 selectedScale)
    {
        if (selectionBorder != null)
        {
            selectionBorder.SetActive(selected);
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }
        
        if (slotObject != null)
        {
            slotObject.transform.localScale = selected ? selectedScale : normalScale;
        }
    }
    
    public void UpdateText(string newText)
    {
        if (displayText != null)
        {
            displayText.text = newText;
        }
    }
}
