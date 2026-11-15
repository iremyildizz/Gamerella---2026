using System.Collections;
using System.Linq;
using UnityEngine;

public class SecurityCameraManager : MonoBehaviour
{
    [SerializeField] SecurityScreen[] screens;

    [Header("Light Flicker on Detection")]
    [SerializeField] private Light[] roomLights;
    [SerializeField] private Renderer[] emissiveLampRenderers;
    [SerializeField] private float flickerIntervalMin = 0.04f;
    [SerializeField] private float flickerIntervalMax = 0.14f;
    [SerializeField] private float dimIntensityMultiplier = 0.15f;
    [SerializeField] private float spikeIntensityMultiplier = 1.25f;

    [Header("Light Flicker Audio")]
    [SerializeField] private AudioSource flickerAudioSource;
    [SerializeField] private AudioClip[] flickerSounds; // Short electric buzz/spark clips
    [SerializeField] private Vector2 flickerPitchRange = new Vector2(0.9f, 1.15f);
    [SerializeField, Range(0f, 1f)] private float flickerVolume = 0.6f;

    [Header("Difficulty (change at runtime)")]
    [Tooltip("Shortest wait between two new silhouettes (seconds)")]
    public float spawnIntervalMin = 10f;
    [Tooltip("Longest wait between two new silhouettes (seconds)")]
    public float spawnIntervalMax = 25f;
    [Tooltip("Max silhouettes visible at once")]
    [Range(1, 4)] public int maxActive = 1;
    [Tooltip("Seconds a silhouette stays far before getting closer")]
    public float closeDelay = 15f;
    [Tooltip("Seconds a silhouette stays close before disappearing")]
    public float closeDuration = 5f;

    [Header("Automatic ramp (optional)")]
    public bool autoRamp = true;
    [Tooltip("Seconds between two difficulty steps")]
    public float rampEvery = 60f;
    [Tooltip("Spawn intervals and close delay are multiplied by this each step (0.85 = 15% faster)")]
    [Range(0.5f, 1f)] public float rampFactor = 0.85f;
    [Tooltip("Max Active goes up by one every N steps")]
    [Min(1)] public int maxActiveEverySteps = 2;
    public float minSpawnInterval = 3f, minCloseDelay = 4f;

    public float CloseDelay => closeDelay;
    public float CloseDuration => closeDuration;

    private float[] baseIntensities;
    private Color[] baseEmissionColors;
    private bool isFlickering = false;
    private int lastFlickerSoundIndex = -1;

    private void Awake()
    {
        if (roomLights != null)
        {
            baseIntensities = new float[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
            {
                if (roomLights[i] != null)
                    baseIntensities[i] = roomLights[i].intensity;
            }
        }

        if (emissiveLampRenderers != null)
        {
            baseEmissionColors = new Color[emissiveLampRenderers.Length];
            for (int i = 0; i < emissiveLampRenderers.Length; i++)
            {
                if (emissiveLampRenderers[i] != null && emissiveLampRenderers[i].material.HasProperty("_EmissionColor"))
                    baseEmissionColors[i] = emissiveLampRenderers[i].material.GetColor("_EmissionColor");
            }
        }
    }

    IEnumerator Start()
    {
        if (autoRamp) StartCoroutine(Ramp());
        StartCoroutine(MonitorMovementForLights());

        while (true)
        {
            float min = Mathf.Min(spawnIntervalMin, spawnIntervalMax);
            float max = Mathf.Max(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(Random.Range(min, max));
            TrySpawn();
        }
    }

    void TrySpawn()
    {
        int active = screens.Count(s => !s.IsFree);
        if (active >= maxActive) return;

        var free = screens.Where(s => s.IsFree).ToArray();
        if (free.Length == 0) return;
        free[Random.Range(0, free.Length)].Activate(this);
    }

    IEnumerator MonitorMovementForLights()
    {
        while (true)
        {
            bool movementDetected = screens != null && screens.Any(s => s != null && !s.IsFree);

            if (movementDetected)
            {
                isFlickering = true;
                bool isDimPulse = Random.value < 0.65f;
                float mult = isDimPulse
                    ? Random.Range(dimIntensityMultiplier, 0.5f)
                    : Random.Range(0.85f, spikeIntensityMultiplier);

                ApplyLightMultiplier(mult);

                // Play a crackle/buzz when the light dips or surges sharply
                if (isDimPulse || mult > 1.1f)
                    PlayFlickerSound();

                yield return new WaitForSeconds(Random.Range(flickerIntervalMin, flickerIntervalMax));
            }
            else
            {
                if (isFlickering)
                {
                    ApplyLightMultiplier(1f);
                    isFlickering = false;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void PlayFlickerSound()
    {
        if (flickerAudioSource == null || flickerSounds == null || flickerSounds.Length == 0)
            return;

        int index = Random.Range(0, flickerSounds.Length);
        if (flickerSounds.Length > 1 && index == lastFlickerSoundIndex)
            index = (index + 1) % flickerSounds.Length;

        lastFlickerSoundIndex = index;

        AudioClip clip = flickerSounds[index];
        if (clip != null)
        {
            flickerAudioSource.pitch = Random.Range(flickerPitchRange.x, flickerPitchRange.y);
            flickerAudioSource.PlayOneShot(clip, flickerVolume);
        }
    }

    private void ApplyLightMultiplier(float multiplier)
    {
        if (roomLights != null)
        {
            for (int i = 0; i < roomLights.Length; i++)
            {
                if (roomLights[i] != null)
                    roomLights[i].intensity = baseIntensities[i] * multiplier;
            }
        }

        if (emissiveLampRenderers != null)
        {
            for (int i = 0; i < emissiveLampRenderers.Length; i++)
            {
                if (emissiveLampRenderers[i] != null && emissiveLampRenderers[i].material.HasProperty("_EmissionColor"))
                    emissiveLampRenderers[i].material.SetColor("_EmissionColor", baseEmissionColors[i] * multiplier);
            }
        }
    }

    IEnumerator Ramp()
    {
        int step = 0;
        while (true)
        {
            yield return new WaitForSeconds(rampEvery);
            step++;
            spawnIntervalMin = Mathf.Max(minSpawnInterval, spawnIntervalMin * rampFactor);
            spawnIntervalMax = Mathf.Max(minSpawnInterval, spawnIntervalMax * rampFactor);
            closeDelay = Mathf.Max(minCloseDelay, closeDelay * rampFactor);
            if (step % maxActiveEverySteps == 0)
                maxActive = Mathf.Min(screens.Length, maxActive + 1);
        }
    }

    public void SetDifficulty(float spawnMin, float spawnMax, int max, float close)
    {
        spawnIntervalMin = spawnMin; spawnIntervalMax = spawnMax;
        maxActive = max; closeDelay = close;
    }
}