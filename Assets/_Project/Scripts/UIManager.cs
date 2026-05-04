using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text waveText;
    [SerializeField] private Text treatmentText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text combatFeedbackText;

    private Coroutine messageRoutine;
    private Coroutine feedbackRoutine;

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void SetLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {lives}";
        }
    }

    public void SetWave(int wave, int totalWaves)
    {
        if (waveText != null)
        {
            waveText.text = $"Wave: {wave}/{totalWaves}";
        }
    }

    public void SetSelectedTreatment(TreatmentType treatmentType)
    {
        if (treatmentText != null)
        {
            treatmentText.text = $"Treatment: {GetTreatmentLabel(treatmentType)}";
        }
    }

    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    public void ShowCombatFeedback(string label, Color color)
    {
        if (combatFeedbackText == null)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(FeedbackRoutine(label, color));
    }

    private IEnumerator MessageRoutine(string message)
    {
        messageText.text = message;
        messageText.enabled = true;
        yield return new WaitForSeconds(1.4f);
        messageText.enabled = false;
    }

    private IEnumerator FeedbackRoutine(string label, Color color)
    {
        combatFeedbackText.text = label;
        combatFeedbackText.color = color;
        combatFeedbackText.enabled = true;
        yield return new WaitForSeconds(0.6f);
        combatFeedbackText.enabled = false;
    }

    private static string GetTreatmentLabel(TreatmentType treatmentType)
    {
        switch (treatmentType)
        {
            case TreatmentType.ImmunoBeam:
                return "Immuno Beam";
            case TreatmentType.TargetedNano:
                return "Targeted Nano";
            default:
                return "Chemo Shot";
        }
    }
}
