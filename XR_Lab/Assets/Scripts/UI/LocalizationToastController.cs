using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LocalizationToastController : MonoBehaviour
{
    private static LocalizationToastController _runtimeInstance;

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

    [Header("State Text")]
    [SerializeField] private string inProgressText = "Localizing...";
    [SerializeField] private string successText = "Localization successful";
    [SerializeField] private string failureText = "Localization failed";

    private Coroutine _activeRoutine;

    private void Awake()
    {
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
        {
            _runtimeInstance = this;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
        {
            _runtimeInstance = this;
        }
    }

    private void OnDestroy()
    {
        if (_runtimeInstance == this)
        {
            _runtimeInstance = null;
        }
    }

    // Wire to MapLocalizationManager's UnityEvents in the Inspector

    public void OnLocalizationStarted()
    {
        var runtimeController = ResolveRuntimeController();
        if (runtimeController != this)
        {
            runtimeController.OnLocalizationStarted();
            return;
        }

        Show(inProgressText, inProgressColor, inProgressIcon, autoHide: false);
    }

    public void OnLocalizationSuccess()
    {
        var runtimeController = ResolveRuntimeController();
        if (runtimeController != this)
        {
            runtimeController.OnLocalizationSuccess();
            return;
        }

        Show(successText, successColor, successIcon, autoHide: true);
    }

    public void OnLocalizationFailure()
    {
        var runtimeController = ResolveRuntimeController();
        if (runtimeController != this)
        {
            runtimeController.OnLocalizationFailure();
            return;
        }

        Show(failureText, failureColor, failureIcon, autoHide: true);
    }

    // Internal

    private void Show(string message, Color color, Sprite sprite, bool autoHide)
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        _activeRoutine = StartCoroutine(ShowRoutine(message, color, sprite, autoHide));
    }

    private LocalizationToastController ResolveRuntimeController()
    {
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
        {
            return this;
        }

        if (_runtimeInstance != null)
        {
            return _runtimeInstance;
        }

        var found = FindFirstObjectByType<LocalizationToastController>();
        return found != null ? found : this;
    }

    private IEnumerator ShowRoutine(string message, Color color, Sprite sprite, bool autoHide)
    {
        if (messageText != null) messageText.text = message;
        if (accentStripe != null) accentStripe.color = color;
        if (icon != null && sprite != null) icon.sprite = sprite;

        float fromAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        if (fromAlpha < 1f)
        {
            yield return Fade(fromAlpha, 1f);
        }

        if (!autoHide)
        {
            _activeRoutine = null;
            yield break;
        }

        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f);

        _activeRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

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