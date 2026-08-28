using TMPro;
using UnityEngine;

public class ClearUI : MonoBehaviour
{
    [SerializeField]
    private StageManager stageManager;

    [SerializeField]
    private TMP_Text clearText;

    private void Awake()
    {
        if (clearText == null)
        {
            Debug.LogError(
                "ClearUI의 Clear Text가 연결되지 않았습니다."
            );

            return;
        }

        clearText.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (stageManager == null)
        {
            Debug.LogError(
                "ClearUI의 StageManager가 연결되지 않았습니다."
            );

            return;
        }

        stageManager.OnStageCleared +=
            ShowClearUI;
    }

    private void OnDestroy()
    {
        if (stageManager != null)
        {
            stageManager.OnStageCleared -=
                ShowClearUI;
        }
    }

    private void ShowClearUI()
    {
        if (clearText == null)
        {
            return;
        }

        clearText.gameObject.SetActive(true);

        Debug.Log("Clear UI 출력");
    }
}