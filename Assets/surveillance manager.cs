using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

        for (int i = 0; i < count; i++)
        {
            CameraPopup popup = Instantiate(popupPrefab, popupParent);
            RectTransform rt = popup.GetComponent<RectTransform>();

            float margin = 60f;
            float x = Random.Range(-rect.width / 2f + margin, rect.width / 2f - margin);
            float y = Random.Range(-rect.height / 2f + margin, rect.height / 2f - margin);
            rt.anchoredPosition = new Vector2(x, y);

            popup.Init(this);

            RawImage img = popup.GetComponentInChildren<RawImage>();
            if (img != null && webcamTexture != null)
            {
                img.texture = webcamTexture;
            }
        }

        Debug.Log("[Surveillance] Spawned " + count + " camera popup(s).");
    }

    // Called by CameraPopup when user clicks X
    public void OnPopupClosed(CameraPopup popup)
    {
        if (popup != null)
        {
            Destroy(popup.gameObject);
        }

        Debug.Log("[Surveillance] Popup closed. Spawning 2 new popups.");
        SpawnCameraPopups(2);
    }
}
