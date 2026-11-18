using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Default Shake (Used by UnityEvents)")]
    [SerializeField] private float defaultDuration = 0.45f;
    [SerializeField] private float defaultPositionMagnitude = 0.08f;
    [SerializeField] private float defaultRotationMagnitude = 1.6f;
    [SerializeField] private float frequency = 28f;
    [SerializeField] private float shakeDelay = 0.5f;

    private Vector3 lastPositionOffset;
    private Vector3 lastRotationOffset;
    private Coroutine activeShake;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void TriggerDefaultShake()
    {
        Shake(defaultDuration, defaultPositionMagnitude, defaultRotationMagnitude);
    }

    public void Shake(float duration, float positionMagnitude, float rotationMagnitude = 1.5f)
    {
        if (activeShake != null)
        {
            StopCoroutine(activeShake);
            ClearOffset();
        }
        activeShake = StartCoroutine(ShakeRoutine(duration, positionMagnitude, rotationMagnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float posMag, float rotMag)
    {
        float elapsed = 0f;
        float seedX = Random.Range(0f, 100f);
        float seedY = Random.Range(0f, 100f);
        float seedZ = Random.Range(0f, 100f);
         
        yield return new WaitForSeconds(shakeDelay);

        while (elapsed < duration)
        {
            ClearOffset();

            elapsed += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            damper *= damper; // Smooth quadratic falloff

            float t = elapsed * frequency;
            float nx = (Mathf.PerlinNoise(seedX, t) * 2f - 1f);
            float ny = (Mathf.PerlinNoise(seedY, t) * 2f - 1f);
            float nz = (Mathf.PerlinNoise(seedZ, t) * 2f - 1f);

            lastPositionOffset = new Vector3(nx, ny, 0f) * (posMag * damper);
            lastRotationOffset = new Vector3(ny, nx, nz) * (rotMag * damper);

            transform.localPosition += lastPositionOffset;
            transform.localEulerAngles += lastRotationOffset;

            yield return null;
        }

        ClearOffset();
        activeShake = null;
    }

    private void ClearOffset()
    {
        transform.localPosition -= lastPositionOffset;
        transform.localEulerAngles -= lastRotationOffset;
        lastPositionOffset = Vector3.zero;
        lastRotationOffset = Vector3.zero;
    }
}