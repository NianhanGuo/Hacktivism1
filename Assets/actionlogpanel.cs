using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionLogPanel : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Text component that displays the log (e.g. a TMP text inside a Scroll View).")]
    public TextMeshProUGUI logText;

    [Tooltip("Optional ScrollRect to auto-scroll to the bottom when a new line is added.")]
    public ScrollRect scrollRect;

    [Header("Settings")]
    [Tooltip("Maximum number of lines to keep in the panel.")]
    public int maxLines = 80;

    [Tooltip("Automatically scroll to the newest line when true.")]
    public bool autoScroll = true;

    private readonly List<string> _lines = new List<string>();

    /// <summary>
    /// Adds a new line to the action log, trimming to maxLines and updating the text.
    /// </summary>
    public void AddLog(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        _lines.Add(line);

        // Trim old lines
        while (_lines.Count > maxLines)
        {
            _lines.RemoveAt(0);
        }

        // Rebuild text
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < _lines.Count; i++)
        {
            sb.AppendLine(_lines[i]);
        }

        if (logText != null)
        {
            logText.text = sb.ToString();
        }

        // Auto scroll to bottom
        if (autoScroll && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // bottom
        }
    }

    /// <summary>
    /// Clears all lines from the log panel.
    /// </summary>
    public void ClearLog()
    {
        _lines.Clear();
        if (logText != null)
        {
            logText.text = string.Empty;
        }
    }
}
