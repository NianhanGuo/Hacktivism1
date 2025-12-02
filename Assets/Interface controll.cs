using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InterfaceController : MonoBehaviour
{
    [Header("Background")]
    // IMPORTANT: Assign your TINT OVERLAY image here (not the XP wallpaper).
    public Image backgroundImage;

    [Header("Log System")]
    public LogManager logManager;

    [Header("Main Window Popup")]
    public GameObject mainWindow;        // drag your "main window" panel here
    public float windowDelay = 1.5f;     // delay before first popup
    public float windowAnimDuration = 0.35f; // pop in/out animation duration

    [Header("Surveillance")]
    public SurveillanceManager surveillanceManager; // drag your SurveillanceManager here

    private RectTransform mainWindowRect;
    private CanvasGroup mainWindowCanvasGroup;
    private Coroutine windowAnimRoutine;

    private void Awake()
    {
        if (mainWindow != null)
        {
            mainWindowRect = mainWindow.GetComponent<RectTransform>();

            mainWindowCanvasGroup = mainWindow.GetComponent<CanvasGroup>();
            if (mainWindowCanvasGroup == null)
            {
                mainWindowCanvasGroup = mainWindow.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Start()
    {
        if (mainWindow != null)
        {
            mainWindow.SetActive(false);
            StartCoroutine(ShowMainWindowAfterDelay());
        }
    }

    private IEnumerator ShowMainWindowAfterDelay()
    {
        yield return new WaitForSeconds(windowDelay);
        ShowWindowPop();
    }

    // ------------------------
    // Window pop in / out
    // ------------------------

    private void ShowWindowPop()
    {
        if (mainWindow == null || mainWindowRect == null || mainWindowCanvasGroup == null)
            return;

        if (windowAnimRoutine != null)
            StopCoroutine(windowAnimRoutine);

        mainWindow.SetActive(true);
        windowAnimRoutine = StartCoroutine(AnimateWindow(true));
    }

    private void HideWindowPop()
    {
        if (mainWindow == null || mainWindowRect == null || mainWindowCanvasGroup == null)
            return;

        if (windowAnimRoutine != null)
            StopCoroutine(windowAnimRoutine);

        windowAnimRoutine = StartCoroutine(AnimateWindow(false));
    }

    private IEnumerator AnimateWindow(bool show)
    {
        float elapsed = 0f;

        Vector3 startScale = show ? Vector3.one * 0.7f : mainWindowRect.localScale;
        Vector3 endScale   = show ? Vector3.one : Vector3.one * 0.7f;

        float startAlpha = show ? 0f : mainWindowCanvasGroup.alpha;
        float endAlpha   = show ? 1f : 0f;

        if (show)
        {
            mainWindowRect.localScale = startScale;
            mainWindowCanvasGroup.alpha = startAlpha;
        }

        while (elapsed < windowAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / windowAnimDuration);
            float eased = t * t * (3f - 2f * t); // smoothstep

            mainWindowRect.localScale = Vector3.Lerp(startScale, endScale, eased);
            mainWindowCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);

            yield return null;
        }

        mainWindowRect.localScale = endScale;
        mainWindowCanvasGroup.alpha = endAlpha;

        if (!show)
        {
            mainWindow.SetActive(false);
        }
    }

    // =======================
    // Button Click Handlers
    // =======================

    public void OnClearHistoryClicked()
    {
        SetBackgroundToWarn();

        if (logManager != null)
        {
            logManager.SpawnRain("log: userClicked(\"ClearHistory\")");
        }

        HideWindowPop();

        if (surveillanceManager != null)
        {
            surveillanceManager.BeginSurveillance();  // turn on camera + popups
        }
    }

    public void OnTurnOffTrackingClicked()
    {
        SetBackgroundToNeutral();

        if (logManager != null)
        {
            logManager.SpawnRain("log: userClicked(\"TurnOffTracking\")");
        }

        HideWindowPop();

        if (surveillanceManager != null)
        {
            surveillanceManager.BeginSurveillance();
        }
    }

    public void OnOptOutClicked()
    {
        SetBackgroundToCalm();

        if (logManager != null)
        {
            logManager.SpawnRain("log: userClicked(\"OptOutOfDataCollection\")");
        }

        HideWindowPop();

        if (surveillanceManager != null)
        {
            surveillanceManager.BeginSurveillance();
        }
    }

    // ============================
    // Background Color (with alpha)
    // ============================

    public void SetBackgroundToWarn()
    {
        if (backgroundImage != null)
        {
            // subtle reddish overlay
            backgroundImage.color = new Color(0.2f, 0.05f, 0.08f, 0.05f);
        }
    }

    public void SetBackgroundToCalm()
    {
        if (backgroundImage != null)
        {
            // subtle bluish overlay
            backgroundImage.color = new Color(0.11f, 0.16f, 0.28f, 0.05f);
        }
    }

    public void SetBackgroundToNeutral()
    {
        if (backgroundImage != null)
        {
            // subtle neutral overlay
            backgroundImage.color = new Color(0.18f, 0.20f, 0.24f, 0.05f);
        }
    }
}
