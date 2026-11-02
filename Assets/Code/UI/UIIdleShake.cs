using UnityEngine;

/// <summary>
/// Gives a UI element a short shake around its pivot that settles back upright, then rests until the next one.
/// </summary>
public class UIIdleShake : MonoBehaviour
{
    [Tooltip("How far the first swing of each shake goes, in degrees. Later swings get smaller.")]
    [Range(0f, 45f)] [SerializeField] float shakeDegrees = 6f;
    [Tooltip("How long each shake lasts.")]
    [Min(0.01f)] [SerializeField] float shakeSeconds = 0.5f;
    [Tooltip("How many times it swings back and forth during a shake.")]
    [Min(1)] [SerializeField] int shakeCount = 3;
    [Tooltip("Time spent still between shakes.")]
    [Min(0f)] [SerializeField] float restSeconds = 2.5f;

    void Update()
    {
        float progress = Mathf.Repeat(Time.time, shakeSeconds + restSeconds) / shakeSeconds;

        float angle = 0f;
        if (progress < 1f)
            angle = Mathf.Sin(progress * shakeCount * Mathf.PI * 2f) * (1f - progress) * shakeDegrees;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
