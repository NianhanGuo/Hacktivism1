using UnityEngine;
using TMPro;

public class RainLogEntry : MonoBehaviour
{
    [Header("Text Reference")]
    public TextMeshProUGUI text;

    [Header("Motion & Life")]
    public float lifetime = 4f;
    public float fallSpeed = 120f;
    public float flashSpeed = 2f;

    // Increase base alpha here:
    public float baseAlpha = 0.9f;   // ⬅⬅ higher opacity

    private float timer = 0f;
    private CanvasGroup canvasGroup;

    private Color randomColor;

    private void Awake()
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (text == null)
        {
            text = GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = GetComponentInChildren<TextMeshProUGUI>();
        }

        // ----- low-brightness neon color (H,S,V) -----
        float hue = Random.Range(0f, 1f);
        float sat = Random.Range(0.25f, 0.5f);
        float val = Random.Range(0.25f, 0.45f);

        randomColor = Color.HSVToRGB(hue, sat, val);

        // ⬅⬅ Make the color itself more opaque
        randomColor.a = 0.95f;
    }

    public void Init(string message)
    {
        if (text != null)
        {
            text.text = message;
            text.color = randomColor;
        }
        else
        {
            Debug.LogWarning("RainLogEntry has no TextMeshProUGUI assigned or found.", this);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        transform.Translate(0f, -fallSpeed * Time.deltaTime, 0f);

        float flash = (Mathf.Sin(timer * flashSpeed) + 1f) * 0.5f;

        // ⬅⬅ final alpha = flash * baseAlpha
        canvasGroup.alpha = baseAlpha * flash;

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
