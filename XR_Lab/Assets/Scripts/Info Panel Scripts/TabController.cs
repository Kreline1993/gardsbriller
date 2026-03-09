using UnityEngine;

public class TabController : MonoBehaviour
{
    public GameObject generalTab;
    public GameObject healthTab;
    public GameObject qualityTab;

    void Start()
    {
        ShowGeneral(); // default tab on open
    }

    public void ShowGeneral()
    {
        generalTab.SetActive(true);
        healthTab.SetActive(false);
        qualityTab.SetActive(false);
    }

    public void ShowHealth()
    {
        generalTab.SetActive(false);
        healthTab.SetActive(true);
        qualityTab.SetActive(false);
    }

    public void ShowQuality()
    {
        generalTab.SetActive(false);
        healthTab.SetActive(false);
        qualityTab.SetActive(true);
    }
}