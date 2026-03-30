using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [Header("Tab Content")]
    public GameObject generalTab;
    public GameObject healthTab;
    public GameObject qualityTab;

    [Header("Tab Buttons")]
    public Button generalButton;
    public Button healthButton;
    public Button qualityButton;

    [Header("Button Colors")]
    public Color activeColor   = new Color(0.36f, 0.965f, 1f, 1f);
    public Color inactiveColor = new Color(1f,    1f,     1f, 1f);

    void Start()
    {
        ShowGeneral();
    }

    public void ShowGeneral()
    {
        SetTabs(true, false, false);
        SetActiveButton(generalButton);
    }

    public void ShowHealth()
    {
        SetTabs(false, true, false);
        SetActiveButton(healthButton);
    }

    public void ShowQuality()
    {
        SetTabs(false, false, true);
        SetActiveButton(qualityButton);
    }

    private void SetTabs(bool general, bool health, bool quality)
    {
        if (generalTab) generalTab.SetActive(general);
        if (healthTab)  healthTab.SetActive(health);
        if (qualityTab) qualityTab.SetActive(quality);
    }

    private void SetActiveButton(Button active)
    {
        SetButtonColor(generalButton, generalButton == active ? activeColor : inactiveColor);
        SetButtonColor(healthButton,  healthButton  == active ? activeColor : inactiveColor);
        SetButtonColor(qualityButton, qualityButton == active ? activeColor : inactiveColor);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.targetGraphic as Image;
        if (img != null) img.color = color;
    }
}