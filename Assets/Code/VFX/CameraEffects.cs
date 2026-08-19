using Unity.Cinemachine;
using UnityEngine;

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

    public void Shake(float force = -1f) 
    { 
        _impulseSource.GenerateImpulse(force < 0f ? defaultForce : force);
    }
}