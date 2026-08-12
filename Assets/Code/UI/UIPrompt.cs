using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class UIPrompt : MonoBehaviour
{
    [SerializeField] float minAlpha = 0.2f;
    [SerializeField] float maxAlpha = 1f;
    [SerializeField] float speed = 1.5f;

    TMP_Text _text;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.unscaledTime * speed, 1f);
        float a = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = _text.color;
        c.a = a;
        _text.color = c;
    }
}