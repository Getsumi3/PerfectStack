
using UnityEngine;
using System.Collections;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class PastelGradientBackground : MonoBehaviour
{
    [Header("Gradient Colors")]
    public Color bottomColor = new Color32(184, 228, 245, 255);
    public Color middleColor = new Color32(211, 211, 248, 255);
    public Color topColor = new Color32(245, 213, 235, 255);

    [Header("Texture Settings")]
    [Min(2)]
    public int textureHeight = 256;

    private SpriteRenderer spriteRenderer;
    private Texture2D generatedTexture;
    private Sprite generatedSprite;

    private Coroutine transitionRoutine;

    void OnEnable()
    {
        RefreshGradient();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= DelayedRefresh;
            UnityEditor.EditorApplication.delayCall += DelayedRefresh;
            return;
        }
#endif
        RefreshGradient();
    }

#if UNITY_EDITOR
    void DelayedRefresh()
    {
        if (this == null || !isActiveAndEnabled)
            return;

        RefreshGradient();
    }
#endif

    void RefreshGradient()
    {
        if (!isActiveAndEnabled)
            return;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        int height = Mathf.Max(2, textureHeight);

        Texture2D texture = new Texture2D(
            2,
            height,
            TextureFormat.RGBA32,
            false
        );

        texture.name = "Generated_SkyGradient";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.DontSave;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, 2, height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        sprite.name = "Generated_SkyGradient_Sprite";
        sprite.hideFlags = HideFlags.DontSave;

        Sprite oldSprite = generatedSprite;
        Texture2D oldTexture = generatedTexture;

        generatedSprite = sprite;
        generatedTexture = texture;

        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = -100;

        UpdateGradientTexture();

        if (oldSprite != null)
            DestroyGenerated(oldSprite);

        if (oldTexture != null)
            DestroyGenerated(oldTexture);

        UpdatePositionAndScale();
    }

    void UpdateGradientTexture()
    {
        if (generatedTexture == null)
            return;

        int height = generatedTexture.height;

        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);

            Color color = t < 0.5f
                ? Color.Lerp(bottomColor, middleColor, t * 2f)
                : Color.Lerp(middleColor, topColor, (t - 0.5f) * 2f);

            generatedTexture.SetPixel(0, y, color);
            generatedTexture.SetPixel(1, y, color);
        }

        generatedTexture.Apply();
    }

    public void TransitionToTheme(
        Color targetBottom,
        Color targetMiddle,
        Color targetTop,
        float duration = 3f)
    {
        if (!Application.isPlaying)
            return;

        if (generatedTexture == null)
            RefreshGradient();

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(
            TransitionRoutine(
                targetBottom,
                targetMiddle,
                targetTop,
                duration
            )
        );
    }

    IEnumerator TransitionRoutine(
        Color targetBottom,
        Color targetMiddle,
        Color targetTop,
        float duration)
    {
        Color startBottom = bottomColor;
        Color startMiddle = middleColor;
        Color startTop = topColor;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth transition.
            t = Mathf.SmoothStep(0f, 1f, t);

            bottomColor = Color.Lerp(startBottom, targetBottom, t);
            middleColor = Color.Lerp(startMiddle, targetMiddle, t);
            topColor = Color.Lerp(startTop, targetTop, t);

            UpdateGradientTexture();

            yield return null;
        }

        bottomColor = targetBottom;
        middleColor = targetMiddle;
        topColor = targetTop;

        UpdateGradientTexture();

        transitionRoutine = null;
    }

    void LateUpdate()
    {
        UpdatePositionAndScale();
    }

    void UpdatePositionAndScale()
    {
        Camera cam = Camera.main;

        if (cam == null || !cam.orthographic || generatedSprite == null)
            return;

        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;

        transform.position = new Vector3(
            cam.transform.position.x,
            cam.transform.position.y,
            5f
        );

        transform.localScale = new Vector3(
            viewWidth / generatedSprite.bounds.size.x,
            viewHeight / generatedSprite.bounds.size.y,
            1f
        );
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= DelayedRefresh;
#endif

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        if (spriteRenderer != null &&
            spriteRenderer.sprite == generatedSprite)
        {
            spriteRenderer.sprite = null;
        }

        if (generatedSprite != null)
            DestroyGenerated(generatedSprite);

        if (generatedTexture != null)
            DestroyGenerated(generatedTexture);

        generatedSprite = null;
        generatedTexture = null;
        transitionRoutine = null;
    }

    void DestroyGenerated(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
