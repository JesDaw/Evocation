using TMPro;
using UnityEngine;

[System.Serializable]

public class BackgroundElement
{
    public SpriteRenderer backgroundSprite;
    [Range(0, 1)] public float scrollSpeed;
    [HideInInspector] public Material spriteMaterial;
}

public class ParallaxEffect2D : MonoBehaviour
{
    private const float ScrollMultiplier = 0.01f;

    [SerializeField] private BackgroundElement[] backgroundElement;
    private void Start()
    {
        foreach (BackgroundElement element in backgroundElement)
        {
            element.spriteMaterial = element.backgroundSprite.material;
        }
    }

    private void Update()
    {
        foreach(BackgroundElement element in backgroundElement)
        {
            element.spriteMaterial.mainTextureOffset = new Vector2(transform.position.x * element.scrollSpeed * ScrollMultiplier, 0);
        }
    }
}