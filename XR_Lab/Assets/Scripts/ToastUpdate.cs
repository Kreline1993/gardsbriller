using UnityEngine;
using System.Collections;
using TMPro; // remove if using legacy Text

public class ToastUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText; // or UnityEngine.UI.Text
    [SerializeField] private float duration = 2f;

    private Coroutine currentToast;

    public void ShowToast(string message)
    {
        if (currentToast != null) StopCoroutine(currentToast);
        gameObject.SetActive(true);
        currentToast = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
        currentToast = null;
    }
}