using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Put on the Player. Moves the player camera in front of a ZoomableScreen so
// the screen fills the view, and back again on the next click.
public class ScreenZoomController : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] PlayerCharacter3D movement;
    [SerializeField] PlayerInteractor interactor;
    [Tooltip("Seconds the zoom in / out takes")]
    [SerializeField] float zoomTime = 0.35f;

    public bool IsZoomed { get; private set; }

    Vector3 savedLocalPos;
    Quaternion savedLocalRot;
    Coroutine anim;
    int zoomedFrame;
    InputAction interactAction;

    void Awake()
    {
        if (movement == null) movement = GetComponent<PlayerCharacter3D>();
        if (interactor == null) interactor = GetComponent<PlayerInteractor>();
        if (playerCamera == null && movement != null && movement.playerCamera != null)
            playerCamera = movement.playerCamera.GetComponent<Camera>();
    }

    void Start() => interactAction = InputSystem.actions.FindAction("Interact");

    public void ZoomTo(ZoomableScreen target)
    {
        if (IsZoomed || anim != null || target.ScreenQuad == null) return;

        IsZoomed = true;
        zoomedFrame = Time.frameCount;
        SetPlayerControl(false);

        var cam = playerCamera.transform;
        savedLocalPos = cam.localPosition;
        savedLocalRot = cam.localRotation;

        var quad = target.ScreenQuad;
        // A Quad is 1x1 in its local X/Y; its visible side faces -forward.
        float w = Mathf.Abs(quad.lossyScale.x) * target.Padding;
        float h = Mathf.Abs(quad.lossyScale.y) * target.Padding;
        float vHalf = playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * playerCamera.aspect);
        float dist = Mathf.Max(h * 0.5f / Mathf.Tan(vHalf), w * 0.5f / Mathf.Tan(hHalf));

        Vector3 pos = quad.position - quad.forward * dist;
        Quaternion rot = Quaternion.LookRotation(quad.forward, quad.up);

        anim = StartCoroutine(Move(cam.position, cam.rotation, pos, rot, world: true));
    }

    public void ZoomOut()
    {
        if (!IsZoomed || anim != null) return;
        var cam = playerCamera.transform;
        anim = StartCoroutine(Move(cam.localPosition, cam.localRotation,
                                   savedLocalPos, savedLocalRot, world: false, onDone: () =>
        {
            IsZoomed = false;
            SetPlayerControl(true);
        }));
    }

    void Update()
    {
        if (!IsZoomed || anim != null || Time.frameCount == zoomedFrame) return;

        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool interact = interactAction != null && interactAction.WasPressedThisFrame();
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (click || interact || escape) ZoomOut();
    }

    IEnumerator Move(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot,
                     bool world, System.Action onDone = null)
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
