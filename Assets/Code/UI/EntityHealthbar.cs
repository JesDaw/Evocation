using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EntityHealthbar : HealthBarBase
{
    // The visible bar color comes from these properties on the Healthbar shader graph material
    // (the fill/trail images are only masks).
    static readonly int HealthColor1 = Shader.PropertyToID("_HealthColor1");
    static readonly int HealthColor2 = Shader.PropertyToID("_HealthColor2");

    // One green copy per source material, shared by every ally bar.
    static readonly Dictionary<(Material, Color), Material> AllyMaterials = new();

    [SerializeField] bool DebugLogs;
    [Tooltip("Health bar color for your side (allies, home base). Enemies keep the shader's red.")]
    [SerializeField] Color allyHealthColor = new Color(0.35f, 1f, 0.42f);
    Stats _Stats;

    readonly List<(Image image, Material original)> _colorImages = new();
    bool _colorImagesFound;
    bool? _appliedAsAlly;

    public void Initialize(Stats statsComponent)
    {
        _Stats = statsComponent;
        UpdateHealth();
    }

    // Checked on every update because spawners set _Enemy after Awake, where Initialize runs.
    void ApplyTeamColor()
    {
        if (!_colorImagesFound)
        {
            _colorImagesFound = true;
            foreach (var image in GetComponentsInChildren<Image>(true))
            {
                var mat = image.material;
                if (mat != null && mat.HasProperty(HealthColor1))
                    _colorImages.Add((image, mat));
            }
        }

        bool isAlly = !_Stats._Enemy;
        if (_appliedAsAlly == isAlly) return;
        _appliedAsAlly = isAlly;

        foreach (var (image, original) in _colorImages)
            image.material = isAlly ? GetAllyMaterial(original) : original;
    }

    Material GetAllyMaterial(Material original)
    {
        if (!AllyMaterials.TryGetValue((original, allyHealthColor), out var ally) || ally == null)
        {
            ally = new Material(original) { name = original.name + " (Ally)" };
            SetColorKeepAlpha(ally, HealthColor1, allyHealthColor);
            SetColorKeepAlpha(ally, HealthColor2, allyHealthColor);
            AllyMaterials[(original, allyHealthColor)] = ally;
        }
        return ally;
    }

    static void SetColorKeepAlpha(Material mat, int property, Color color)
    {
        if (!mat.HasProperty(property)) return;
        color.a = mat.GetColor(property).a;
        mat.SetColor(property, color);
    }

    protected override void Start()
    {
        base.Start();   
    }

    public override void UpdateHealth()
    {
        if (_Stats == null || _HealthFillImage == null || _HealthTrailImage == null)
        {
            Debug.LogError($"{gameObject.name} heathbar is missing reference!");
            return;
        }

        float ratio = _Stats._MaxHealth > 0 ? (float)_Stats._CurrentHealth / _Stats._MaxHealth : 0f;
        ratio = Mathf.Clamp01(ratio);

        ApplyTeamColor();
        AnimateHealthChange(ratio);

        if (HealthBarObject != null)
        {
            HealthBarObject.SetActive(_Stats._CurrentHealth < _Stats._MaxHealth && _Stats._CurrentHealth > 0);
        }
        if(DebugLogs) Debug.Log($"{gameObject.name} health now at {_Stats._CurrentHealth}/{_Stats._MaxHealth}");
    }

    void OnDestroy()
    {

        if (_healthBarSequence != null)
        {
            _healthBarSequence.Kill();
            _healthBarSequence = null;
        }
        if (_HealthFillImage != null)
            DOTween.Kill(_HealthFillImage);
        if (_HealthTrailImage != null)
            DOTween.Kill(_HealthTrailImage);
    }
}