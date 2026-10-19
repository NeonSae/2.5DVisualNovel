
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemName;

    public void Setup(InventoryItem item)
    {
        if (item == null)
            return;

        itemName.text = item.itemName;

        if (item.icon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = true;
        }
        else
        {
            itemIcon.enabled = false;
        }
    }   
}
