using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class LoreIntroManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup loreCanvasGroup;
    [SerializeField] private TextMeshProUGUI loreText;
    [SerializeField] private TextMeshProUGUI skipText;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float textDelay = 1f;
    [SerializeField] private float paragraphDelay = 2f;
    [SerializeField] private float fadeOutDuration = 2f;
    [SerializeField] private float autoSkipTime = 15f;

    [Header("Lore Text (Spanish)")]
    [TextArea(3, 5)]
    [SerializeField] private string paragraph1 = "Vesper entró en la Torre de Ceniza buscando fortuna. El Núcleo Carmesí, una gema de poder inimaginable, lo llamaba desde las profundidades.";
    [TextArea(3, 5)]
    [SerializeField] private string paragraph2 = "Al tocarlo, el cristal estalló. Una mitad se clavó en su pecho; la otra despertó a Malikor, un gigante implacable atrapado durante siglos. Sus vidas quedaron unidas por el fragmento.";
    [TextArea(3, 5)]
    [SerializeField] private string paragraph3 = "Ahora, cada muerte de Vesper agrieta más la gema. Para romper el vínculo y destruir al monstruo, deberá usar las trampas de la torre contra sí mismo... sacrificando todo.";

    private System.Action onIntroDone;
    private bool skipRequested;

    public void StartIntro(System.Action callback)
    {
        onIntroDone = callback;
        gameObject.SetActive(true);

        if (loreCanvasGroup != null)
        {
            loreCanvasGroup.alpha = 1f;
            loreCanvasGroup.blocksRaycasts = true;
        }

        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        // Start with fully black screen
        if (loreText != null) loreText.text = "";
        if (skipText != null) skipText.alpha = 0f;

        yield return new WaitForSeconds(textDelay);

        // Show skip hint
        if (skipText != null)
        {
            skipText.text = "Pulsa cualquier tecla para continuar...";
            StartCoroutine(FadeInText(skipText, 1f));
        }

        // Show paragraph 1
        yield return StartCoroutine(ShowParagraph(paragraph1));
        if (skipRequested) { EndIntro(); yield break; }

        yield return new WaitForSeconds(paragraphDelay);
        if (skipRequested) { EndIntro(); yield break; }

        // Show paragraph 2
        yield return StartCoroutine(ShowParagraph(paragraph1 + "\n\n" + paragraph2));
        if (skipRequested) { EndIntro(); yield break; }

        yield return new WaitForSeconds(paragraphDelay);
        if (skipRequested) { EndIntro(); yield break; }

        // Show paragraph 3
        yield return StartCoroutine(ShowParagraph(paragraph1 + "\n\n" + paragraph2 + "\n\n" + paragraph3));
        if (skipRequested) { EndIntro(); yield break; }

        // Wait for auto-skip or player input
        float waitTimer = 0f;
        while (waitTimer < autoSkipTime && !skipRequested)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        EndIntro();
    }

    private IEnumerator ShowParagraph(string fullText)
    {
        if (loreText == null) yield break;

        loreText.text = fullText;
        loreText.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            loreText.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);

            // Check for skip input
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame && !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                skipRequested = true;
                yield break;
            }

            yield return null;
        }

        loreText.alpha = 1f;
    }

    private IEnumerator FadeInText(TextMeshProUGUI text, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            text.alpha = Mathf.Lerp(0f, 0.5f, elapsed / duration);
            yield return null;
        }
    }

    private void Update()
    {
        // Skip on any key press (except Escape)
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame && !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            skipRequested = true;
        }
    }

    private void EndIntro()
    {
        StartCoroutine(FadeOutAndFinish());
    }

    private IEnumerator FadeOutAndFinish()
    {
        if (loreCanvasGroup == null)
        {
            FinishIntro();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            loreCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        loreCanvasGroup.alpha = 0f;
        loreCanvasGroup.blocksRaycasts = false;
        FinishIntro();
    }

    private void FinishIntro()
    {
        gameObject.SetActive(false);
        onIntroDone?.Invoke();
    }
}
