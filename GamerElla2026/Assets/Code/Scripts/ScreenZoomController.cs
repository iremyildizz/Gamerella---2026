using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Put on the Player. Moves the player camera to a close-up view (a camera
// screen, a window to repair...) and back again. While zoomed, the player
// can't move or interact.
public class ScreenZoomController : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] PlayerCharacter3D movement;
    [SerializeField] PlayerInteractor interactor;
    [Tooltip("Seconds the zoom in / out takes")]
    [SerializeField] float zoomTime = 0.35f;
    [Tooltip("Hide the player's body while zoomed so it never blocks the view")]
    [SerializeField] bool hidePlayerWhileZoomed = true;

    public bool IsZoomed { get; private set; }

    Renderer[] bodyRenderers;
    public Camera PlayerCamera => playerCamera;

    Vector3 savedLocalPos;
    Quaternion savedLocalRot;
    Coroutine anim;
    int zoomedFrame;
    bool exitOnClick;
    InputAction interactAction;

    void Awake()
    {
        if (movement == null) movement = GetComponent<PlayerCharacter3D>();
        if (interactor == null) interactor = GetComponent<PlayerInteractor>();
        if (playerCamera == null && movement != null && movement.playerCamera != null)
            playerCamera = movement.playerCamera.GetComponent<Camera>();

        // Everything rendered on the player except what's under the camera
        // (e.g. held items or UI in front of the camera stay visible).
        bodyRenderers = GetComponentsInChildren<Renderer>(true);
        if (playerCamera != null)
            bodyRenderers = System.Array.FindAll(bodyRenderers,
                r => !r.transform.IsChildOf(playerCamera.transform));
    }

    void SetBodyVisible(bool visible)
    {
        if (!hidePlayerWhileZoomed || bodyRenderers == null) return;
        foreach (var r in bodyRenderers)
            if (r != null) r.enabled = visible;
    }

    void Start() => interactAction = InputSystem.actions.FindAction("Interact");

    // Security camera screens: fill the view with the screen quad.
    public void ZoomTo(ZoomableScreen target)
    {
        if (target.ScreenQuad == null) return;

        var quad = target.ScreenQuad;
        // A Quad is 1x1 in its local X/Y; its visible side faces -forward.
        float w = Mathf.Abs(quad.lossyScale.x) * target.Padding;
        float h = Mathf.Abs(quad.lossyScale.y) * target.Padding;
        float dist = FitDistance(w, h);

        Vector3 pos = quad.position - quad.forward * dist;
        Quaternion rot = Quaternion.LookRotation(quad.forward, quad.up);
        ZoomToPose(pos, rot, exitOnClick: true);
    }

    // Generic zoom. With exitOnClick = false the caller decides when to
    // ZoomOut (used by minigames that need the mouse clicks).
    public bool ZoomToPose(Vector3 pos, Quaternion rot, bool exitOnClick, Action onArrived = null)
    {
        if (IsZoomed || anim != null) return false;

        IsZoomed = true;
        zoomedFrame = Time.frameCount;
        this.exitOnClick = exitOnClick;
        SetPlayerControl(false);
        SetBodyVisible(false);

        var cam = playerCamera.transform;
        savedLocalPos = cam.localPosition;
        savedLocalRot = cam.localRotation;

        anim = StartCoroutine(Move(cam.position, cam.rotation, pos, rot, world: true, onDone: onArrived));
        return true;
    }

    public void ZoomOut(Action onDone = null)
    {
        if (!IsZoomed || anim != null) return;
        var cam = playerCamera.transform;
        anim = StartCoroutine(Move(cam.localPosition, cam.localRotation,
                                   savedLocalPos, savedLocalRot, world: false, onDone: () =>
        {
            IsZoomed = false;
            SetPlayerControl(true);
            SetBodyVisible(true);
            onDone?.Invoke();
        }));
    }

    // Distance at which a w x h area (meters) fills the camera view.
    public float FitDistance(float w, float h)
    {
        float vHalf = playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * playerCamera.aspect);
        return Mathf.Max(h * 0.5f / Mathf.Tan(vHalf), w * 0.5f / Mathf.Tan(hHalf));
    }

    void Update()
    {
        if (!IsZoomed || !exitOnClick || anim != null || Time.frameCount == zoomedFrame) return;

        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool interact = interactAction != null && interactAction.WasPressedThisFrame();
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (click || interact || escape) ZoomOut();
    }

    IEnumerator Move(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot,
                     bool world, Action onDone = null)
    {
        var cam = playerCamera.transform;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, zoomTime))
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            Apply(cam, Vector3.Lerp(fromPos, toPos, k), Quaternion.Slerp(fromRot, toRot, k), world);
            yield return null;
        }
        Apply(cam, toPos, toRot, world);
        anim = null;
        onDone?.Invoke();
    }

    static void Apply(Transform t, Vector3 p, Quaternion r, bool world)
    {
        if (world) t.SetPositionAndRotation(p, r);
        else { t.localPosition = p; t.localRotation = r; }
    }

    void SetPlayerControl(bool on)
    {
        if (movement != null) movement.enabled = on;
        if (interactor != null)
        {
            interactor.enabled = on;
            if (interactor.crosshairImage != null) interactor.crosshairImage.enabled = on;
        }
    }
}
