using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameServerClient : MonoBehaviour
{

    [Header("Server")]
    [SerializeField]
    private string baseUrl = "https://localhost:7031";


    [Header("Login UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button loginButton;
    [SerializeField] private String gameSceneName;

    [Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class LoginResponse
    {
        public bool success;
        public string message;
        public long userId;
    }

    private void Start()
    {
        statusText.text = string.Empty;
    }

    public void TestConnection()
    {
        StartCoroutine(TestConnectionCoroutine());
    }

    private IEnumerator TestConnectionCoroutine()
    {
        string url = baseUrl + "/api/test";

        using (UnityWebRequest request =
               UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                Debug.Log("服务器连接成功！");
                Debug.Log(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError(
                    "服务器连接失败：" + request.error);
            }
        }
    }

    public void Login()
    {
        string username = usernameInput.text.Trim();
        //string username = "player001";
        string password = passwordInput.text;
        //string password = "123456";

        if (string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter your username and password.";
            return;
        }

        StartCoroutine(LoginCoroutine(username, password));
    }

    private IEnumerator LoginCoroutine(
        string username, string password)
    {
        loginButton.interactable = false;
        statusText.text = "Connecting to the server...";

        LoginRequest data = new LoginRequest
        {
            username = username,
            password = password
        };

        string json = JsonUtility.ToJson(data);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(
            baseUrl + "/api/auth/login", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LoginResponse response =
                    JsonUtility.FromJson<LoginResponse>(
                        request.downloadHandler.text);

                if (response.success)
                {
                    statusText.text = "Login succeeded";

                    Debug.Log("User ID: " + response.userId);

                    PlayerPrefs.SetInt("UserId", (int)response.userId);

                    if (string.IsNullOrWhiteSpace(gameSceneName))
                    {
                        Debug.LogError("Game Scene Name is empty. Please set it in Inspector.");
                        yield break;
                    }

                    // Jump to game scene
                    SceneManager.LoadScene(gameSceneName);
                }
                else
                {
                    statusText.text = response.message;
                }
            }
            else if (request.responseCode == 401)
            {
                statusText.text = "Incorrect account or password";
            }
            else if (request.responseCode == 400)
            {
                statusText.text = "Parameter error";
            }
            else
            {
                statusText.text =
                    "Connecting failed. " + request.error;

                Debug.LogError(
                    request.responseCode + ": " + request.downloadHandler.text);
            }
        }

        loginButton.interactable = true;
    }

    public void showURL()
    {
        Debug.Log(baseUrl.ToString());
    }
}
