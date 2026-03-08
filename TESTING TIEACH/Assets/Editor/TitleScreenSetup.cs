using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class TitleScreenSetup
{
    const string MenuName = "GameObject/UI/Title Screen";

    static void AssignTMPFont(TextMeshProUGUI tmp)
    {
        if (tmp.font != null) return;
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null) tmp.font = defaultFont;
    }

    static GameObject CreateUIButton(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.35f, 0.95f);

        go.AddComponent<Button>();

        GameObject textGo = new GameObject("Text (TMP)", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Button";
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        AssignTMPFont(tmp);
        return go;
    }

    [MenuItem(MenuName, false, 10)]
    [MenuItem("Tools/Create Title Screen", false, 100)]
    public static void CreateTitleScreen()
    {
        // ---- Title Camera (background = view of restaurant exterior) ----
        GameObject titleCamGo = new GameObject("TitleCamera");
        var titleCam = titleCamGo.AddComponent<Camera>();
        titleCam.clearFlags = CameraClearFlags.Skybox;
        titleCam.cullingMask = -1;
        titleCam.orthographic = false;
        titleCam.orthographicSize = 5f;
        titleCam.nearClipPlane = 0.3f;
        titleCam.farClipPlane = 1000f;
        titleCam.depth = -1;
        titleCam.enabled = false;
        titleCamGo.tag = "Untagged";
        titleCamGo.transform.position = new Vector3(0f, 5f, -12f);
        titleCamGo.transform.LookAt(Vector3.zero);

        // ---- Canvas for title UI ----
        GameObject canvasGo = new GameObject("TitleScreenCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject root = new GameObject("TitleScreen", typeof(RectTransform));
        root.transform.SetParent(canvasGo.transform, false);
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var controller = root.AddComponent<TitleScreenController>();
        controller.titleCamera = titleCam;

        Camera gameCam = null;
        var cams = Object.FindObjectsOfType<Camera>();
        foreach (var cam in cams)
        {
            if (cam != titleCam && cam.CompareTag("MainCamera"))
            {
                gameCam = cam;
                break;
            }
        }
        if (gameCam == null) gameCam = Object.FindObjectOfType<Camera>();
        if (gameCam == titleCam) gameCam = null;
        controller.gameCamera = gameCam;

        var allCanvases = Object.FindObjectsOfType<Canvas>();
        foreach (var c in allCanvases)
        {
            if (c.gameObject != canvasGo)
            {
                controller.inGameUIRoot = c.gameObject;
                break;
            }
        }

        // Full-screen panel (semi-transparent so camera view shows through)
        GameObject panel = new GameObject("TitlePanel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.1f, 0.5f);

        controller.titlePanel = panel;

        // Game title text
        GameObject titleTextGo = new GameObject("TitleText", typeof(RectTransform));
        titleTextGo.transform.SetParent(panel.transform, false);
        RectTransform titleTextRect = (RectTransform)titleTextGo.transform;
        titleTextRect.anchorMin = new Vector2(0.5f, 0.75f);
        titleTextRect.anchorMax = new Vector2(0.5f, 0.75f);
        titleTextRect.pivot = new Vector2(0.5f, 0.5f);
        titleTextRect.anchoredPosition = Vector2.zero;
        titleTextRect.sizeDelta = new Vector2(600, 80);
        var titleTMP = titleTextGo.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Restaurant";
        titleTMP.fontSize = 56;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;
        AssignTMPFont(titleTMP);

        // Play button
        GameObject playBtn = CreateUIButton("PlayButton", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(220, 50), new Vector2(0, 20));
        if (playBtn.GetComponentInChildren<TextMeshProUGUI>() != null)
            playBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
        controller.playButton = playBtn.GetComponent<Button>();

        // Settings button
        GameObject settingsBtn = CreateUIButton("SettingsButton", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(220, 50), new Vector2(0, -45));
        if (settingsBtn.GetComponentInChildren<TextMeshProUGUI>() != null)
            settingsBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Settings";
        controller.settingsButton = settingsBtn.GetComponent<Button>();

        // Exit button
        GameObject exitBtn = CreateUIButton("ExitButton", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(220, 50), new Vector2(0, -110));
        if (exitBtn.GetComponentInChildren<TextMeshProUGUI>() != null)
            exitBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Exit Game";
        controller.exitButton = exitBtn.GetComponent<Button>();

        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Title Screen");
        Undo.RegisterCreatedObjectUndo(titleCamGo, "Create Title Camera");
        Selection.activeGameObject = root;
        Debug.Log("Title Screen created. In-Game UI Root auto-assigned to hide game UI on title. Position TitleCamera to view the outside of your restaurant.");
    }
}
