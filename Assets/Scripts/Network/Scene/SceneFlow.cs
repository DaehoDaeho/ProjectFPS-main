using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 로비에서 START 신호를 받으면 Game_Main 씬으로 전환.
/// </summary>
[DisallowMultipleComponent]
public class SceneFlow : MonoBehaviour
{
    public string gameSceneName = "Game_Main";   // 전환할 게임 씬 이름

    private void OnEnable()
    {
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.onStartSignal += OnStartSignal;
        }
    }

    private void OnDisable()
    {
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.onStartSignal -= OnStartSignal;
        }
    }

    private void OnStartSignal()
    {
        // START 신호 수신 시 게임 씬으로 로드
        SceneManager.LoadScene(gameSceneName);
    }
}
