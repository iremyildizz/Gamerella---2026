using System.Collections;
using TMPro;
using UnityEngine;

public class MenuFeedBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    [Header("Feeds")]
    [Tooltip("Normal camera views (e.g. c1, c2, c3, c4)")]
    [SerializeField] private Material[] feeds;
    [Tooltip("Views with a zombie in frame, shown now and then (e.g. c1.1, c3.2)")]
    [SerializeField] private Material[] rareFeeds;
    [SerializeField, Range(0f, 1f)] private float rareChance = 0.2f;

    [Header("Timing")]
    [SerializeField] private Vector2 holdSeconds = new Vector2(5f, 9f);
    [SerializeField, Min(0.05f)] private float staticDuration = 0.45f;

    [Header("Overlay (optional)")]
    [SerializeField] private TMP_Text camLabel;

    static readonly int StaticBurstId = Shader.PropertyToID("_StaticBurst");
    static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

    private MeshRenderer quad;
    private MaterialPropertyBlock block;
    private float burst;
    private int camNumber = 1;
    private int lastIndex = -1;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        block = new MaterialPropertyBlock();

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "FeedQuad";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(targetCamera.transform, false);
        quad = go.GetComponent<MeshRenderer>();
        quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quad.receiveShadows = false;
    }

    private void Start()
    {
        if (feeds == null || feeds.Length == 0)
        {
            Debug.LogWarning("MenuFeedBackground: no feed materials assigned", this);
            quad.gameObject.SetActive(false);
            enabled = false;
            return;
        }
        Show(PickNext());
        StartCoroutine(Cycle());
    }

    private void LateUpdate()
    {
        FitToScreen();
        UpdateLabel();
    }

    private IEnumerator Cycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(holdSeconds.x, holdSeconds.y));

            float half = staticDuration * 0.35f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / half) { SetBurst(t); yield return null; }
            SetBurst(1f);
            Show(PickNext());
            float rest = staticDuration - half;
            for (float t = 0f; t < 1f; t += Time.deltaTime / rest) { SetBurst(1f - t * t); yield return null; }
            SetBurst(0f);
        }
    }

    private Material PickNext()
    {
        bool rare = rareFeeds != null && rareFeeds.Length > 0 && Random.value < rareChance;
        Material[] pool = rare ? rareFeeds : feeds;
        int i = Random.Range(0, pool.Length);
        if (!rare && pool.Length > 1 && i == lastIndex) i = (i + 1) % pool.Length;
        if (!rare) lastIndex = i;
        return pool[i];
    }

    private void Show(Material m)
    {
        quad.sharedMaterial = m;
        camNumber = ParseCamNumber(m.name);
        FitToScreen();
    }

    private void SetBurst(float amount)
    {
        burst = Mathf.Clamp01(amount);
        Apply();
    }

    private void FitToScreen()
    {
        if (quad == null || targetCamera == null) return;

        float dist = targetCamera.nearClipPlane + 0.5f;
        float h = 2f * dist * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float screenAspect = targetCamera.aspect;
        var t = quad.transform;
        t.localPosition = new Vector3(0f, 0f, dist);
        t.localRotation = Quaternion.identity;
        t.localScale = new Vector3(h * screenAspect, h, 1f);
        Apply();
    }

    private void Apply()
    {
        var tex = quad.sharedMaterial != null ? quad.sharedMaterial.mainTexture : null;
        Vector4 st = new Vector4(1f, 1f, 0f, 0f);
        if (tex != null && tex.height > 0)
        {
            float imgAspect = (float)tex.width / tex.height;
            float screenAspect = targetCamera.aspect;
            if (screenAspect > imgAspect)
            {
                float sy = imgAspect / screenAspect;
                st = new Vector4(1f, sy, 0f, (1f - sy) * 0.5f);
            }
            else
            {
                float sx = screenAspect / imgAspect;
                st = new Vector4(sx, 1f, (1f - sx) * 0.5f, 0f);
            }
        }

        quad.GetPropertyBlock(block);
        block.SetFloat(StaticBurstId, burst);
        block.SetVector(BaseMapStId, st);
        quad.SetPropertyBlock(block);
    }

    private void UpdateLabel()
    {
        if (camLabel == null) return;
        bool dotOn = Mathf.FloorToInt(Time.unscaledTime / 0.5f) % 2 == 0;
        string dot = dotOn ? "<color=#E02020>●</color>" : "<color=#00000000>●</color>";
        camLabel.text = $"{dot} REC   CAM {camNumber:00}\n{System.DateTime.Now:dd.MM.yyyy  HH:mm:ss}";
    }

    private static int ParseCamNumber(string matName)
    {
        foreach (char c in matName)
            if (char.IsDigit(c)) return c - '0';
        return 1;
    }
}
