using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class MainMenuSceneBuilder
{
    const string ScenePath = "Assets/Level/Scenes/MainMenu.unity";
    const string FontPath = "Assets/Art/Fonts/PermanentMarker/PermanentMarker SDF.asset";
    const string MusicPath = "Assets/Audio/Music/Dark Background Ambience.mp3";
    const string FeedFolder = "Assets/Art/camera/Surveillance images";

    static readonly Color BgColor = new Color(0.04f, 0.04f, 0.05f, 1f);
    static readonly Color Accent = new Color(0.78f, 0.12f, 0.10f, 1f);
    static readonly Color TextColor = new Color(0.93f, 0.91f, 0.88f, 1f);

    [MenuItem("Tools/GamerElla/Create Main Menu Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Main Menu", $"{ScenePath} already exists. Overwrite it?", "Overwrite", "Cancel"))
            return;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        camGo.AddComponent<AudioListener>();

        // Event system (new Input System)
        var esGo = new GameObject("EventSystem");
        var es = esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        // Canvas
        var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Darkening layer over the camera feed so the menu stays readable
        var bg = NewUI("Background", canvasGo.transform);
        Stretch(bg);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = new Color(BgColor.r, BgColor.g, BgColor.b, 0.55f);
        bgImg.raycastTarget = false;

        // REC / CAM overlay in the corner, like the in-game monitors
        var camLabel = Text("CamLabel", canvasGo.transform, "", 30, null, new Color(0.85f, 1f, 0.88f, 0.8f));
        camLabel.alignment = TextAlignmentOptions.TopLeft;
        camLabel.rectTransform.anchorMin = camLabel.rectTransform.anchorMax = new Vector2(0, 1);
        camLabel.rectTransform.pivot = new Vector2(0, 1);
        camLabel.rectTransform.anchoredPosition = new Vector2(40, -30);
        camLabel.rectTransform.sizeDelta = new Vector2(600, 100);

        // Security camera feed behind everything
        var feed = new GameObject("CameraFeedBackground").AddComponent<MenuFeedBackground>();
        var feedSo = new SerializedObject(feed);
        feedSo.FindProperty("targetCamera").objectReferenceValue = cam;
        feedSo.FindProperty("camLabel").objectReferenceValue = camLabel;
        SetMaterials(feedSo.FindProperty("feeds"), "c1", "c2", "c3", "c4");
        SetMaterials(feedSo.FindProperty("rareFeeds"), "c1.1", "c2.1", "c3.1", "c4.1");
        feedSo.ApplyModifiedPropertiesWithoutUndo();

        // Red accent stripe behind the title
        var stripe = NewUI("AccentStripe", canvasGo.transform);
        stripe.anchorMin = new Vector2(0, 0.62f);
        stripe.anchorMax = new Vector2(1, 0.64f);
        stripe.offsetMin = stripe.offsetMax = Vector2.zero;
        stripe.gameObject.AddComponent<Image>().color = new Color(Accent.r, Accent.g, Accent.b, 0.35f);

        // ---------- Main panel ----------
        var mainPanel = NewUI("MainPanel", canvasGo.transform);
        Stretch(mainPanel);

        var title = Text("Title", mainPanel, "GAMERELLA", 150, font, TextColor);
        Place(title, new Vector2(0.5f, 0.75f), new Vector2(1600, 220));
        title.fontStyle = FontStyles.Normal;

        var subtitle = Text("Subtitle", mainPanel, "Survive. Hack. Get out.", 40, font, Accent);
        Place(subtitle, new Vector2(0.5f, 0.6f), new Vector2(1200, 60));

        var column = NewUI("Buttons", mainPanel);
        column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.3f);
        column.sizeDelta = new Vector2(460, 330);
        var vlg = column.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 24;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = vlg.childControlWidth = true;
        vlg.childForceExpandHeight = vlg.childForceExpandWidth = true;

        var playBtn = MakeButton("PlayButton", column, "PLAY", font);
        var controlsBtn = MakeButton("ControlsButton", column, "CONTROLS", font);
        var quitBtn = MakeButton("QuitButton", column, "QUIT", font);

        var version = Text("Version", mainPanel, "v" + Application.version, 24, null, new Color(1, 1, 1, 0.35f));
        version.alignment = TextAlignmentOptions.BottomRight;
        version.rectTransform.anchorMin = version.rectTransform.anchorMax = new Vector2(1, 0);
        version.rectTransform.pivot = new Vector2(1, 0);
        version.rectTransform.anchoredPosition = new Vector2(-30, 20);
        version.rectTransform.sizeDelta = new Vector2(300, 40);

        // ---------- Controls panel ----------
        var controlsPanel = NewUI("ControlsPanel", canvasGo.transform);
        Stretch(controlsPanel);

        var cTitle = Text("ControlsTitle", controlsPanel, "CONTROLS", 90, font, TextColor);
        Place(cTitle, new Vector2(0.5f, 0.82f), new Vector2(1200, 130));

        var cBody = Text("ControlsBody", controlsPanel,
            "<color=#C71F1A>WASD</color>   Move\n" +
            "<color=#C71F1A>Mouse</color>   Look\n" +
            "<color=#C71F1A>E</color>   Interact\n" +
            "<color=#C71F1A>Q</color>   Tactical HUD\n" +
            "<color=#C71F1A>Esc</color>   Leave a screen / minigame",
            44, null, TextColor);
        cBody.lineSpacing = 25;
        Place(cBody, new Vector2(0.5f, 0.5f), new Vector2(1000, 480));

        var backBtn = MakeButton("BackButton", controlsPanel, "BACK", font);
        var backRt = (RectTransform)backBtn.transform;
        backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0.14f);
        backRt.sizeDelta = new Vector2(460, 90);
        controlsPanel.gameObject.SetActive(false);

        // ---------- Music ----------
        var music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        if (music != null)
        {
            var src = new GameObject("MenuMusic").AddComponent<AudioSource>();
            src.clip = music;
            src.loop = true;
            src.playOnAwake = true;
            src.volume = 0.5f;
        }

        // ---------- Wire up MainMenu ----------
        var menu = new GameObject("MainMenu").AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("mainPanel").objectReferenceValue = mainPanel.gameObject;
        so.FindProperty("controlsPanel").objectReferenceValue = controlsPanel.gameObject;
        so.FindProperty("firstSelectedMain").objectReferenceValue = playBtn.gameObject;
        so.FindProperty("firstSelectedControls").objectReferenceValue = backBtn.gameObject;
        string gamePath = FindGameScenePath();
        if (gamePath != null) so.FindProperty("gameScene").stringValue = gamePath;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(playBtn.onClick, new UnityAction(menu.Play));
        UnityEventTools.AddPersistentListener(controlsBtn.onClick, new UnityAction(menu.ShowControls));
        UnityEventTools.AddPersistentListener(quitBtn.onClick, new UnityAction(menu.Quit));
        UnityEventTools.AddPersistentListener(backBtn.onClick, new UnityAction(menu.ShowMain));

        es.firstSelectedGameObject = playBtn.gameObject;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettingsFirst(ScenePath);

        Debug.Log($"Main menu created at {ScenePath}. Play loads: {gamePath ?? "(set Game Scene on the MainMenu object)"}");
    }

    // The level file name contains an accented character, so read its exact path from Build Settings.
    static string FindGameScenePath()
    {
        var match = EditorBuildSettings.scenes
            .Select(s => s.path)
            .FirstOrDefault(p => p.StartsWith("Assets/Level/Scenes/") &&
                                 p.EndsWith("Level.unity") &&
                                 !p.Contains("/Gyms/"));
        return match;
    }

    static void AddToBuildSettingsFirst(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes.Where(s => s.path != path));
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ---------- helpers ----------
    static void SetMaterials(SerializedProperty array, params string[] names)
    {
        var mats = names
            .Select(n => AssetDatabase.LoadAssetAtPath<Material>($"{FeedFolder}/{n}.mat"))
            .Where(m => m != null)
            .ToArray();
        array.arraySize = mats.Length;
        for (int i = 0; i < mats.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = mats[i];
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Place(TMP_Text t, Vector2 anchor, Vector2 size)
    {
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    static TextMeshProUGUI Text(string name, Transform parent, string content, float size, TMP_FontAsset font, Color color)
    {
        var t = NewUI(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(string name, Transform parent, string label, TMP_FontAsset font)
    {
        var rt = NewUI(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = Color.white;

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = new Color(0.12f, 0.12f, 0.13f, 0.85f);
        colors.highlightedColor = new Color(Accent.r, Accent.g, Accent.b, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.45f, 0.06f, 0.05f, 1f);
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 90;

        var txt = Text("Label", rt, label, 48, font, TextColor);
        Stretch(txt.rectTransform);
        return btn;
    }
}
