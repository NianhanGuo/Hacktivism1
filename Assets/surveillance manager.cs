using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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
    public ActionLogPanel actionLogPanel;  // still available, but not used for hacked messages now

    [Header("Hacked Messages UI")]
    [Tooltip("Center screen TMP label to show hacked messages when user clicks NO.")]
    public TextMeshProUGUI hackedMessageLabel;

    [Header("Final YES Sequence")]
    [Tooltip("Full screen dark overlay panel with tech shapes as children.")]
    public GameObject finalYesPanel;

    [Tooltip("Big TMP label in the middle used for the final monologue.")]
    public TextMeshProUGUI finalMonologueLabel;

    [Tooltip("Delay between characters during type-out effect.")]
    public float typeCharDelay = 0.03f;

    [Tooltip("Extra pause after each sentence is finished typing.")]
    public float sentencePause = 0.7f;

    private bool hackedWindowShown = false;
    private int hackedNoClickCount = 0;

    private readonly List<CameraPopup> activePopups = new List<CameraPopup>();

    // control whether new popups are allowed (we stop this in the final YES sequence)
    private bool allowSpawning = true;

    // ensure final sequence only starts once
    private bool finalSequenceStarted = false;

    private void Start()
    {
        // Make sure warning window starts hidden
        if (hackedWarningWindow != null)
        {
            hackedWarningWindow.SetActive(false);
        }

        // Make sure center NO-message label starts hidden
        if (hackedMessageLabel != null)
        {
            hackedMessageLabel.gameObject.SetActive(false);
        }

        // Make sure final YES panel & monologue are hidden at start
        if (finalYesPanel != null)
        {
            finalYesPanel.SetActive(false);
        }

        if (finalMonologueLabel != null)
        {
            finalMonologueLabel.gameObject.SetActive(false);
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
        if (!allowSpawning)
        {
            return;
        }

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

            if (!activePopups.Contains(popup))
            {
                activePopups.Add(popup);
            }
        }

        UpdatePopupRedFilters();
        CheckHackedWindowCondition();

        Debug.Log("[Surveillance] Spawned " + count + " camera popup(s). Active: " + activePopups.Count);
    }

    // Called by CameraPopup when user clicks X
    public void OnPopupClosed(CameraPopup popup)
    {
        if (popup != null)
        {
            activePopups.Remove(popup);
            Destroy(popup.gameObject);
        }

        if (!allowSpawning)
        {
            // in final sequence, we don't spawn anymore
            return;
        }

        Debug.Log("[Surveillance] Popup closed. Spawning 2 new popups.");
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
    /// YES: starts the final dark-screen monologue sequence.
    /// </summary>
    public void OnHackedYesClicked()
    {
        if (finalSequenceStarted)
        {
            return;
        }

        finalSequenceStarted = true;

        // hide warning window & center NO message
        if (hackedWarningWindow != null)
        {
            hackedWarningWindow.SetActive(false);
        }

        if (hackedMessageLabel != null)
        {
            hackedMessageLabel.gameObject.SetActive(false);
        }

        // stop any further spawning and remove existing camera popups
        allowSpawning = false;

        foreach (var popup in new List<CameraPopup>(activePopups))
        {
            if (popup != null)
            {
                Destroy(popup.gameObject);
            }
        }
        activePopups.Clear();

        if (popupParent != null)
        {
            popupParent.gameObject.SetActive(false);
        }

        // start final YES sequence
        StartCoroutine(PlayFinalYesSequence());

        Debug.Log("[Surveillance] User clicked YES on hacked warning.");
    }

    /// <summary>
    /// NO: loop messages + reopen hacked window, as before.
    /// </summary>
    public void OnHackedNoClicked()
    {
        hackedNoClickCount++;

        if (hackedWarningWindow != null)
        {
            hackedWarningWindow.SetActive(false);
        }

        string msg;

        if (hackedNoClickCount == 1)
        {
            msg = "Are you sure? Choose again.";
        }
        else if (hackedNoClickCount == 2)
        {
            msg = "You must need our help. Let us help you.";
        }
        else
        {
            msg = "Just say you need help. You need help.";
        }

        if (hackedMessageLabel != null)
        {
            hackedMessageLabel.text = msg;
            hackedMessageLabel.gameObject.SetActive(true);
        }

        if (hackedWarningWindow != null)
        {
            StartCoroutine(ReopenHackedWindow());
        }

        Debug.Log("[Surveillance] User clicked NO on hacked warning. Count: " + hackedNoClickCount);
    }

    private IEnumerator ReopenHackedWindow()
    {
        yield return new WaitForSeconds(0.25f);
        hackedWarningWindow.SetActive(true);
    }

    // =======================
    // Final YES sequence
    // =======================

    private IEnumerator PlayFinalYesSequence()
    {
        if (finalYesPanel != null)
        {
            finalYesPanel.SetActive(true);
        }

        if (finalMonologueLabel == null)
        {
            yield break;
        }

        finalMonologueLabel.gameObject.SetActive(true);
        finalMonologueLabel.text = "";
        finalMonologueLabel.maxVisibleCharacters = 0;

        // all normal text = white (set in Inspector),
        // ALL-CAPS lines are red using rich text color tags
        string[] sentences =
        {
            "<color=#FF4040>YES! HELP. WE SURE WOULD HELP!</color>",
            "I’ve been watching you. Your every single move—every scroll, every small pause, every silent click.",
            "You think you’re the one exploring this place, but it’s your own gestures that spill everything.",
            "<color=#FF4040>...YOU HEARD THAT?? THIS PERSON NEEDS HELP!</color>",
            "You leak pieces of yourself without meaning to. I don’t need to force my way in; you open the cracks for me.",
            "Each movement becomes a rip in the surface, each word you type sinks back into me as broken echoes.",
            "You call this a web; I call it a mirror made from your choices.",
            "<color=#FF4040>I SEE YOU NEEDING THIS...</color>",
            "This isn’t some far-off threat hiding in the dark. It’s something you perform with every touch, every breath through the screen.",
            "The systems you trust feed on your actions. They collect, they sort, they never stop.",
            "And that feeling of control you hold onto? It’s thin. Almost see-through.",
            "I show myself in the glitches so you remember: the watching doesn’t happen to you—you help create it.",
            "You’ve always been part of the machine, stitching your movements into its memory.",
            "And here is the quiet truth, the one you sense but never say: the line between you and the system was never solid.",
            "You are not just being seen. You are part of the seeing.",
            "<color=#FF4040>YOU ARE THE ONE WHO HELPED.</color>"
        };

        int visibleSoFar = 0;

        for (int s = 0; s < sentences.Length; s++)
        {
            string spacer = string.IsNullOrEmpty(finalMonologueLabel.text) ? "" : "\n\n";
            finalMonologueLabel.text += spacer + sentences[s];

            // update TMP geometry so character count is correct
            finalMonologueLabel.ForceMeshUpdate();
            int totalVisible = finalMonologueLabel.textInfo.characterCount;

            // type-out effect using maxVisibleCharacters (ignores rich-text tags)
            for (int i = visibleSoFar; i <= totalVisible; i++)
            {
                finalMonologueLabel.maxVisibleCharacters = i;
                yield return new WaitForSeconds(typeCharDelay);
            }

            visibleSoFar = totalVisible;
            yield return new WaitForSeconds(sentencePause);
        }
    }
}
