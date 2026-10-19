
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform itemGrid;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Inventory Reference")]
    [SerializeField] private Inventory inventory;

    private readonly List<GameObject> spawnedSlots = new List<GameObject>();

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inventoryPanel != null)
            {
                bool shouldOpen = !inventoryPanel.activeSelf;
                inventoryPanel.SetActive(shouldOpen);

                if (shouldOpen)
                    RefreshInventory();
            }
        }

        if (inventoryPanel != null &&
            inventoryPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            inventoryPanel.SetActive(false);
        }
    }

    public void RefreshInventory()
    {
        if (inventory == null || itemGrid == null || slotPrefab == null)
        {
            Debug.LogWarning("InventoryUI is missing a reference.");
            return;
        }

        foreach (GameObject slot in spawnedSlots)
            Destroy(slot);

        spawnedSlots.Clear();

        foreach (InventoryItem item in inventory.Items)
        {
            InventorySlotUI newSlot = Instantiate(slotPrefab, itemGrid);
            newSlot.Setup(item);
            spawnedSlots.Add(newSlot.gameObject);
        }
    }
}
