using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

public class UnityAmeshWindow : EditorWindow
{
    static UnityAmeshWindow _window;

    [MenuItem("UnityAmesh/Open UnityAmesh Window")]
    private static void OpenWindow()
    {
        CheckInit();
        _window.Show();
    }

    public static void CheckInit()
    {
        if (_window == null)
        {
            _window = GetWindow<UnityAmeshWindow>("UnityAmesh");
            _window.minSize = new Vector2(640, 480);

            _window._resultTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        }
    }

    void OnEnable()
    {
        CheckInit(); // プロジェクトを開いたときに、ウインドウ状態が復元していきなりウインドウが開く場合の対策
        InitAmesh();
    }

    void OnDisable()
    {
    }

    void OnDestroy()
    {
    }

    void OnGUI()
    {
        float window_w = position.size.x;
        float window_h = position.size.y;

        if (GUILayout.Button("Reload", GUILayout.Width(120)))
        {
            ReloadAmesh();
        }

        float h = (_resultTexture.height / (float)_resultTexture.width) * window_w;
        EditorGUI.DrawPreviewTexture(new Rect(0, 20, window_w, 20 + h), _resultTexture);
    }

    ////////////////////////////////////////////////////////////

    UnityWebRequest _req;
    Texture2D _mapTexture;
    Texture2D _boarderTexture;
    Texture2D _ameshTexture;
    Texture2D _resultTexture;

    void InitAmesh()
    {
        DownloadBaseMap();
    }

    void ReloadAmesh()
    {
        DownloadAmesh();
    }

    void DownloadBaseMap()
    {
        var url = "https://tokyo-ame.jwa.or.jp/map/map000.jpg";

        _req = UnityWebRequest.Get(url);
        _req.SendWebRequest();

        EditorApplication.update += DownloadBaseMapUpdate;
    }

    void DownloadBaseMapUpdate()
    {
        if (_req == null || !_req.isDone) return;
        EditorApplication.update -= DownloadBaseMapUpdate;

        if (_req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(_req.error);
            return;
        }

        _mapTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        _mapTexture.LoadImage(_req.downloadHandler.data);

        DownloadPrefecturalBorder();
    }

    void DownloadPrefecturalBorder()
    {
        var url = "https://tokyo-ame.jwa.or.jp/map/msk000.png";

        _req = UnityWebRequest.Get(url);
        _req.SendWebRequest();

        EditorApplication.update += DownloadPrefecturalBorderUpdate;
    }

    void DownloadPrefecturalBorderUpdate()
    {
        if (_req == null || !_req.isDone) return;
        EditorApplication.update -= DownloadPrefecturalBorderUpdate;

        if (_req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(_req.error);
            return;
        }

        _boarderTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        _boarderTexture.LoadImage(_req.downloadHandler.data);

        DownloadAmesh();
    }

    void DownloadAmesh()
    {
        var base_url = "https://tokyo-ame.jwa.or.jp/mesh/000/";
        var yyyyMMddhh_str = DateTime.Now.ToString("yyyyMMddHH");
        var m = (int)DateTime.Now.Minute / (int)5;
        m = m > 0 ? m - 1 : 0;
        var mm_str = string.Format("{0:D2}", m * 5);
        var url = base_url + yyyyMMddhh_str + mm_str + ".gif"; // yyyyMMddHHmm.gif (mmは5分でquantizeされた数値)

        _req = UnityWebRequest.Get(url);
        _req.SendWebRequest();

        EditorApplication.update += DownloadAmeshUpdate;
    }

    void DownloadAmeshUpdate()
    {
        if (_req == null || !_req.isDone) return;
        EditorApplication.update -= DownloadAmeshUpdate;

        if (_req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(_req.error);
            return;
        }

        using (var decoder = new MG.GIF.Decoder(_req.downloadHandler.data))
        {
            var img = decoder.NextImage();

            while (img != null)
            {
                _ameshTexture = img.CreateTexture();
                img = decoder.NextImage();
            }
        }

        mergeTextures();
    }

    void mergeTextures()
    {
        int w = _mapTexture.width;
        int h = _mapTexture.height;
        _resultTexture = new Texture2D(w, h, TextureFormat.RGB24, false);

        for (int y = 0; y < h; ++y)
        {
            for (int x = 0; x < w; ++x)
            {
                // マップの色
                Color c_m = _mapTexture.GetPixel(x, y);
                _resultTexture.SetPixel(x, y, c_m);

                // 黒以外だったら色を配置
                Color c_a = _ameshTexture.GetPixel(x, y);
                if (!(c_a.r == 0 && c_a.b == 0 && c_a.g == 0))
                {
                    _resultTexture.SetPixel(x, y, c_a);
                }

                // 県境の色
                Color c_b = _boarderTexture.GetPixel(x, y);
                if (c_b.a > 0)
                {
                    _resultTexture.SetPixel(x, y, c_b);
                }
            }
        }
        _resultTexture.Apply(false);
    }
}
