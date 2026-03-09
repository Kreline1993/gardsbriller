using UnityEngine;

public class AutoManualShift : MonoBehaviour
{
    [Header("Settings")]
    public GameObject extraDetails; // The text/panel that appears
    public float shiftAmount = 100f; // Pixels to move everything down

    private bool isOpen = false;

    public void ToggleExpansion()
    {
        isOpen = !isOpen;

        // 1. Show/Hide the extra info
        if (extraDetails != null)
            extraDetails.SetActive(isOpen);

        // 2. Determine shift (Negative Y is DOWN in Unity UI)
        float moveDistance = isOpen ? -shiftAmount : shiftAmount;

        // 3. Find the parent (the "Content" object)
        Transform parentFolder = transform.parent;

        // 4. Get this button's position in the list (index)
        int myIndex = transform.GetSiblingIndex();

        // 5. Loop through only the objects AFTER this one
        for (int i = myIndex + 1; i < parentFolder.childCount; i++)
        {
            RectTransform sibling = parentFolder.GetChild(i).GetComponent<RectTransform>();

            if (sibling != null)
            {
                // Shift the position
                Vector2 pos = sibling.anchoredPosition;
                pos.y += moveDistance;
                sibling.anchoredPosition = pos;
            }
        }
    }
}
