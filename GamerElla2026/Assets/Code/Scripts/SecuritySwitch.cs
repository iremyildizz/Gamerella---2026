using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Put on a switch (with a collider, on the Interactable layer).
// Clicking it fires the shared laser at the linked camera screen.
public class SecuritySwitch : MonoBehaviour, IInteractable
{
    [Tooltip("The camera this switch is wired to")]
    [SerializeField] SecurityScreen target;
    [Tooltip("Leave empty to use the LaserSystem in the scene")]
    [SerializeField] LaserSystem laser;

    [Header("Optional lever animation")]
    [Tooltip("A child part to rotate when the switch is used (leave empty for none)")]
    [SerializeField] Transform lever;
    [SerializeField] Vector3 leverRotation = new Vector3(0f, 0f, -45f);
    [SerializeField] float leverTime = 0.15f;

    [Header("Events (sounds...)")]
    public UnityEvent onUsed;     // laser fired from this switch
    public UnityEvent onDenied;   // laser still cooling down

    Quaternion leverRest;
    Coroutine leverRoutine;

    public SecurityScreen Target => target;

    void Awake()
    {
        if (laser == null) laser = FindFirstObjectByType<LaserSystem>();
        if (laser == null) Debug.LogWarning("No LaserSystem in the scene", this);
        if (target == null) Debug.LogWarning($"{name}: no camera assigned", this);
        if (lever != null) leverRest = lever.localRotation;
    }

    public void Interact()
    {
        if (laser == null || target == null) return;

        if (laser.Fire(target))
        {
            onUsed.Invoke();
            if (lever != null)
            {
                if (leverRoutine != null) StopCoroutine(leverRoutine);
                leverRoutine = StartCoroutine(FlipLever());
            }
        }
        else if (!laser.IsReady)
        {
            onDenied.Invoke();
        }
    }

    IEnumerator FlipLever()
    {
        Quaternion down = leverRest * Quaternion.Euler(leverRotation);
        yield return Rotate(leverRest, down);
        yield return new WaitForSeconds(0.2f);
        yield return Rotate(down, leverRest);
        leverRoutine = null;
    }

    IEnumerator Rotate(Quaternion from, Quaternion to)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, leverTime))
        {
            lever.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
        lever.localRotation = to;
    }
}
