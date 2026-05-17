using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private string gameSceneName = "GameScene";

    private void OnEnable() => hostBtn.onClick.AddListener(OnHost);
    private void OnDisable() => hostBtn.onClick.RemoveListener(OnHost);

    private void OnHost() => SceneManager.LoadScene(gameSceneName);
}
