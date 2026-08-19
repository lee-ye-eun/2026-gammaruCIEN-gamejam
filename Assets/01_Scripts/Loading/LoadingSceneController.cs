using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// LoadingScene에 배치. GameManager.Instance.PendingSceneName을 비동기로 로드하면서
// progressBar(Image, Type=Filled)로 진행률을 보여준다.
public class LoadingSceneController : MonoBehaviour
{
    [Header("진행도 UI")]
    [SerializeField] private Image progressBar; // Image 컴포넌트의 Image Type을 Filled로 설정

    [Header("옵션")]
    [SerializeField] private float minDisplayDuration = 0.5f; // 로딩이 너무 빨리 끝나도 최소 이만큼은 보여줌

    private void Start()
    {
        StartCoroutine(LoadTargetSceneAsync());
    }

    private IEnumerator LoadTargetSceneAsync()
    {
        SetProgress(0f);

        string targetScene = GameManager.Instance != null ? GameManager.Instance.PendingSceneName : null;
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("LoadingSceneController: 이동할 씬 이름이 없습니다.");
            yield break;
        }

        float elapsed = 0f;
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        // Unity는 씬 로드가 끝나도 activation 직전까지 progress를 0.9에서 멈춰둔다
        while (operation.progress < 0.9f)
        {
            elapsed += Time.deltaTime;
            SetProgress(operation.progress / 0.9f);
            yield return null;
        }

        SetProgress(1f);

        if (elapsed < minDisplayDuration)
        {
            yield return new WaitForSeconds(minDisplayDuration - elapsed);
        }

        operation.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        if (progressBar != null) progressBar.fillAmount = Mathf.Clamp01(value);
    }
}
