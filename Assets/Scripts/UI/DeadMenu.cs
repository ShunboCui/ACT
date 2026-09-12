using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeadMenu : BasePanel
{
    private Transform RestartButton;

    override protected void Awake()
    {
        base.Awake();
        InitUI();
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void InitUI()
    {
        InitUIName();
        InitClick();
    }

    private void InitUIName()
    {
        RestartButton = transform.Find("Center/Restart");
    }

    private void InitClick()
    {
        RestartButton.gameObject.GetComponent<Button>().onClick.AddListener(OnClickRestart);
    }

    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
