using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ManaSystem))]
public class SpellsManager : MonoBehaviour
{
    public List<PlayerSpells> PlayerSpells = new List<PlayerSpells>();
    ManaSystem manaSystem;
    public List<Transform> CurrentlySelected = new List<Transform>();
    public UnityEvent<PlayerSpells> OnSwapSpells;
    [SerializeField] uint currentSpellContext = 0;
    [SerializeField] Transform detectionRadiusObject;
    [SerializeField] bool DebugLogs = false;
    //this means spell is ready and primed (can't be switched)
    bool charged = false;
    Vector2 magicRadiusHoverInput;
    private Color _startVignetteColor = Color.black, _flashVignetteColor = new Color(215/255f, 75/255f, 100/255f).linear;

    void Awake() => manaSystem = GetComponent<ManaSystem>();

    void InvokeSpell()
    {
        if(PlayerSpells.Count == 0) return;
        if(charged)
        {
            // UseSpell();
            // Debug.Log("Starting Coroutine spellCoroutine");
            StartCoroutine(SpellCoroutine(PlayerSpells[(int)currentSpellContext]));
            return;
        }

        if(!manaSystem.SpendMana(PlayerSpells[(int)currentSpellContext].Cost)) return;
        charged = true;

        GlobalInputManager.Instance.DisableCursor();
        detectionRadiusObject.position = ActivePlayer.Instance.CurrentPlayer.transform.position;
        detectionRadiusObject.gameObject.SetActive(true);

        float radius = PlayerSpells[(int)currentSpellContext].Radius;
        detectionRadiusObject.localScale = new Vector3(radius, radius, 0);
        if(DebugLogs) Debug.Log("Spells Invoked");
    }

    void UseSpell()
    {
        if(PlayerSpells.Count == 0) return;
        PlayerSpells[(int)currentSpellContext].OnHit.Invoke(CurrentlySelected.ToArray());
        PlayerSpells[(int)currentSpellContext].OnHitPosition.Invoke(detectionRadiusObject);
        if(DebugLogs) Debug.Log("Spells Used");
        detectionRadiusObject.gameObject.SetActive(false);
    }

    void SwitchSpells(bool _forward)
    {
        if(charged) return;
        int len = PlayerSpells.Count;
        currentSpellContext = (uint)((currentSpellContext + (_forward ? 1 : len - 1)) % len);
        OnSwapSpells.Invoke(PlayerSpells[(int)currentSpellContext]);
    }

    void Start()
    {
        if(PlayerSpells.Count == 0) return;
        detectionRadiusObject.gameObject.SetActive(false);
        SubscribeToSpells();

        OnSwapSpells.Invoke(PlayerSpells[(int)currentSpellContext]); // index error here
    }

    void Update()
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;
        if(!input.enabled) return;

        magicRadiusHoverInput = input.Look.ReadValue<Vector2>() * 0.02f;

