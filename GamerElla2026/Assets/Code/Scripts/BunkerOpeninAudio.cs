using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BunkerOpeninAudio : MonoBehaviour
{
    [Header("3D Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Window / Door Light")]
    [SerializeField] private WindowLaserLight windowLaserLight;

    [Header("Closeness Sounds")]
    [SerializeField] private AudioClip[] farSounds;
    [SerializeField] private bool loopFarSound = false;
    [SerializeField, Range(0f, 1f)] private float farVolume;

    [Space(6)]
    [SerializeField] private AudioClip[] closeSounds;
    [SerializeField] private bool loopCloseSound = true;
    [SerializeField, Range(0f, 1f)] private float closeVolume;

    [Header("Zombie Dying & Laser Zap Sounds")]
    [SerializeField] private AudioClip[] zombieDyingSounds;
    [SerializeField, Range(0f, 2f)] private float deathVolume;
    [SerializeField] private float delaySeconds;
    [SerializeField] private float dyingSoundDelay;

    [Header("Switch & Laser Sounds")]
    [SerializeField] private AudioClip switchSound;
    [SerializeField, Range(0f, 2f)] private float switchVolume;
    [SerializeField] private AudioClip[] laserZapOrExplosionSounds;
    [SerializeField] private float laserSoundDelay;

    [Header("Random Ambient Zombie Sounds")]
    [SerializeField] private AudioClip[] randomZombieSounds;
    [SerializeField] private bool enableRandomSounds = true;
    [SerializeField] private bool onlyWhenZombiePresent = true;
    [SerializeField] private float randomIntervalMin = 4f;
    [SerializeField] private float randomIntervalMax = 10f;
    [SerializeField, Range(0f, 1f)] private float randomSoundVolume = 0.55f;

    [Header("Variation")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.93f, 1.07f);

    private bool isZombiePresent = false;
    private Coroutine delayedStopCoroutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (windowLaserLight == null)
            windowLaserLight = GetComponent<WindowLaserLight>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    private void Start()
    {
        if (enableRandomSounds) StartCoroutine(RandomZombieSoundsLoop());
    }

    // Connect to: SecurityScreen -> On Detected Far ()
    public void PlayFarSound()
    {
        CancelDelayedStop();
        isZombiePresent = true;
        PlayFromPool(farSounds, farVolume, loopFarSound);
    }

    // Connect to: SecurityScreen -> On Reached Close ()
    public void PlayCloseSound()
    {
        CancelDelayedStop();
        isZombiePresent = true;
        PlayFromPool(closeSounds, closeVolume, loopCloseSound);
    }

    public void PlayOnlyZombieDyingSound()
    {
        isZombiePresent = false;
        StopLoopingSound();
        StartCoroutine(PlayZombieDyingDelayedRoutine());
    }

    private IEnumerator PlayZombieDyingDelayedRoutine()
    {
        if (dyingSoundDelay > 0f)
            yield return new WaitForSeconds(dyingSoundDelay);

        PlayOneShotFromPool(zombieDyingSounds, deathVolume);
    }

    public void PlaySwitchUsedSound()
    {
        StartCoroutine(PlaySwitchAndZapRoutine());
    }

    private IEnumerator PlaySwitchAndZapRoutine()
    {
        if (windowLaserLight != null)
            windowLaserLight.TriggerLaserSequence(laserSoundDelay);

        if (audioSource != null && switchSound != null)
            audioSource.PlayOneShot(switchSound, switchVolume);

        yield return new WaitForSeconds(laserSoundDelay);
        PlayOneShotFromPool(laserZapOrExplosionSounds, switchVolume);
    }

    public void StopAllSounds()
    {
        isZombiePresent = false;
        StopLoopingSound();
    }

    public void StopAllSoundsAfterDelay()
    {
        isZombiePresent = false;
        CancelDelayedStop();
        delayedStopCoroutine = StartCoroutine(HardStopAfterDelay(delaySeconds));
    }

    private IEnumerator HardStopAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.Stop();
            audioSource.clip = null;
        }
        delayedStopCoroutine = null;
    }

    private void CancelDelayedStop()
    {
        if (delayedStopCoroutine != null)
        {
            StopCoroutine(delayedStopCoroutine);
            delayedStopCoroutine = null;
        }
    }

    private void StopLoopingSound()
    {
        if (audioSource == null) return;

        audioSource.loop = false;
        if (audioSource.clip != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    private IEnumerator RandomZombieSoundsLoop()
    {
        while (true)
        {
            float wait = Random.Range(
                Mathf.Min(randomIntervalMin, randomIntervalMax),
                Mathf.Max(randomIntervalMin, randomIntervalMax)
            );
            yield return new WaitForSeconds(wait);

            if (!enableRandomSounds) continue;
            if (onlyWhenZombiePresent && !isZombiePresent) continue;

            PlayOneShotFromPool(randomZombieSounds, randomSoundVolume);
        }
    }

    private void PlayFromPool(AudioClip[] pool, float volume, bool shouldLoop)
    {
        if (audioSource == null || pool == null || pool.Length == 0) return;

        AudioClip clip = pool[Random.Range(0, pool.Length)];
        if (clip == null) return;

        audioSource.Stop();
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.volume = volume;
        audioSource.loop = shouldLoop;
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void PlayOneShotFromPool(AudioClip[] pool, float volume)
    {
        if (audioSource == null || pool == null || pool.Length == 0) return;

        AudioClip clip = pool[Random.Range(0, pool.Length)];
        if (clip == null) return;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume);
    }
}