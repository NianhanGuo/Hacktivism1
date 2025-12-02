using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogManager : MonoBehaviour
{
    [Header("Rain Settings")]
    public RectTransform logParent;       
    public RainLogEntry logEntryPrefab;   
    public int logsPerClick = 25;         
    public float topMargin = 40f;         
    public float sideMargin = 40f;        

    [Header("Text Style")]
    public bool enableGlitch = true;      

    private RectTransform parentRect;

    private void Awake()
    {
        if (logParent != null)
            parentRect = logParent;
        else
            Debug.LogWarning("[LogManager] logParent is NULL.", this);

        if (logEntryPrefab == null)
            Debug.LogWarning("[LogManager] logEntryPrefab is NULL.", this);
    }

    // Called by InterfaceController after ANY button click
    public void SpawnRain(string baseLog)
    {
        if (parentRect == null || logEntryPrefab == null)
        {
            Debug.LogWarning("[LogManager] Missing references — cannot spawn logs.");
            return;
        }

        for (int i = 0; i < logsPerClick; i++)
        {
            CreateLog(baseLog);
        }
    }

    private void CreateLog(string baseLog)
    {
        RainLogEntry entry = Instantiate(logEntryPrefab, logParent);

        RectTransform rt = entry.GetComponent<RectTransform>();

        // random x position
        float x = Random.Range(
            -parentRect.rect.width / 2f + sideMargin,
            parentRect.rect.width / 2f - sideMargin
        );

        // start at top of screen
        float y = parentRect.rect.height / 2f - topMargin;

        rt.anchoredPosition = new Vector2(x, y);

        // random falling speed
        entry.fallSpeed = Random.Range(110f, 220f);

        // random lifetime
        entry.lifetime = Random.Range(3f, 5f);

        // random base alpha
        entry.baseAlpha = Random.Range(0.45f, 0.75f);

        // apply glitch
        if (enableGlitch)
            entry.text.text = GlitchString(baseLog);
        else
            entry.text.text = baseLog;
    }


    // =============== ASCII-ONLY GLITCH FUNCTION (NO FONT WARNINGS) ===============

    private string GlitchString(string baseText)
    {
        if (string.IsNullOrEmpty(baseText))
            return baseText;

        // ASCII-safe glitch symbols
        string glitchChars = "#$%&*@/<>+=-";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (char c in baseText)
        {
            float r = Random.value;

            if (r < 0.25f)
            {
                // 25% chance: replace with glitch
                sb.Append(glitchChars[Random.Range(0, glitchChars.Length)]);
            }
            else if (r < 0.30f)
            {
                // 5% chance: insert glitch then original
                sb.Append(glitchChars[Random.Range(0, glitchChars.Length)]);
                sb.Append(c);
            }
            else
            {
                // keep original
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