        detectionRadiusObject.position +=
            new Vector3(magicRadiusHoverInput.x, magicRadiusHoverInput.y, 0);
    }

    void SubscribeToSpells()
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.CastSpell.performed += _ => InvokeSpell();
        input.SwapSpell1.performed += _ => SwitchSpells(true);
        input.SwapSpell2.performed += _ => SwitchSpells(false);
    }

    void UnsubscribeToSpells()
    {
        //not sure if this would work...
        //ehh might as well
        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.CastSpell.performed -= _ => InvokeSpell();
        input.SwapSpell1.performed -= _ => SwitchSpells(true);
        input.SwapSpell2.performed -= _ => SwitchSpells(false);
    }

    private Vector3 GetTargetWorldPosition(Vector2 _screenPos)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(_screenPos.x, _screenPos.y, Camera.main.nearClipPlane));
        worldPos.z = 0;
        return worldPos;
    }
    
    public IEnumerator SpellCoroutine(PlayerSpells spell)
    {
        FModAudioManager.instance.PlaySoundByName("explosion");
        charged = false;
        detectionRadiusObject.GetComponentInChildren<Image>().enabled = false;
        int vignetteStrengthID = Shader.PropertyToID("_VignetteStrength");
        int crackStrengthID = Shader.PropertyToID("_ScreenCrackOpacity");
        int vignetteColorID = Shader.PropertyToID("_VignetteColor");
        Shader.SetGlobalColor(vignetteColorID, _startVignetteColor);
        UILogic.pauseState ^= UILogic.PauseState.SpellPaused;
        Time.timeScale = 0;
        float elapsed = 0f;
        var spellVFX = Instantiate(spell.spellVFX, detectionRadiusObject.transform.position, Quaternion.identity);
        var v = GlobalInputManager.Instance.InputActions.UI;
        Action<InputAction.CallbackContext> pauseAction = context => TogglePauseSpellParticleSystem(spellVFX);
        v.TogglePause.performed += pauseAction;
        // UILogic.PauseEvent.AddListener(() => TogglePauseSpellParticleSystem(spellVFX));
        while (elapsed < spell.hitboxDelay)
        {
            if(!UILogic.GameIsPaused)
            {
                elapsed += Time.unscaledDeltaTime;
                Shader.SetGlobalFloat(vignetteStrengthID, Mathf.Sqrt(elapsed / spell.hitboxDelay));
            }
            yield return null;
        }
        
        UseSpell();
        UILogic.pauseState ^= UILogic.PauseState.SpellPaused;
        Time.timeScale = UILogic.pauseState == UILogic.PauseState.Unpaused ? 1 : 0;
        v.TogglePause.performed -= pauseAction;
        detectionRadiusObject.GetComponentInChildren<Image>().enabled = true;
        // StartCoroutine(CameraShake(spell.animationDuration - spell.hitboxDelay, 0.30f, 2f, 10f));
        while (elapsed < spell.animationDuration)
        {
            if (!UILogic.GameIsPaused)
            {
                elapsed += Time.unscaledDeltaTime;
                float vignette_crack_strength =
                    1 - Mathf.Sqrt(Mathf.InverseLerp(spell.hitboxDelay, spell.animationDuration, elapsed));
                Shader.SetGlobalFloat(vignetteStrengthID, vignette_crack_strength);
                Shader.SetGlobalFloat(crackStrengthID, vignette_crack_strength);
                // Shader.SetGlobalColor(vignetteColorID, Color.Lerp(_flashVignetteColor, _startVignetteColor, Mathf.Pow(Mathf.InverseLerp(spell.hitboxDelay, spell.animationDuration, elapsed), 3)));
            }
            yield return null;
        }
        Shader.SetGlobalColor(vignetteColorID, _startVignetteColor);
        // UILogic.PauseEvent.RemoveListener(() => TogglePauseSpellParticleSystem(spellVFX));
        // v.TogglePause.performed -= pauseAction;
        // detectionRadiusObject.GetComponentInChildren<Image>().enabled = true;
        Shader.SetGlobalFloat(vignetteStrengthID, 0);
        Destroy(spellVFX);
        // UILogic.pauseState ^= UILogic.PauseState.SpellPaused;
        // Time.timeScale = UILogic.pauseState == UILogic.PauseState.Unpaused ? 1 : 0;
        yield return null;
    }
    
    private void TogglePauseSpellParticleSystem(GameObject particleObj)
    {
        foreach (ParticleSystem ps in particleObj.GetComponentsInChildren<ParticleSystem>())
        {
            var x = ps.main;
            x.useUnscaledTime = !x.useUnscaledTime;
        }
    }
    
    public IEnumerator CameraShake(float duration, float dampingMin, float maxOffset, float noiseRate)
    {
        float elapsed = 0f;
        Vector3 initialPos = transform.position;
        float sinOscillator, dampMultiplier, x, y;
        
        while (elapsed < duration)
        {
            sinOscillator = Mathf.Sin(elapsed / duration * Mathf.PI);
            dampMultiplier = (elapsed / duration > 0.5f) ? dampingMin * (1 - sinOscillator) + sinOscillator : sinOscillator;
            x = dampMultiplier * maxOffset * ((Mathf.PerlinNoise1D(Random.value*10000f + elapsed * noiseRate)) - 0.5f);
            y = dampMultiplier * maxOffset * ((Mathf.PerlinNoise1D(Random.value*10000f - elapsed * noiseRate)) - 0.5f)/2;

            Camera.main.transform.position = new Vector3(initialPos.x + x, initialPos.y + y, initialPos.z);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void OnDestroy()
    {
        UnsubscribeToSpells();
    }
}
