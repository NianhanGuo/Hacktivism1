using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button openButton;
    [SerializeField] private Button submitButton;

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenPanel);
        }

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(TryClosePanel);
        }
    }

    public void SetQuestion(string question)
    {
        if (questionText != null)
        {
            questionText.text = question;
        }
    }

    private void OpenPanel()
    {
        if (panelRoot == null || answerInput == null) return;

        panelRoot.SetActive(true);
        answerInput.text = "";
        answerInput.ActivateInputField();

        // 🔻 新增：打开面板后隐藏 openButton
        if (openButton != null)
        {
            openButton.gameObject.SetActive(false);
        }
    }

    private void TryClosePanel()
    {
        if (answerInput == null || panelRoot == null) return;

        string text = answerInput.text.Trim();

        if (string.IsNullOrEmpty(text))
        {
            // 在这里可以弹一句提示 UI，现在先用日志
            Debug.Log("Please answer the question before closing the panel.");
            return;
        }

        panelRoot.SetActive(false);

        // 这里如果你想保存答案，可以在这里调用别的 manager
        // Debug.Log("Player answered: " + text);
    }
}
