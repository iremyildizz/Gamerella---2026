using System.Collections;
using UnityEngine;

public class BunkerOpeninAudio : MonoBehaviour
{
    [Header("3D Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Closeness Sounds")]
    [SerializeField] private AudioClip[] farSounds;
    [SerializeField] private bool loopFarSound = false;
    [SerializeField, Range(0f, 1f)] private float farVolume = 0.6f;

    [Space(6)]
    [SerializeField] private AudioClip[] closeSounds;
    [SerializeField] private bool loopCloseSound = true;
    [SerializeField, Range(0f, 1f)] private float closeVolume = 1.0f;

    [Header("Zombie Dying & Laser Zap Sounds")]
    [SerializeField] private AudioClip[] zombieDyingSounds;
    [SerializeField] private AudioClip[] laserZapOrExplosionSounds;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1.0f;

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
    private Coroutine randomSoundCoroutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    private void Start()
    {
        if (enableRandomSounds)
            randomSoundCoroutine = StartCoroutine(RandomZombieSoundsLoop());
    }

    // Connect to: SecurityScreen -> On Detected Far ()
    public void PlayFarSound()
    {
        isZombiePresent = true;
        PlayFromPool(farSounds, farVolume, loopFarSound);
    }

    // Connect to: SecurityScreen -> On Reached Close ()
    public void PlayCloseSound()
    {
        isZombiePresent = true;
        PlayFromPool(closeSounds, closeVolume, loopCloseSound);
    }

    // Connect to: SecurityScreen -> On Zombie Died ()
    // Plays BOTH a zombie death groan AND the laser zap/explosion sound together!
    public void PlayZombieDyingSound()
    {
        isZombiePresent = false;
        StopLoopingSound();
        PlayOneShotFromPool(zombieDyingSounds, deathVolume);
        PlayOneShotFromPool(laserZapOrExplosionSounds, deathVolume);
    }

    // Optional: Play ONLY the laser zap / explosion sound
    public void PlayLaserZapOnly()
    {
        PlayOneShotFromPool(laserZapOrExplosionSounds, deathVolume);
    }

    // Connect to: SecurityScreen -> On Cleared ()
    public void StopAllSounds()
    {
        isZombiePresent = false;
        if (audioSource == null) return;
        audioSource.loop = false;
        audioSource.Stop();
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

    private void StopLoopingSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.loop = false;
            audioSource.Stop();
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