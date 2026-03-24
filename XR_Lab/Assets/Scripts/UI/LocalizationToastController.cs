using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LocalizationToastController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image accentStripe;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text messageText;

    [Header("State Colors")]
    [SerializeField] private Color inProgressColor = new Color(1f, 0.75f, 0f, 1f);
    [SerializeField] private Color successColor    = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color failureColor    = new Color(0.9f, 0.2f, 0.2f, 1f);

    [Header("State Icons")]
    [SerializeField] private Sprite inProgressIcon;
    [SerializeField] private Sprite successIcon;
    [SerializeField] private Sprite failureIcon;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float holdDuration = 2.5f;

    private Coroutine _activeRoutine;

    //Wire to MapLocalizationManager's UnityEvents in the Inspector

    public void OnLocalizationStarted()
    {
        Show("Localising...", inProgressColor, inProgressIcon, autoHide: false);
    }

    public void OnLocalizationSuccess()
    {
        Show("Localisation Successful", successColor, successIcon, autoHide: true);
    }

    public void OnLocalizationFailure()
    {
        Show("Localisation Failed", failureColor, failureIcon, autoHide: true);
    }

    //internal

    private void Show(string message, Color color, Sprite sprite, bool autoHide)
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(ShowRoutine(message, color, sprite, autoHide));
    }

    private IEnumerator ShowRoutine(string message, Color color, Sprite sprite, bool autoHide)
    {
        messageText.text = message;
        accentStripe.color = color;
        if (icon != null && sprite != null) icon.sprite = sprite;

        yield return Fade(0f, 1f);

        if (!autoHide) yield break;

        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f);

        _activeRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}