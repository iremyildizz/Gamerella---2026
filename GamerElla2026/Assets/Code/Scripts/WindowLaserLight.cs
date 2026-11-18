using System.Collections;
using UnityEngine;

public class WindowLaserLight : MonoBehaviour
{
    [Header("Window Light")]
    [SerializeField] private Light targetLight;

    [Header("Idle / Ambient State")]
    [SerializeField] private Color idleColor = new Color(0.3f, 0.4f, 0.6f, 0.1f);
    [SerializeField] private float idleIntensity;
    [SerializeField] private float idleTemperature = 12000f;

    [Header("Laser Charge-Up")]
    [SerializeField] private float chargePeakIntensity = 12f;
    [SerializeField] private Color chargeColor = new Color(0.74f, 0.87f, 1f);
    [SerializeField] private float chargeTemperature = 3200f;

    [Header("Explosion Flash")]
    [SerializeField] private float explosionPeakIntensity = 60f;
    [SerializeField] private Color explosionColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private float explosionTemperature = 1500f;
    [SerializeField] private float explosionFlashDuration = 0.3f;
    [SerializeField] private float coolDownDuration = 3f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponentInChildren<Light>();

        if (targetLight != null)
        {
            targetLight.useColorTemperature = true;
            targetLight.intensity = idleIntensity;
            targetLight.color = idleColor;
            targetLight.colorTemperature = idleTemperature;
        }
    }
    public void TriggerLaserSequence(float delay)
    {
        if (targetLight == null) return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(LaserAndExplosionRoutine(delay));
    }

    private IEnumerator LaserAndExplosionRoutine(float chargeDuration)
    {
        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / chargeDuration);
            float flicker = Random.Range(0.85f, 1.15f);

            targetLight.intensity = Mathf.Lerp(idleIntensity, chargePeakIntensity, t * t) * flicker;
            targetLight.color = Color.Lerp(idleColor, chargeColor, t);
            targetLight.colorTemperature = Mathf.Lerp(idleTemperature, chargeTemperature, t);
            yield return null;
        }

        // 2. Explosion spike & cool down
        yield return StartCoroutine(ExplosionFlashRoutine());
        activeRoutine = null;
    }

    private IEnumerator ExplosionFlashRoutine()
    {
        targetLight.useColorTemperature = true;
        targetLight.intensity = explosionPeakIntensity;
        targetLight.color = explosionColor;
        targetLight.colorTemperature = explosionTemperature;

        yield return new WaitForSeconds(explosionFlashDuration);

        // Smoothly cool back down to the cold idle window light
        float elapsed = 0f;
        while (elapsed < coolDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / coolDownDuration);

            targetLight.intensity = Mathf.Lerp(explosionPeakIntensity, idleIntensity, t);
            targetLight.colorTemperature = Mathf.Lerp(explosionTemperature, idleTemperature, t);
            targetLight.color = Color.Lerp(explosionColor, idleColor, t);
            yield return null;
        }

        targetLight.intensity = idleIntensity;
        targetLight.colorTemperature = idleTemperature;
        targetLight.color = idleColor;
    }
}

