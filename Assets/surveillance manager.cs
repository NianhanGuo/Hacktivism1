using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SurveillanceManager : MonoBehaviour
{
    [Header("Popup Setup")]
    public RectTransform popupParent;      // usually your Canvas
    public CameraPopup popupPrefab;        // camera pop prefab
    public int initialPopupCount = 3;      // how many at start

    [Header("Webcam")]
    public string preferredDeviceName = ""; // leave empty for default
    private WebCamTexture webcamTexture;
    private bool surveillanceStarted = false;

    [Header("Red Filter Settings")]
    [Tooltip("Maximum alpha of the red overlay on each camera popup.")]
    public float maxRedAlpha = 0.5f;

    [Tooltip("Number of open popups required to reach maxRedAlpha and show hacked warning.")]
    public int popupsForMaxRed = 50;

    [Header("Hacked Warning Window")]
    [Tooltip("Panel that asks: 'Is your computer hacked? Need help?' with Yes/No buttons.")]
    public GameObject hackedWarningWindow;

    [Tooltip("If true, the hacked warning will only appear once per session when threshold is reached.")]
    public bool showHackedWindowOnce = true;

    [Tooltip("Optional: console-style panel that records player actions / hacker lines.")]
    public ActionLogPanel actionLogPanel;

    private bool hackedWindowShown = false;

    // how many times player has clicked NO on hacked window
    private int hackedNoClickCount = 0;

    // track all active camera popups so we can update their red overlay
    private readonly List<CameraPopup> activePopups = new List<CameraPopup>();

    private void Start()
    {
        // Make sure warning window starts hidden
        if (hackedWarningWindow != null)
        {
            hackedWarningWindow.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }

    // Called from InterfaceController after user clicks a button
    public void BeginSurveillance()
    {
        if (surveillanceStarted)
        {
            Debug.Log("[Surveillance] BeginSurveillance called again, ignoring.");
            return;
        }

        surveillanceStarted = true;
        Debug.Log("[Surveillance] BeginSurveillance called. Starting coroutine...");
        StartCoroutine(StartWebcamAndSpawn());
    }

    private IEnumerator StartWebcamAndSpawn()
    {
        Debug.Log("[Surveillance] Checking webcam devices...");

        if (WebCamTexture.devices.Length > 0)
        {
            WebCamDevice device = WebCamTexture.devices[0];

            if (!string.IsNullOrEmpty(preferredDeviceName))
            {
                foreach (var d in WebCamTexture.devices)
                {
                    if (d.name.Contains(preferredDeviceName))
                    {
                        device = d;
                        break;
                    }
                }
            }

            Debug.Log("[Surveillance] Using webcam device: " + device.name);

            webcamTexture = new WebCamTexture(device.name);
            webcamTexture.Play();

            // wait a bit for webcam to actually start
            float timeout = 3f;
            float t = 0f;
            while (t < timeout && !webcamTexture.didUpdateThisFrame)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (!webcamTexture.isPlaying)
            {
                Debug.LogWarning("[Surveillance] Webcam failed to start. Check OS camera permission.");
            }
        }
        else
        {
            Debug.LogWarning("[Surveillance] No webcam devices found. Popups will have no video.");
        }

        // small delay, then spawn popups (even if no webcam)
        yield return new WaitForSeconds(0.1f);

        SpawnCameraPopups(initialPopupCount);
    }

    public void SpawnCameraPopups(int count)
    {
        if (popupParent == null || popupPrefab == null)
        {
            Debug.LogWarning("[Surveillance] Missing popupParent or popupPrefab.", this);
            return;
        }

        Rect rect = popupParent.rect;
        float margin = 60f;

        for (int i = 0; i < count; i++)
        {
            CameraPopup popup = Instantiate(popupPrefab, popupParent);
            RectTransform rt = popup.GetComponent<RectTransform>();

            float x = Random.Range(-rect.width / 2f + margin, rect.width / 2f - margin);
            float y = Random.Range(-rect.height / 2f + margin, rect.height / 2f - margin);
            rt.anchoredPosition = new Vector2(x, y);

            popup.Init(this);

            RawImage img = popup.GetComponentInChildren<RawImage>();
            if (img != null && webcamTexture != null)
            {
                img.texture = webcamTexture;
            }

            // track it for red overlay + hacked window logic
            if (!activePopups.Contains(popup))
            {
                activePopups.Add(popup);
            }
        }

        // after spawning, update red intensity + hacked window
        UpdatePopupRedFilters();
        CheckHackedWindowCondition();

        Debug.Log("[Surveillance] Spawned " + count + " camera popup(s). Active: " + activePopups.Count);
    }

    // Called by CameraPopup when user clicks X
    public void OnPopupClosed(CameraPopup popup)
    {
        if (popup != null)
        {
            // remove from our list before destroying
            activePopups.Remove(popup);
            Destroy(popup.gameObject);
        }

        Debug.Log("[Surveillance] Popup closed. Spawning 2 new popups.");

        // Spawn 2 new popups as before
        SpawnCameraPopups(2);
    }

    /// <summary>
    /// Recalculates the red overlay alpha based on how many popups are open,
    /// and applies it to every popup.
    /// </summary>
    private void UpdatePopupRedFilters()
    {
        int count = activePopups.Count;
        if (count <= 0)
            return;

        float normalized = 0f;

        if (popupsForMaxRed > 0)
        {
            normalized = Mathf.Clamp01((float)count / popupsForMaxRed);
        }

        float targetAlpha = maxRedAlpha * normalized;

        foreach (var popup in activePopups)
        {
            if (popup != null)
            {
                popup.SetRedFilterAlpha(targetAlpha);
            }
        }
    }

    /// <summary>
    /// Shows the hacked warning window once the number of open popups
    /// reaches or exceeds popupsForMaxRed (default 50).
    /// </summary>
    private void CheckHackedWindowCondition()
    {
        if (hackedWarningWindow == null)
            return;

        if (popupsForMaxRed <= 0)
            return;

        if (activePopups.Count >= popupsForMaxRed)
        {
            if (!showHackedWindowOnce || !hackedWindowShown)
            {
                hackedWarningWindow.SetActive(true);
                hackedWindowShown = true;
                Debug.Log("[Surveillance] Hacked warning window shown.");
            }
        }
    }

    // =======================
    // Hacked window buttons
    // =======================

    /// <summary>
    /// Called by the YES button on the hacked warning window.
    /// You can extend this (e.g., open a help panel, quit game, etc.).
    /// Right now it just closes the warning.
    /// </summary>
    public void OnHackedYesClicked()
    {
        if (hackedWarningWindow != null)
        {
            hackedWarningWindow.SetActive(false);
        }

        if (actionLogPanel != null)
        {
            actionLogPanel.AddLog("// user accepted help.");
        }

        Debug.Log("[Surveillance] User clicked YES on hacked warning.");
    }

    /// <summary>
    /// Called by the NO button on the hacked warning window.
    /// Shows different lines depending on how many times NO was clicked,
    /// then re-opens the same hacked window so the player must choose again.
    /// </summary>
    public void OnHackedNoClicked()
    {
        hackedNoClickCount++;

        if (hackedWarningWindow != null)
        {
            hackedWarningWindow.SetActive(false);
        }

        if (actionLogPanel != null)
        {
            string msg;

            if (hackedNoClickCount == 1)
            {
                msg = "\"Are you sure? Choose again.\"";
            }
            else if (hackedNoClickCount == 2)
            {
                msg = "\"You must need our help. Let us help you.\"";
            }
            else
            {
                msg = "\"Just say you need help. You need help.\"";
            }

            actionLogPanel.AddLog(msg);
        }

        if (hackedWarningWindow != null)
        {
            // small delay to feel like it closes then pops up again
            StartCoroutine(ReopenHackedWindow());
        }

        Debug.Log("[Surveillance] User clicked NO on hacked warning. Count: " + hackedNoClickCount);
    }

    private IEnumerator ReopenHackedWindow()
    {
        yield return new WaitForSeconds(0.25f);
        hackedWarningWindow.SetActive(true);
    }
}
