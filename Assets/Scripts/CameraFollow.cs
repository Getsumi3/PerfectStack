
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public StackManager stackManager;

    [Header("Camera Settings")]
    public float followStartHeight = 2.5f;
    public float targetScreenHeight = 2.5f;
    public float smoothSpeed = 3f;

    [Header("Camera Shake")]
    public float shakeDuration = 0.12f;
    public float shakeMagnitude = 0.06f;

    private float followY;
    private float velocity;
    private float shakeTimer;
    private float currentShakeMagnitude;
    private float originalX;
    private float originalZ;

    void Start()
    {
        followY = transform.position.y;
        originalX = transform.position.x;
        originalZ = transform.position.z;
    }

    void LateUpdate()
    {
        // CAMERA FOLLOW
        if (stackManager != null && stackManager.baseBlock != null)
        {
            Collider2D blockCollider =
                stackManager.baseBlock.GetComponent<Collider2D>();

            if (blockCollider != null)
            {
                float towerTop = blockCollider.bounds.max.y;

                if (towerTop > followY + followStartHeight)
                {
                    float targetY = towerTop - targetScreenHeight;

                    targetY = Mathf.Max(targetY, followY);

                    followY = Mathf.SmoothDamp(
                        followY,
                        targetY,
                        ref velocity,
                        1f / smoothSpeed
                    );
                }
            }
        }

        // CAMERA SHAKE
        Vector2 shakeOffset = Vector2.zero;

        if (shakeTimer > 0f)
        {
            float strength = shakeTimer / shakeDuration;

            shakeOffset = Random.insideUnitCircle *
                          currentShakeMagnitude * strength;

            shakeTimer -= Time.deltaTime;
        }

        if (shakeTimer <= 0f)
        {
            shakeTimer = 0f;
            currentShakeMagnitude = 0f;
        }

        // APPLY CAMERA POSITION
        transform.position = new Vector3(
            originalX + shakeOffset.x,
            followY + shakeOffset.y,
            originalZ
        );
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        if (duration <= 0f || magnitude <= 0f)
            return;

        shakeTimer = Mathf.Max(shakeTimer, duration);

        currentShakeMagnitude = Mathf.Max(
            currentShakeMagnitude,
            magnitude
        );
    }
}
