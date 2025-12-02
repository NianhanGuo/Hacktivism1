using UnityEngine;

public class ConsoleToActionLog : MonoBehaviour
{
    [Header("Target")]
    public ActionLogPanel actionLog;   // drag your action log panel here

    [Header("Filters")]
    public bool logInfo = true;
    public bool logWarnings = true;
    public bool logErrors = true;

    private float startTime;

    private void OnEnable()
    {
        startTime = Time.realtimeSinceStartup;
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (actionLog == null)
            return;

        // Filter types if you want
        if (type == LogType.Log && !logInfo) return;
        if (type == LogType.Warning && !logWarnings) return;
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !logErrors) return;

        // ---------- build timestamp ----------
        float t = Time.realtimeSinceStartup - startTime;
        int totalSeconds = Mathf.FloorToInt(t);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        int millis = Mathf.FloorToInt((t - Mathf.Floor(t)) * 1000f);

        string timeText = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, millis);

        // ---------- choose level tag + color ----------
        string levelTag;
        switch (type)
        {
            case LogType.Warning:
                levelTag = "<color=#FFD700>[WARN]</color>";
                break;

            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                levelTag = "<color=#FF5555>[ERR ]</color>";
                break;

            default:
                levelTag = "<color=#55CCFF>[INFO]</color>";
                break;
        }

        // escape < > so user logs don't break our rich text
        string safeMessage = EscapeRichText(logString);

        // final "codey" line:
        // 00:12.345 [INFO] userClicked("ClearHistory");
        string formatted = $"<color=#888888>{timeText}</color> {levelTag} {safeMessage};";

        actionLog.AddLog(formatted);
    }

    private string EscapeRichText(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
