using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ModeIndicator : MonoBehaviour
{
    //References

    [Header("References")]
    [Tooltip("Auto-found in scene if left empty.")]
    [SerializeField] private ModeController modeController;

    //Mode Colors

    [Header("Mode Colors")]
    [SerializeField] private Color defaultColor   = Color.white;
    [SerializeField] private Color overviewColor  = new Color(0.55f, 0.0f, 1.00f, 1f);
    [SerializeField] private Color pickingColor   = new Color(0.10f, 0.85f, 0.10f, 1f);
    [SerializeField] private Color weedingColor   = new Color(1.00f, 0.85f, 0.00f, 1f);

    //Color Transition

    [Header("Transition")]
    [SerializeField] [Range(1f, 20f)] private float colorLerpSpeed = 8f;

    //Pulse Animation

    [Header("Pulse")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] [Range(0.5f, 6f)] private float pulseSpeed = 2f;
    [SerializeField] [Range(0f, 1f)] private float pulseMinAlpha = 0.55f;
    [SerializeField] [Range(0f, 1f)] private float pulseMaxAlpha = 1.0f;

    //Private State

    private Image _dot;
    private Color _targetColor;

    private void Awake()
    {
        _dot = GetComponent<Image>();
    }

    private void Start()
    {
        if (modeController == null)
            modeController = FindFirstObjectByType<ModeController>();

        if (modeController == null)
        {
            Debug.LogError("[ModeIndicator] No ModeController found in scene. Indicator disabled.", this);
            gameObject.SetActive(false);
            return;
        }

        modeController.ModeChanged += OnModeChanged;
        ApplyMode(modeController.CurrentMode, instant: true);
    }

    private void OnDestroy()
    {
        if (modeController != null)
            modeController.ModeChanged -= OnModeChanged;
    }

    private void Update()
    {
        Color blended = Color.Lerp(_dot.color, _targetColor, Time.deltaTime * colorLerpSpeed);

        if (enablePulse)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            blended.a = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);
        }

        _dot.color = blended;
    }

    private void OnModeChanged(AppMode mode) => ApplyMode(mode, instant: false);

    private void ApplyMode(AppMode mode, bool instant)
    {
        _targetColor = ModeToColor(mode);

        if (instant)
            _dot.color = _targetColor;
    }

    private Color ModeToColor(AppMode mode) => mode switch
    {
        AppMode.Default      => defaultColor,
        AppMode.Overview     => overviewColor,
        AppMode.PlantPicking => pickingColor,
        AppMode.Weeding      => weedingColor,
        _                    => defaultColor
    };
}