using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Management screen Workers tab: hire button, and a scrollable list of worker cards (name, fire, assign stations).
/// </summary>
public class WorkersUI : MonoBehaviour
{
    [Tooltip("Shows current number of employees")]
    public TextMeshProUGUI countText;
    [Tooltip("Shows hire cost (e.g. Cost: $100)")]
    public TextMeshProUGUI costText;
    [Tooltip("Click to hire one worker")]
    public Button hireButton;
    [Tooltip("Parent for worker cards. If null, a scroll area is created at runtime.")]
    public Transform cardContainer;

    ProductionManager production;
    bool listenerAdded;
    ScrollRect cardsScrollRect;

    void Start()
    {
        EnsureRefs();
        EnsureCardContainer();
    }

    void OnEnable()
    {
        EnsureRefs();
        EnsureCardContainer();
        Refresh();
    }

    void EnsureRefs()
    {
        if (production == null)
            production = ProductionManager.Instance != null ? ProductionManager.Instance : FindObjectOfType<ProductionManager>();
        if (countText == null) countText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (hireButton == null) hireButton = GetComponentInChildren<Button>(true);
        if (hireButton != null && !listenerAdded)
        {
            hireButton.onClick.AddListener(OnHireClicked);
            listenerAdded = true;
        }
    }

    void EnsureCardContainer()
    {
        if (cardContainer != null) return;
        var rect = GetComponent<RectTransform>();
        if (rect == null) return;

        GameObject scrollGo = new GameObject("WorkerCardsScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(transform, false);
        var scrollRect = (RectTransform)scrollGo.transform;
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(12, 12);
        scrollRect.offsetMax = new Vector2(-12, -140);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRect = (RectTransform)viewport.transform;
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("CardContainer", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0, 1f);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        scroll.viewport = vpRect;
        scroll.content = contentRect;
        cardContainer = content.transform;
        cardsScrollRect = scroll;
    }

    void OnHireClicked()
    {
        if (production == null)
        {
            production = FindObjectOfType<ProductionManager>();
            if (production == null) return;
        }
        KitchenEmployee hired = production.HireWorker();
        Refresh();
    }

    public void Refresh()
    {
        EnsureRefs();
        EnsureCardContainer();

        if (production == null)
        {
            if (countText != null) countText.text = "Workers: —";
            if (costText != null) costText.text = "Cost: — (No ProductionManager in scene)";
            if (hireButton != null) hireButton.interactable = false;
            return;
        }

        int count = production.employees != null ? production.employees.Count : 0;
        if (countText != null)
            countText.text = "Workers: " + count;

        bool canHire = production.employeePrefab != null;
        var money = FindObjectOfType<MoneyManager>();
        string status = "";
        if (production.employeePrefab == null)
        {
            canHire = false;
            status = " — Assign Employee Prefab on ProductionManager.";
        }
        else if (money != null && !money.CanAfford(production.hireCost))
        {
            canHire = false;
            status = " — Not enough money.";
        }

        if (costText != null)
            costText.text = "Cost: $" + production.hireCost + status;

        if (hireButton != null)
            hireButton.interactable = canHire;

        if (cardContainer == null) return;
        for (int i = cardContainer.childCount - 1; i >= 0; i--)
            Destroy(cardContainer.GetChild(i).gameObject);

        if (production.employees == null) return;
        foreach (var emp in production.employees)
        {
            if (emp == null) continue;
            var card = CreateWorkerCard();
            if (card != null)
            {
                card.transform.SetParent(cardContainer, false);
                card.Bind(emp);
            }
        }
    }

    WorkerCardUI CreateWorkerCard()
    {
        var cardGo = new GameObject("WorkerCard", typeof(RectTransform));
        var cardRect = (RectTransform)cardGo.transform;
        cardRect.sizeDelta = new Vector2(0, 72);
        var cardImage = cardGo.AddComponent<Image>();
        cardImage.color = new Color(0.25f, 0.25f, 0.3f, 0.95f);
        var le = cardGo.AddComponent<LayoutElement>();
        le.minHeight = 72;
        le.preferredHeight = 72;
        le.flexibleWidth = 1;

        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var row1 = new GameObject("Row1", typeof(RectTransform));
        row1.transform.SetParent(cardGo.transform, false);
        var row1Rect = (RectTransform)row1.transform;
        row1Rect.sizeDelta = new Vector2(0, 28);
        var row1HLG = row1.AddComponent<HorizontalLayoutGroup>();
        row1HLG.spacing = 8;
        row1HLG.childForceExpandWidth = false;
        row1HLG.childControlWidth = true;
        var row1LE = row1.AddComponent<LayoutElement>();
        row1LE.minHeight = 28;
        row1LE.preferredHeight = 28;

        var nameInputGo = CreateNameInput(row1.transform);
        var fireBtn = CreateButton(row1.transform, "Fire", new Color(0.6f, 0.2f, 0.2f, 1f));

        var row2 = new GameObject("Row2", typeof(RectTransform));
        row2.transform.SetParent(cardGo.transform, false);
        var row2HLG = row2.AddComponent<HorizontalLayoutGroup>();
        row2HLG.spacing = 12;
        row2HLG.childForceExpandWidth = false;
        var row2LE = row2.AddComponent<LayoutElement>();
        row2LE.minHeight = 24;
        row2LE.preferredHeight = 24;

        var tFreezer = CreateStationToggle(row2.transform, "Freezer");
        var tGrill = CreateStationToggle(row2.transform, "Grill");
        var tPantry = CreateStationToggle(row2.transform, "Pantry");
        var tAssembly = CreateStationToggle(row2.transform, "Assembly");

        var cardUI = cardGo.AddComponent<WorkerCardUI>();
        cardUI.nameInputObject = nameInputGo;
        cardUI.fireButton = fireBtn;
        cardUI.toggleFreezer = tFreezer;
        cardUI.toggleGrill = tGrill;
        cardUI.togglePantry = tPantry;
        cardUI.toggleAssembly = tAssembly;
        return cardUI;
    }

    GameObject CreateNameInput(Transform parent)
    {
        var go = new GameObject("NameInput", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(180, 26);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 120;
        le.preferredWidth = 180;
        le.flexibleWidth = 1;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        var input = go.AddComponent<InputField>();

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6, 2);
        textRect.offsetMax = new Vector2(-6, -2);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.supportRichText = false;
        input.textComponent = text;
        input.interactable = true;
        input.transition = Selectable.Transition.ColorTint;
        return go;
    }

    Button CreateButton(Transform parent, string label, Color color)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(70, 26);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 70;
        le.preferredWidth = 70;
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        return btn;
    }

    Toggle CreateStationToggle(Transform parent, string label)
    {
        var go = new GameObject("Toggle_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(80, 22);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 80;
        le.preferredWidth = 80;
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bg;

        var checkGo = new GameObject("Checkmark", typeof(RectTransform));
        checkGo.transform.SetParent(go.transform, false);
        var checkRect = (RectTransform)checkGo.transform;
        checkRect.anchorMin = new Vector2(0, 0.25f);
        checkRect.anchorMax = new Vector2(0, 0.75f);
        checkRect.offsetMin = new Vector2(4, 0);
        checkRect.offsetMax = new Vector2(22, 0);
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.color = new Color(0.3f, 0.8f, 0.3f, 1f);
        toggle.graphic = checkImg;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = (RectTransform)labelGo.transform;
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(26, 0);
        labelRect.offsetMax = new Vector2(-4, 0);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 12;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(0.9f, 0.9f, 0.95f, 1f);
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        return toggle;
    }
}
