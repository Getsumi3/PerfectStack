
using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(100)]
public class CloudSpawner : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Transform[] clouds;

    [Header("Cloud Spawning")]
    public float minVerticalSpacing = 2.5f;
    public float maxVerticalSpacing = 4f;
    public float recycleMargin = 2f;
    public float horizontalPadding = 0.5f;
    public float spawnMargin = 1.5f;

    [Header("Cloud Drift")]
    public float driftDistance = 0.25f;
    public float minDriftSpeed = 0.2f;
    public float maxDriftSpeed = 0.4f;

    private float[] cloudBaseX;
    private float[] cloudDriftSpeed;
    private float[] cloudDriftPhase;

    private SpriteRenderer[] cloudRenderers;

    private Color currentCloudColor = Color.white;
    private Coroutine colorTransition;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        cloudRenderers = new SpriteRenderer[clouds.Length];

        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] != null)
            {
                cloudRenderers[i] =
                    clouds[i].GetComponentInChildren<SpriteRenderer>();

                if (cloudRenderers[i] != null)
                    cloudRenderers[i].color = currentCloudColor;
            }
        }

        cloudBaseX = new float[clouds.Length];
        cloudDriftSpeed = new float[clouds.Length];
        cloudDriftPhase = new float[clouds.Length];

        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] == null)
                continue;

            cloudBaseX[i] = clouds[i].position.x;

            cloudDriftSpeed[i] = Random.Range(
                minDriftSpeed,
                maxDriftSpeed
            );

            cloudDriftPhase[i] =
                Random.Range(0f, Mathf.PI * 2f);
        }
    }

    public void TransitionCloudColor(Color targetColor, float duration = 3f)
    {
        if (!Application.isPlaying)
            return;

        if (colorTransition != null)
            StopCoroutine(colorTransition);

        colorTransition = StartCoroutine(
            TransitionCloudColorRoutine(targetColor, duration)
        );
    }

    IEnumerator TransitionCloudColorRoutine(
        Color targetColor,
        float duration)
    {
        Color startColor = currentCloudColor;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            currentCloudColor = Color.Lerp(
                startColor,
                targetColor,
                t
            );

            ApplyCloudColor();

            yield return null;
        }

        currentCloudColor = targetColor;
        ApplyCloudColor();

        colorTransition = null;
    }

    void ApplyCloudColor()
    {
        if (cloudRenderers == null)
            return;

        for (int i = 0; i < cloudRenderers.Length; i++)
        {
            if (cloudRenderers[i] != null)
                cloudRenderers[i].color = currentCloudColor;
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null || !mainCamera.orthographic)
            return;

        float cameraBottom =
            mainCamera.transform.position.y -
            mainCamera.orthographicSize;

        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] == null || cloudRenderers[i] == null)
                continue;

            float cloudBottom = cloudRenderers[i].bounds.min.y;

            if (cloudBottom < cameraBottom - recycleMargin)
                RecycleCloud(i);
        }

        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] == null)
                continue;

            float offset = Mathf.Sin(
                Time.time * cloudDriftSpeed[i] +
                cloudDriftPhase[i]
            ) * driftDistance;

            Vector3 position = clouds[i].position;
            position.x = cloudBaseX[i] + offset;
            clouds[i].position = position;
        }
    }

    void RecycleCloud(int cloudIndex)
    {
        float highestY = float.MinValue;

        for (int i = 0; i < clouds.Length; i++)
        {
            if (i == cloudIndex || clouds[i] == null)
                continue;

            highestY = Mathf.Max(
                highestY,
                clouds[i].position.y
            );
        }

        if (highestY == float.MinValue)
            highestY = mainCamera.transform.position.y;

        float cameraTop =
            mainCamera.transform.position.y +
            mainCamera.orthographicSize;

        float cloudHalfHeight =
            cloudRenderers[cloudIndex].bounds.extents.y;

        float spacingY = highestY +
            Random.Range(minVerticalSpacing, maxVerticalSpacing);

        float aboveCameraY =
            cameraTop + cloudHalfHeight + spawnMargin;

        float newY = Mathf.Max(spacingY, aboveCameraY);

        float cameraHalfWidth =
            mainCamera.orthographicSize * mainCamera.aspect;

        float cloudHalfWidth =
            cloudRenderers[cloudIndex].bounds.extents.x;

        float minX = -cameraHalfWidth - cloudHalfWidth * 0.75f;
        float maxX = cameraHalfWidth + cloudHalfWidth * 0.75f;

        float newX = Random.Range(minX, maxX);

        Vector3 newPosition = clouds[cloudIndex].position;
        newPosition.x = mainCamera.transform.position.x + newX;
        newPosition.y = newY;

        clouds[cloudIndex].position = newPosition;
        cloudBaseX[cloudIndex] = newPosition.x;
    }

    void OnDisable()
    {
        if (colorTransition != null)
        {
            StopCoroutine(colorTransition);
            colorTransition = null;
        }
    }
}
