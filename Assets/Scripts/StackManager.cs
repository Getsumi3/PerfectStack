using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class StackManager : MonoBehaviour
{
    [Header("References")] public Transform baseBlock;
    public GameObject movingBlockPrefab;
    public MovingBlock currentBlock;

    [Header("UI")] public TMP_Text scoreText;
    public TMP_Text bestScoreText;
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text finalBestScoreText;

    [Header("Settings")] public float spawnDistance = 2f;
    public float perfectThreshold = 0.05f;
    public int perfectBonus = 2;
    public Vector3 baseBlockSize = new Vector3(3, 0.5f, 1);

    [Header("Block Colors")] public Color[] blockColors;
    private int colorIndex = 0;

    [Header("Visual Effects")] public ParticleSystem landingParticlePrefab;

    [Header("Camera Effects")] public CameraFollow cameraFollow;

    [Header("Procedural Audio")] public ProceduralAudio proceduralAudio;

    [Header("Background")]
    public PastelGradientBackground background;
    public CloudSpawner cloudSpawner;
    public StarfieldController starfield;
    
    private int score = 0;
    private int bestScore = 0;
    private bool isProcessing = false;
    private bool gameOver = false;

    private int perfectStreak = 0;

    private bool nextSpawnFromLeft = true;
    
    private int currentBackgroundStage = 0;
    void Start()
    {
        bestScore = PlayerPrefs.GetInt("BestScore", 0);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateUI();

        if (currentBlock != null)
            currentBlock.manager = this;

        currentBlock.SetGameplayEnabled(false);

        if (proceduralAudio == null)
        {
            proceduralAudio = GetComponent<ProceduralAudio>();
        }
    }

    public void LandBlock(MovingBlock block)
    {
        if (isProcessing || gameOver)
            return;

        if (block != currentBlock)
            return;

        isProcessing = true;

        Bounds baseBounds =
            baseBlock.GetComponent<Collider2D>().bounds;

        Bounds blockBounds =
            block.GetComponent<Collider2D>().bounds;

        float leftEdge = Mathf.Max(
            baseBounds.min.x,
            blockBounds.min.x
        );

        float rightEdge = Mathf.Min(
            baseBounds.max.x,
            blockBounds.max.x
        );

        float overlap = rightEdge - leftEdge;

        // Game Over if there is no overlap.
        if (overlap <= 0f)
        {
            GameOver();
            return;
        }

        float centerX = (leftEdge + rightEdge) / 2f;

        // Check for a perfect alignment.
        bool isPerfect =
            Mathf.Abs(blockBounds.center.x -
                      baseBounds.center.x) <= perfectThreshold;

        if (isPerfect)
        {
            perfectStreak++;

            SpriteRenderer blockRenderer =
                block.GetComponent<SpriteRenderer>();

            Color particleColor = blockRenderer != null
                ? blockRenderer.color
                : Color.white;

            PlayLandingEffect(
                centerX,
                baseBounds.max.y,
                particleColor
            );
        }
        else
        {
            perfectStreak = 0;
        }


        // Position the remaining block.
        Vector3 position = block.transform.position;

        if (isPerfect)
        {
            // Snap the collider center exactly to the previous block's center.
            position.x += baseBounds.center.x - blockBounds.center.x;
        }
        else
        {
            // Center the remaining overlap.
            position.x += centerX - blockBounds.center.x;
        }

        block.transform.position = position;

        // Cut away the overhanging portion only if the hit isn't perfect.
        if (!isPerfect)
        {
            Vector3 scale = block.transform.localScale;
            scale.x = overlap;
            block.transform.localScale = scale;
        }
        else
        {
            if (perfectStreak % 5 == 0)
            {
                block.transform.localScale = baseBlockSize;
            }
        }

        StartCoroutine(LandingBounce(block.transform));

        // Stop physics.
        Rigidbody2D rb = block.GetComponent<Rigidbody2D>();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Update the new base.
        baseBlock = block.transform;

        // Increase score.
        score++;

        if (proceduralAudio != null)
        {
            proceduralAudio.PlayLanding(perfectStreak);
        }

        if (isPerfect)
        {
            score += perfectBonus;

            if (cameraFollow != null)
                cameraFollow.ShakeCamera(0.12f, 0.045f);

            Debug.Log("PERFECT STACK!");
        }
        else
        {
            if (cameraFollow != null)
                cameraFollow.ShakeCamera(0.08f, 0.018f);
        }
        
        UpdateBackgroundTheme();

        // Save best score.
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        UpdateUI();

        Debug.Log("Score: " + score);

        Invoke(nameof(SpawnNextBlock), 0.5f);
    }

    void SpawnNextBlock()
    {
        if (gameOver)
            return;

        Bounds bounds =
            baseBlock.GetComponent<Collider2D>().bounds;

        float newY = bounds.max.y +
                     baseBlock.localScale.y / 2f +
                     spawnDistance;

        float spawnX = nextSpawnFromLeft ? -3f : 3f;

        Vector3 spawnPosition = new Vector3(
            spawnX,
            newY,
            0f
        );

        GameObject newBlock = Instantiate(
            movingBlockPrefab,
            spawnPosition,
            Quaternion.identity
        );

        SpriteRenderer sr =
            newBlock.GetComponent<SpriteRenderer>();

        if (sr != null && blockColors.Length > 1)
        {
            sr.color = GetGradientColor(colorIndex);
            colorIndex++;
        }

        newBlock.transform.localScale = currentBlock.transform.localScale;

        currentBlock = newBlock.GetComponent<MovingBlock>();
        currentBlock.speed = GetBlockSpeed();
        currentBlock.fallGravity = GetBlockGravity();
        currentBlock.SetMovingDirection(nextSpawnFromLeft);
        nextSpawnFromLeft = !nextSpawnFromLeft;

        currentBlock.manager = this;

        currentBlock.SetGameplayEnabled(true);

        isProcessing = false;

        Debug.Log("New block spawned!");
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = score > 0 ? score.ToString() : "";

        if (bestScoreText != null)
            bestScoreText.text = "BEST SCORE " + bestScore;
    }

    public void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;
        isProcessing = true;

        if (currentBlock != null)
        {
            Rigidbody2D rb =
                currentBlock.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = score.ToString();
            finalBestScoreText.text = "BEST SCORE " + bestScore;
            scoreText.text = "";
        }

        if (proceduralAudio != null)
        {
            proceduralAudio.PlayGameOver();
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.Log("GAME OVER!");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }


    void PlayLandingEffect(float x, float y, Color blockColor)
    {
        if (landingParticlePrefab == null)
            return;

        Vector3 effectPosition = new Vector3(x, y, -0.2f);

        ParticleSystem effect = Instantiate(
            landingParticlePrefab,
            effectPosition,
            Quaternion.identity
        );

        var main = effect.main;

        main.startColor = blockColor;

        effect.Play();

        Destroy(effect.gameObject, 2f);
    }

    IEnumerator LandingBounce(Transform block)
    {
        Vector3 originalScale = block.localScale;

        Vector3 squashScale = originalScale;
        squashScale.y *= 0.82f;

        Vector3 stretchScale = originalScale;
        stretchScale.y *= 1.08f;

        float duration = 0.07f;
        float elapsed = 0f;

        // Squash
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            block.localScale = Vector3.Lerp(
                originalScale,
                squashScale,
                elapsed / duration
            );

            yield return null;
        }

        // Bounce
        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            block.localScale = Vector3.Lerp(
                squashScale,
                stretchScale,
                elapsed / duration
            );

            yield return null;
        }

        // Return to original size
        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            block.localScale = Vector3.Lerp(
                stretchScale,
                originalScale,
                elapsed / duration
            );

            yield return null;
        }

        block.localScale = originalScale;
    }

    public void BeginGame()
    {
        isProcessing = false;
        gameOver = false;

        // Choose a random starting point in the color gradient.
        colorIndex = Random.Range(0, blockColors.Length * 10);

// Give the base block the starting color.
        SpriteRenderer baseRenderer =
            baseBlock.GetComponent<SpriteRenderer>();

        if (baseRenderer != null)
            baseRenderer.color = GetGradientColor(colorIndex);

// Move to the next shade for the first moving block.
        colorIndex++;

        if (currentBlock != null)
        {
            // First block enters from the left.
            Vector3 position = currentBlock.transform.position;
            position.x = -3f;
            currentBlock.transform.position = position;

            currentBlock.SetMovingDirection(true);
            currentBlock.speed = GetBlockSpeed();

            currentBlock.manager = this;
            currentBlock.SetGameplayEnabled(true);
            
            SpriteRenderer currentRenderer =
                currentBlock.GetComponent<SpriteRenderer>();

            if (currentRenderer != null)
            {
                currentRenderer.color = GetGradientColor(colorIndex);
                colorIndex++;
            }
        }

        // The next block will enter from the right.
        nextSpawnFromLeft = false;

        Debug.Log("GAME STARTED!");
    }


    float GetBlockSpeed()
    {
        if (score <= 5) return 3.0f;
        if (score <= 10) return 3.4f;
        if (score <= 20) return 3.8f;
        if (score <= 30) return 4.2f;
        if (score <= 40) return 4.6f;
        if (score <= 50) return 5.0f;

        int cyclePosition = (score - 51) % 60;

        if (cyclePosition <= 9) return 3.2f;
        if (cyclePosition <= 19) return 3.6f;
        if (cyclePosition <= 29) return 4.0f;
        if (cyclePosition <= 39) return 4.4f;
        if (cyclePosition <= 49) return 4.8f;

        return 5.0f;
    }

    float GetBlockGravity()
    {
        if (score <= 5) return 3.0f;
        if (score <= 10) return 3.2f;
        if (score <= 20) return 3.4f;
        if (score <= 30) return 3.6f;
        if (score <= 40) return 3.8f;
        if (score <= 50) return 4.0f;

        int cyclePosition = (score - 51) % 60;

        if (cyclePosition <= 9) return 3.1f;
        if (cyclePosition <= 19) return 3.3f;
        if (cyclePosition <= 29) return 3.5f;
        if (cyclePosition <= 39) return 3.7f;
        if (cyclePosition <= 49) return 3.9f;

        return 4.0f;
    }

    Color GetGradientColor(int index)
    {
        int stepsPerColor = 5;

        float t = (float)index / stepsPerColor;

        int colorA = Mathf.FloorToInt(t) % blockColors.Length;
        int colorB = (colorA + 1) % blockColors.Length;

        float blend = t - Mathf.Floor(t);

        return Color.Lerp(
            blockColors[colorA],
            blockColors[colorB],
            blend
        );
    }
    
    
    void UpdateBackgroundTheme()
    {
        if (background == null)
            return;

        int targetStage = 0;

        if (score >= 100)
            targetStage = 4;
        else if (score >= 75)
            targetStage = 3;
        else if (score >= 50)
            targetStage = 2;
        else if (score >= 25)
            targetStage = 1;

        // Don't replay a transition if we're already in this stage.
        if (targetStage <= currentBackgroundStage)
            return;

        currentBackgroundStage = targetStage;

        Color bottom;
        Color middle;
        Color top;

        switch (targetStage)
        {
            case 1: // DAY
                bottom = new Color32(168, 217, 240, 255);
                middle = new Color32(185, 221, 244, 255);
                top = new Color32(217, 234, 248, 255);
                break;

            case 2: // SUNSET
                bottom = new Color32(246, 195, 165, 255);
                middle = new Color32(239, 182, 201, 255);
                top = new Color32(201, 185, 232, 255);
                break;

            case 3: // TWILIGHT
                bottom = new Color32(119, 125, 189, 255);
                middle = new Color32(137, 133, 197, 255);
                top = new Color32(178, 164, 216, 255);
                break;

            case 4: // NIGHT
                bottom = new Color32(53, 63, 101, 255);
                middle = new Color32(77, 86, 127, 255);
                top = new Color32(115, 116, 158, 255);
                break;

            default:
                return;
        }

        background.TransitionToTheme(
            bottom,
            middle,
            top,
            3f
        );
        
        if (cloudSpawner != null)
        {
            Color cloudColor = Color.white;

            switch (targetStage)
            {
                case 1: // Day
                    cloudColor = new Color32(244, 250, 255, 255);
                    break;

                case 2: // Sunset
                    cloudColor = new Color32(255, 240, 229, 255);
                    break;

                case 3: // Twilight
                    cloudColor = new Color32(220, 216, 242, 255);
                    break;

                case 4: // Night
                    cloudColor = new Color32(194, 199, 229, 255);
                    break;
            }

            cloudSpawner.TransitionCloudColor(cloudColor, 3f);
        }
        
        if (starfield != null)
        {
            starfield.SetStage(targetStage);
        }

        Debug.Log("BACKGROUND THEME: " +
                  new string[] { "Morning", "Day", "Sunset", "Twilight", "Night" }[targetStage]);
    }

}