using System.Collections;
using System.Linq;
using UnityEngine;

public class SecurityCameraManager : MonoBehaviour
{
    [SerializeField] SecurityScreen[] screens;

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

    IEnumerator Start()
    {
        if (autoRamp) StartCoroutine(Ramp());

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

    IEnumerator Ramp()
    {
        int step = 0;
        while (true)
        {
            yield return new WaitForSeconds(rampEvery);
            step++;
            spawnIntervalMin = Mathf.Max(minSpawnInterval, spawnIntervalMin * rampFactor);
            spawnIntervalMax = Mathf.Max(minSpawnInterval, spawnIntervalMax * rampFactor);
            closeDelay       = Mathf.Max(minCloseDelay,    closeDelay       * rampFactor);
            if (step % maxActiveEverySteps == 0)
                maxActive = Mathf.Min(screens.Length, maxActive + 1);
        }
    }

    // Call from other scripts, e.g. when a level or night changes
    public void SetDifficulty(float spawnMin, float spawnMax, int max, float close)
    {
        spawnIntervalMin = spawnMin; spawnIntervalMax = spawnMax;
        maxActive = max; closeDelay = close;
    }
}
