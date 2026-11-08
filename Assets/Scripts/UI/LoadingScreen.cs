using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 全屏加载界面：显示进度条，异步加载目标场景，读条完成后再切换。
/// UI 在运行时代码生成，无需在场景中摆放。
/// 用法：LoadingScreen.Load("SampleScene");
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    private const float MinShowSeconds = 1.0f;   // 最短显示时间，避免一闪而过
    private const float FillSpeed = 1.5f;        // 进度条追赶速度（每秒）

    private Image fillImage;
    private TMP_Text percentText;
    private TMP_Text titleText;

    public static void Load(string sceneName)
    {
        var go = new GameObject("LoadingScreen");
        DontDestroyOnLoad(go);
        var screen = go.AddComponent<LoadingScreen>();
        screen.BuildUI();
        screen.StartCoroutine(screen.LoadRoutine(sceneName));
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>(); // 挡住下方的点击

        // 背景
        var bg = CreateImage("Background", transform, new Color(0.05f, 0.06f, 0.09f, 1f));
        Stretch(bg.rectTransform);

        // 标题
        titleText = CreateText("Title", transform, "Loading...", 48);
        SetRect(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 80));

        // 进度条底
        var barBg = CreateImage("BarBg", transform, new Color(1f, 1f, 1f, 0.15f));
        SetRect(barBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1000, 16));

        // 进度条填充
        fillImage = CreateImage("Fill", barBg.transform, new Color(0.35f, 0.75f, 1f, 1f));
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 0f;
        Stretch(fillImage.rectTransform);

        // 百分比
        percentText = CreateText("Percent", transform, "0%", 32);
        SetRect(percentText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(400, 50));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        // 等一帧让加载界面先渲染出来，再开始加载
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError("LoadingScreen: cannot load scene '" + sceneName + "'. Is it in Build Settings?");
            Destroy(gameObject);
            yield break;
        }
        op.allowSceneActivation = false;

        float shown = 0f;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            // Unity 异步加载最多到 0.9，换算成 0~1
            float target = Mathf.Clamp01(op.progress / 0.9f);
            shown = Mathf.MoveTowards(shown, target, FillSpeed * Time.unscaledDeltaTime);

            fillImage.fillAmount = shown;
            percentText.text = Mathf.RoundToInt(shown * 100f) + "%";

            if (shown >= 1f && elapsed >= MinShowSeconds)
                break;

            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // 新场景已激活，等一帧后销毁加载界面
        yield return null;
        Destroy(gameObject);
    }

    // ---------- UI 辅助 ----------

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
