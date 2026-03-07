using UnityEngine;
using TMPro;

public class GameModeManager : MonoBehaviour
{
    public enum Mode { Build, Play }
    public Mode CurrentMode { get; private set; } = Mode.Build;

    [Header("UI")]
    public TextMeshProUGUI modeText;

    void Start()
    {
        UpdateUI();
    }

    public void ToggleMode()
    {
        CurrentMode = (CurrentMode == Mode.Build) ? Mode.Play : Mode.Build;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (modeText)
            modeText.text = "Mode: " + CurrentMode.ToString();
    }
}