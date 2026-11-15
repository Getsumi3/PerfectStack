
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StarfieldController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;

    [Header("Star Settings")]
    public int twilightStarCount = 10;
    public int nightStarCount = 35;

    public float minStarSize = 0.035f;
    public float maxStarSize = 0.075f;

    [Header("Appearance")]
    public Color starColor = new Color32(255, 250, 245, 255);
    public float twilightBrightness = 0.25f;
    public float nightBrightness = 0.85f;

    private class Star
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float phase;
        public float twinkleSpeed;
        public float brightness;
    }

    private readonly List<Star> stars = new List<Star>();

    private Sprite starSprite;
    private int currentStage = 0;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        starSprite = CreateStarSprite();
        CreateStars();
        SetStage(0);
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            return;

        Vector3 cameraPosition = mainCamera.transform.position;
        transform.position = new Vector3(
            cameraPosition.x,
            cameraPosition.y,
            4f
        );

        foreach (Star star in stars)
        {
            float twinkle = Mathf.Lerp(
                0.75f,
                1f,
                (Mathf.Sin(Time.time * star.twinkleSpeed + star.phase) + 1f) * 0.5f
            );

            Color color = starColor;
            color.a = star.brightness * twinkle;

            star.renderer.color = color;
        }
    }

    void CreateStars()
    {
        int totalStars = Mathf.Max(twilightStarCount, nightStarCount);

        float height = mainCamera.orthographicSize * 2f;
        float width = height * mainCamera.aspect;

        for (int i = 0; i < totalStars; i++)
        {
            GameObject starObject = new GameObject("Star_" + i);
            starObject.transform.SetParent(transform, false);

            float x = Random.Range(-width * 0.48f, width * 0.48f);
            float y = Random.Range(-height * 0.48f, height * 0.48f);

            starObject.transform.localPosition = new Vector3(x, y, 0f);

            float size = Random.Range(minStarSize, maxStarSize);
            starObject.transform.localScale = Vector3.one * size;

            SpriteRenderer sr = starObject.AddComponent<SpriteRenderer>();
            sr.sprite = starSprite;
            sr.sortingOrder = -95;

            stars.Add(new Star
            {
                transform = starObject.transform,
                renderer = sr,
                phase = Random.Range(0f, Mathf.PI * 2f),
                twinkleSpeed = Random.Range(0.6f, 1.4f),
                brightness = 0f
            });
        }
    }


    private Coroutine stageTransition;

    public void SetStage(int stage)
    {
        currentStage = stage;

        if (stageTransition != null)
            StopCoroutine(stageTransition);

        stageTransition = StartCoroutine(TransitionStage(stage));
    }

    IEnumerator TransitionStage(int stage)
    {
        int visibleCount = 0;
        float targetBrightness = 0f;

        if (stage == 3)
        {
            visibleCount = twilightStarCount;
            targetBrightness = twilightBrightness;
        }
        else if (stage >= 4)
        {
            visibleCount = nightStarCount;
            targetBrightness = nightBrightness;
        }

        float[] startingBrightness = new float[stars.Count];

        for (int i = 0; i < stars.Count; i++)
        {
            startingBrightness[i] = stars[i].brightness;

            if (i < visibleCount)
                stars[i].renderer.enabled = true;
        }

        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / duration)
            );

            for (int i = 0; i < stars.Count; i++)
            {
                float target = i < visibleCount
                    ? targetBrightness
                    : 0f;

                stars[i].brightness = Mathf.Lerp(
                    startingBrightness[i], target, t
                );
            }

            yield return null;
        }

        for (int i = 0; i < stars.Count; i++)
        {
            stars[i].brightness = i < visibleCount
                ? targetBrightness
                : 0f;

            stars[i].renderer.enabled = i < visibleCount;
        }

        stageTransition = null;
    }

    Sprite CreateStarSprite()
    {
        const int size = 64;

        Texture2D texture = new Texture2D(
            size, size, TextureFormat.RGBA32, false
        );

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y) - center;

                float angle = Mathf.Atan2(p.y, p.x);
                float radius = p.magnitude;

                float starRadius = 24f +
                    10f * Mathf.Cos(angle * 5f);

                float alpha = Mathf.Clamp01(
                    (starRadius - radius) * 0.8f
                );

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return sprite;
    }
}
