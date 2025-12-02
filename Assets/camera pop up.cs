using UnityEngine;
using UnityEngine.UI;

public class CameraPopup : MonoBehaviour
{
    public Button closeButton;              // the X button in top-left
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
}
