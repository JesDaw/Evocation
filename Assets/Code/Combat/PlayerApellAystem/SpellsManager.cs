using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// so this is like the swapping aiming and activating of spells
/// there is a lot of code commented out idk what its all suposed to be for
/// </summary>

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
    bool IsAimingSpell = false;
    Vector2 magicRadiusHoverInput;
    private Color _startVignetteColor = Color.black, _flashVignetteColor = new Color(215/255f, 75/255f, 100/255f).linear;

    void Awake() => manaSystem = GetComponent<ManaSystem>();
    void Start()
    {
        if(PlayerSpells.Count == 0) return;
        detectionRadiusObject.gameObject.SetActive(false);
        SubscribeToSpells();

        OnSwapSpells.Invoke(PlayerSpells[(int)currentSpellContext]); // index error here
    }
    void SubscribeToSpells()
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.CastSpell.performed += _ => InvokeSpell();
        input.SwapSpell1.performed += _ => SwitchSpells(true);
        input.SwapSpell2.performed += _ => SwitchSpells(false);
    }
    void OnDestroy()
    {
        UnsubscribeToSpells();
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

    void InvokeSpell()
    {
        if(PlayerSpells.Count == 0) return;
        if(IsAimingSpell) // this is sthe actual activation of the spell
        {
            StartCoroutine(SpellCoroutine(PlayerSpells[(int)currentSpellContext]));
            return;
        }

        // this is for if you are aiming. Not all spells need to be aimed Btw sometime they just automatically instantly heal the player or do other things
        if(!manaSystem.SpendMana(PlayerSpells[(int)currentSpellContext].Cost)) return;
        IsAimingSpell = true;

        GlobalInputManager.Instance.DisableCursor();
        detectionRadiusObject.position = ActivePlayer.Instance.CurrentPlayer.transform.position;
        detectionRadiusObject.gameObject.SetActive(true);

        float radius = PlayerSpells[(int)currentSpellContext].Radius;
        detectionRadiusObject.localScale = new Vector3(radius, radius, 0);
        if(DebugLogs) Debug.Log("Spells Invoked");
    }
    void Update() //this is WeakReference you aim with the mouse
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;
        if(!input.enabled) return;

        magicRadiusHoverInput = input.Look.ReadValue<Vector2>() * 0.02f;
        detectionRadiusObject.position += new Vector3(magicRadiusHoverInput.x, magicRadiusHoverInput.y, 0);
    }

    public IEnumerator SpellCoroutine(PlayerSpells spell) // the spell animation and effects will differ from spell to spell it should noty be hardcodes directly into this manager script the effects should be managed on a lower lever like in the spells affector
    { //this needs to be broken up into sub functions its doing too much
        IsAimingSpell = false;
        detectionRadiusObject.GetComponentInChildren<Image>().enabled = false;

        //effects 
        // where is Shader first initially declared
        FModAudioManager.instance.PlaySoundByName("explosion");
        int vignetteStrengthID = Shader.PropertyToID("_VignetteStrength");
        int crackStrengthID = Shader.PropertyToID("_ScreenCrackOpacity");
        int vignetteColorID = Shader.PropertyToID("_VignetteColor");
        Shader.SetGlobalColor(vignetteColorID, _startVignetteColor);
        UILogic.pauseState ^= UILogic.PauseState.SpellPaused;
        Time.timeScale = 0;

        
        var spellVFX = Instantiate(spell.spellVFX, detectionRadiusObject.transform.position, Quaternion.identity);
        var v = GlobalInputManager.Instance.InputActions.UI;
        //making it so pausing the game pauses the spell effects? does time scale = 0 not automatically do that?
        Action<InputAction.CallbackContext> pauseAction = context => TogglePauseSpellParticleSystem(spellVFX);
        v.TogglePause.performed += pauseAction;
        // UILogic.PauseEvent.AddListener(() => TogglePauseSpellParticleSystem(spellVFX));

        //ok so this is how the actual hit is timed???
        float elapsed = 0f;
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
        // ive never seen ^= before idk what it does
        UILogic.pauseState ^= UILogic.PauseState.SpellPaused;
        Time.timeScale = UILogic.pauseState == UILogic.PauseState.Unpaused ? 1 : 0;

        v.TogglePause.performed -= pauseAction;

        detectionRadiusObject.GetComponentInChildren<Image>().enabled = true; //why is this getting enabled here?
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
        if(IsAimingSpell) return;
        int len = PlayerSpells.Count;
        currentSpellContext = (uint)((currentSpellContext + (_forward ? 1 : len - 1)) % len);
        OnSwapSpells.Invoke(PlayerSpells[(int)currentSpellContext]);
    }

    private Vector3 GetTargetWorldPosition(Vector2 _screenPos) //what the heck is this for?
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(_screenPos.x, _screenPos.y, Camera.main.nearClipPlane));
        worldPos.z = 0;
        return worldPos;
    }
    
    private void TogglePauseSpellParticleSystem(GameObject particleObj) //this is for when the game gets paused
    {
        foreach (ParticleSystem ps in particleObj.GetComponentsInChildren<ParticleSystem>())
        {
            var x = ps.main;
            x.useUnscaledTime = !x.useUnscaledTime;
        }
    }
    
    public IEnumerator CameraShake(float duration, float dampingMin, float maxOffset, float noiseRate) //this is an effect the didnt work IG but its fine bc I want to make a dedicated camera effect script
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


}
