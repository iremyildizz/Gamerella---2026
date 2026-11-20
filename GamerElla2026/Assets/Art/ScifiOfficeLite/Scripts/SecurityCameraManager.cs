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
    [SerializeField] private AudioSource[] flickerAudioSources;
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
    private bool[] isFlickering;
    private int lastFlickerSoundIndex = -1;

    private void Awake()
    {
        if (roomLights != null)
        {
            baseIntensities = new float[roomLights.Length];
            isFlickering = new bool[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
            {
                if (roomLights[i] != null)
                    baseIntensities[i] = roomLights[i].intensity;
                isFlickering[i] = false;
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

    private int GetLightIndexForScreen(int screenIndex)
    {
        // Camera 1 (0) & Camera 2 (1) -> Light 1 (0)
        // Camera 3 (2) -> Light 2 (1)
        // Camera 4 (3) -> Light 3 (2)
        if (screenIndex <= 1) return 0;
        return screenIndex - 1;
    }

    IEnumerator MonitorMovementForLights()
    {
        while (true)
        {
            if (screens == null || screens.Length == 0)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // Track which of the 3 lights (0, 1, 2) have movement on their mapped camera(s)
            bool[] lightHasMovement = new bool[3];
            for (int i = 0; i < screens.Length; i++)
            {
                bool active = screens[i] != null && !screens[i].IsFree;
                if (isFlickering != null && i < isFlickering.Length)
                    isFlickering[i] = active;

                if (active)
                {
                    int lightIdx = GetLightIndexForScreen(i);
                    if (lightIdx >= 0 && lightIdx < lightHasMovement.Length)
                        lightHasMovement[lightIdx] = true;
                }
            }

            bool anyLightActive = false;

            for (int lightIdx = 0; lightIdx < lightHasMovement.Length; lightIdx++)
            {
                if (lightHasMovement[lightIdx])
                {
                    anyLightActive = true;
                    bool isDimPulse = Random.value < 0.65f;
                    float mult = isDimPulse
                        ? Random.Range(dimIntensityMultiplier, 0.5f)
                        : Random.Range(0.85f, spikeIntensityMultiplier);

                    ApplyLightMultiplier(lightIdx, mult);

                    if (isDimPulse || mult > 1.1f)
                        PlayFlickerSound(lightIdx);
                }
                else
                {
                    // Restore this specific light to normal if its mapped camera(s) are clear
                    ApplyLightMultiplier(lightIdx, 1f);
                }
            }

            yield return new WaitForSeconds(
                anyLightActive
                    ? Random.Range(flickerIntervalMin, flickerIntervalMax)
                    : 0.1f
            );
        }
    }

    private void PlayFlickerSound(int index)
    {
        if (flickerAudioSources == null || flickerAudioSources.Length == 0 || flickerSounds == null || flickerSounds.Length == 0)
            return;

        AudioSource source = flickerAudioSources[index % flickerAudioSources.Length];
        if (source == null) return;

        int clipIndex = Random.Range(0, flickerSounds.Length);
        if (flickerSounds.Length > 1 && clipIndex == lastFlickerSoundIndex)
            clipIndex = (clipIndex + 1) % flickerSounds.Length;

        lastFlickerSoundIndex = clipIndex;

        AudioClip clip = flickerSounds[clipIndex];
        if (clip != null)
        {
            source.pitch = Random.Range(flickerPitchRange.x, flickerPitchRange.y);
            source.PlayOneShot(clip, flickerVolume);
        }
    }

    private void ApplyLightMultiplier(int lightIndex, float multiplier)
    {
        if (roomLights != null && lightIndex >= 0 && lightIndex < roomLights.Length && roomLights[lightIndex] != null)
        {
            roomLights[lightIndex].intensity = baseIntensities[lightIndex] * multiplier;
        }

        if (emissiveLampRenderers != null && lightIndex >= 0 && lightIndex < emissiveLampRenderers.Length && emissiveLampRenderers[lightIndex] != null)
        {
            if (emissiveLampRenderers[lightIndex].material.HasProperty("_EmissionColor"))
                emissiveLampRenderers[lightIndex].material.SetColor("_EmissionColor", baseEmissionColors[lightIndex] * multiplier);
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