using UnityEngine;
using UnityEngine.UI;

public class CameraPopup : MonoBehaviour
{
    [Header("UI References")]
    public Button closeButton;              // the X button in top-left

    [Tooltip("Optional: red overlay image used for the progressive red filter.")]
    public Image redFilterImage;            // assign in the prefab if you want the effect

    private SurveillanceManager manager;

    // called right after Instantiate
    public void Init(SurveillanceManager mgr)
    {
        manager = mgr;

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }

    private void OnCloseClicked()
    {
        if (manager != null)
        {
            manager.OnPopupClosed(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by SurveillanceManager whenever the global red intensity changes.
    /// alpha will be clamped to [0,1]. We cap it there, but SurveillanceManager
    /// will typically give you values up to 0.5f.
    /// </summary>
    public void SetRedFilterAlpha(float alpha)
    {
        if (redFilterImage == null)
            return;

        Color c = redFilterImage.color;
        c.a = Mathf.Clamp01(alpha);
        redFilterImage.color = c;
    }
}
