using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SecurityScreen : MonoBehaviour
{
    public enum State { Empty, Far, Close }

    [SerializeField] Renderer screen;
    [SerializeField] Material emptyMat, farMat, closeMat;
    [Tooltip("Leave empty to use the child object named \"RecDot\" of this camera")]
    [SerializeField] GameObject recDot;
    [SerializeField] float blinkInterval = 0.5f;

    public UnityEvent onReachedClose;
    public UnityEvent onCleared;

    public State Current { get; private set; } = State.Empty;
    public bool IsFree => Current == State.Empty;

    SecurityCameraManager manager;
    Coroutine routine;

    void Awake()
    {
        // Each camera must blink its own dot. If the field is empty or points to
        // a dot on another camera, use this camera's child named "RecDot".
        if (recDot == null || !recDot.transform.IsChildOf(transform))
        {
            var child = FindChild(transform, "RecDot");
            if (child != null) recDot = child.gameObject;
            else Debug.LogWarning($"{name}: no RecDot child found", this);
        }
        SetState(State.Empty);
    }

    public void Activate(SecurityCameraManager owner)
    {
        manager = owner;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    public void Clear()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        SetState(State.Empty);
        onCleared.Invoke();
    }

    IEnumerator Run()
    {
        SetState(State.Far);
        yield return Hold(() => manager.CloseDelay);

        SetState(State.Close);
        onReachedClose.Invoke();
        yield return Hold(() => manager.CloseDuration);

        routine = null;
        SetState(State.Empty);
        onCleared.Invoke();
    }

    // Waits while blinking. Reads the duration every frame so difficulty
    // changes also affect silhouettes that are already on screen.
    IEnumerator Hold(System.Func<float> duration)
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime / Mathf.Max(0.01f, duration());
            BlinkDot();
            yield return null;
        }
    }

    void BlinkDot()
    {
        if (recDot != null)
            recDot.SetActive(Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0);
    }

    void SetState(State s)
    {
        Current = s;
        screen.sharedMaterial = s switch
        {
            State.Far => farMat,
            State.Close => closeMat,
            _ => emptyMat,
        };
        if (s == State.Empty && recDot != null) recDot.SetActive(false);
    }

    static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t != parent && t.name.StartsWith(childName)) return t;
        return null;
    }

    [ContextMenu("Preview/Empty")] void PreviewEmpty() => PreviewState(State.Empty);
    [ContextMenu("Preview/Far")]   void PreviewFar()   => PreviewState(State.Far);
    [ContextMenu("Preview/Close")] void PreviewClose() => PreviewState(State.Close);

    void PreviewState(State s)
    {
        SetState(s);
        if (recDot != null) recDot.SetActive(s != State.Empty);
    }
}
