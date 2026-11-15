
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProceduralAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    public int sampleRate = 44100;

    private AudioSource audioSource;
    private AudioSource landingAudioSource;

    private AudioClip landingClip;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = masterVolume;

        // Separate source for landing sounds and pitch changes.
        landingAudioSource = gameObject.AddComponent<AudioSource>();
        landingAudioSource.playOnAwake = false;
        landingAudioSource.loop = false;
        landingAudioSource.spatialBlend = 0f;
        landingAudioSource.volume = 1f;

        landingClip = CreateLandingSound();
    }

    // NORMAL LANDING SOUND
    AudioClip CreateLandingSound()
    {
        float duration = 0.055f;
        int count = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[count];

        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;

            float frequency = Mathf.Lerp(480f, 340f, t / duration);
            phase += 2f * Mathf.PI * frequency / sampleRate;

            float body = Mathf.Sin(phase);
            float envelope = Mathf.Exp(-t * 85f);
            float texture = Mathf.Sin(2f * Mathf.PI * 1400f * t);

            float sound = body * 0.12f + texture * 0.025f;

            samples[i] = sound * envelope * 1f;
        }

        return CreateClip("Generated_Landing", samples);
    }
    
    // CREATE AUDIO CLIP
    AudioClip CreateClip(string clipName, float[] samples)
    {
        AudioClip clip = AudioClip.Create(
            clipName,
            samples.Length,
            1,
            sampleRate,
            false
        );

        clip.SetData(samples, 0);
        return clip;
    }

    // LANDING SOUND
    public void PlayLanding(int perfectStreak)
    {
        if (perfectStreak > 0)
        {
            perfectStreak = Mathf.Min(perfectStreak + 1, 10);

            float[] streakPitches =
            {
                1.1f, 1.2f, 1.3f, 1.4f, 1.5f,
                1.6f, 1.7f, 1.8f, 1.9f, 2.0f
            };

            float pitch = streakPitches[perfectStreak - 1];

            landingAudioSource.pitch = pitch;
        }
        else
        {
            landingAudioSource.pitch = 1f;
        }

        landingAudioSource.PlayOneShot(landingClip);
    }

    // GAME OVER SOUND

    public void PlayGameOver()
    {
        StopAllCoroutines();
        StartCoroutine(PlayGameOverSequence());
    }

    IEnumerator PlayGameOverSequence()
    {
        // First note: clear and slightly brighter.
        landingAudioSource.pitch = 1.3f;
        landingAudioSource.PlayOneShot(landingClip, 1f);

        yield return new WaitForSeconds(0.16f);

        // Second note: softer and slightly lower.
        landingAudioSource.pitch = 1.2f;
        landingAudioSource.PlayOneShot(landingClip, 0.8f);

        yield return new WaitForSeconds(0.085f);

        // Third note: soft ending.
        landingAudioSource.pitch = 0.8f;
        landingAudioSource.PlayOneShot(landingClip, 0.6f);

        yield return new WaitForSeconds(0.1f);

        // Restore normal pitch.
        landingAudioSource.pitch = 1f;
    }

    // MENU BASE BLOCK LANDING
    public void PlayMenuLanding()
    {
        landingAudioSource.pitch = 1f;
        landingAudioSource.PlayOneShot(landingClip);
    }
}
