using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Central place for camera-side feedback. Uses Cinemachine Impulse instead
/// of writing to Camera.main.transform directly - a CinemachineBrain drives
/// that transform every frame and will fight a manual position write, which
/// is almost certainly why the old CameraShake coroutine "didn't work."
///
/// Scene setup required: add a CinemachineImpulseSource to this object, and
/// a CinemachineImpulseListener extension on whichever virtual camera(s)
/// should react to it.
/// </summary>
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraEffects : MonoBehaviour
{
    public static CameraEffects Instance { get; private set; }

    [SerializeField] float defaultForce = 1f;

    CinemachineImpulseSource _impulseSource;

    void Awake()
    {
        Instance = this;
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void Shake(float force = -1f) =>
        _impulseSource.GenerateImpulse(force < 0f ? defaultForce : force);
}