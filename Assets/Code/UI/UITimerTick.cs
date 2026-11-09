using UnityEngine;
using TMPro;

/// <summary>
/// Pulses the timer text on every second and makes it more urgent as time runs out.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class UITimerTick : MonoBehaviour
{
    [Tooltip("How long each pulse takes to settle.")]
    [Min(0.01f)] [SerializeField] float pulseSeconds = 0.3f;
    [Tooltip("How much the text grows on each second, before the warning starts.")]
    [Range(0f, 1f)] [SerializeField] float calmPulse = 0.03f;

    [Header("Warning")]
    [Min(0f)] [SerializeField] int warningSeconds = 60;
    [Range(0f, 1f)] [SerializeField] float warningPulse = 0.08f;
    [SerializeField] Color warningColor = new Color(1f, 0.72f, 0.15f);

    [Header("Critical")]
    [Min(0f)] [SerializeField] int criticalSeconds = 10;
    [Range(0f, 1f)] [SerializeField] float criticalPulse = 0.22f;
    [SerializeField] Color criticalColor = new Color(1f, 0.2f, 0.18f);

    TMP_Text _text;
    Color _calmColor;
    int _lastSecond = -1;
    float _pulseStart = float.NegativeInfinity;
    float _pulseSize;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _calmColor = _text.color;
    }

    void Update()
    {
        Timer timer = Timer.Instance;
        if (timer == null) return;

        // Same rounding as the timer's own display, so the pulse lands as the digits change.
        int second = Mathf.FloorToInt(timer.RemainingTimeSeconds);

        if (second != _lastSecond)
        {
            _lastSecond = second;
            if (timer.TimeIsActive)
            {
                _pulseStart = Time.time;
                _pulseSize = second <= criticalSeconds ? criticalPulse : second <= warningSeconds ? warningPulse : calmPulse;
            }
        }

        _text.color = second <= criticalSeconds ? criticalColor : second <= warningSeconds ? warningColor : _calmColor;

        float remaining = 1f - Mathf.Clamp01((Time.time - _pulseStart) / pulseSeconds);
        transform.localScale = Vector3.one * (1f + _pulseSize * remaining * remaining);
    }
}
