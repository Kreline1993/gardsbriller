using UnityEngine;
using TMPro;

public class InfoPanelBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text speciesText;
    [SerializeField] private TMP_Text idText;

    public void Populate(Plant data)
    {
        if (data == null) return;

        if (speciesText != null) speciesText.text = data.species;
        if (idText != null)      idText.text = data.plantId;
    }
}
