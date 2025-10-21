using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels & UI")]
    [SerializeField] GameObject creditPanel;
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject leftMenu;
    [SerializeField] GameObject title;

    void Awake()
    {
        if (creditPanel) creditPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
    }

    // === Buttons ===
    public void PlayGame()
    {
        // kalau belum punya scene Game, sementara log saja:
        // Debug.Log("Play clicked");
        SceneManager.LoadScene("Game");
    }

    public void OpenCredit()
    {
        if (leftMenu) leftMenu.SetActive(false);
        if (title)    title.SetActive(false);
        if (creditPanel) creditPanel.SetActive(true);
    }

    public void CloseCredit()
    {
        if (creditPanel) creditPanel.SetActive(false);
        if (leftMenu) leftMenu.SetActive(true);
        if (title)    title.SetActive(true);
    }

    public void OpenOptions()
    {
        if (leftMenu) leftMenu.SetActive(false);
        if (title)    title.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (leftMenu) leftMenu.SetActive(true);
        if (title)    title.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quit game");
        Application.Quit();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (creditPanel && creditPanel.activeSelf) CloseCredit();
            else if (optionsPanel && optionsPanel.activeSelf) CloseOptions();
        }
    }
}
