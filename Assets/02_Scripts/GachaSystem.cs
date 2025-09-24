using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using SQLite;

/// <summary>
/// GachaSystem — tira de personajes con vídeo de rareza y retrato full-width,
/// permite skip del vídeo al tocar y pulsa retrato para ocultar.
/// </summary>
public class GachaSystem : MonoBehaviour
{
    [Header("DB & Economía")]
    public int rollCost = 25;
    private GameDatabase db;
    private System.Random rng;

    [Header("Probabilidades")]
    [Range(0, 1f)] public float p3 = 0.65f;
    [Range(0, 1f)] public float p4 = 0.34f;
    [Range(0, 1f)] public float p5 = 0.01f;

    [Header("Vídeos (Resources/Video/)")]
    public string pathE3 = "Video/E3";
    public string pathE4 = "Video/E4";
    public string pathE5 = "Video/E5";

    [Header("UI Inspector (opcional)")]
    public Button rollButton;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI infoText;

    // — elementos dinámicos —
    private Canvas uiCanvas;
    private RawImage videoRaw;
    private VideoPlayer vp;
    private Image portraitImage;
    private TextMeshProUGUI nameText, rarityText;

    // flag para skip
    private bool skipRequested = false;

    void Awake()
    {
        rng = new System.Random(Environment.TickCount);
        ClampProbabilities();

        if (db == null) db = new GameDatabase();

        CreateUICanvas();
        CreateVideoPlayer();
        CreateResultUI();
    }

    void Start()
    {
        RefreshGoldUI();
        ClearInfo();
        if (rollButton) rollButton.onClick.AddListener(Roll);
    }

    void Update()
    {
        bool tapped = Input.GetMouseButtonDown(0)
                   || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        // si retrato visible, ocúltalo
        if (portraitImage.enabled && tapped)
        {
            portraitImage.enabled = false;
            nameText.text = "";
            rarityText.text = "";
        }
        // si vídeo visible y tap, solicita skip
        else if (videoRaw.gameObject.activeSelf && tapped)
        {
            skipRequested = true;
            if (vp.isPlaying) vp.Stop();
            videoRaw.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (rollButton) rollButton.onClick.RemoveListener(Roll);
        if (vp != null) vp.loopPointReached -= OnVideoFinished;
    }

    public void Init(GameDatabase externalDb)
    {
        db = externalDb ?? new GameDatabase();
        RefreshGoldUI();
    }

    void ClampProbabilities()
    {
        float sum = p3 + p4 + p5;
        if (Mathf.Approximately(sum, 1f)) return;
        if (sum <= 0f) { p3 = .65f; p4 = .34f; p5 = .01f; return; }
        p3 /= sum; p4 /= sum; p5 /= sum;
    }

    #region UI + VideoPlayer Dinámicos

    void CreateUICanvas()
    {
        var go = new GameObject("GachaUICanvas");
        uiCanvas = go.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.AddComponent<GraphicRaycaster>();
    }

    void CreateVideoPlayer()
    {
        // RawImage full-screen para vídeo
        var rawGo = new GameObject("VideoDisplay", typeof(RawImage));
        rawGo.transform.SetParent(uiCanvas.transform, false);
        videoRaw = rawGo.GetComponent<RawImage>();
        var rt = rawGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // VideoPlayer + audio
        var vpGo = new GameObject("VideoPlayer");
        vp = vpGo.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.renderMode = VideoRenderMode.RenderTexture;
        var targetRT = new RenderTexture(Screen.width, Screen.height, 0);
        vp.targetTexture = targetRT;
        videoRaw.texture = targetRT;
        var audio = vpGo.AddComponent<AudioSource>();
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.SetTargetAudioSource(0, audio);

        vp.loopPointReached += OnVideoFinished;
        videoRaw.gameObject.SetActive(false);
    }

    void CreateResultUI()
    {
        // Portrait full-width
        var imgGo = new GameObject("Portrait", typeof(Image));
        imgGo.transform.SetParent(uiCanvas.transform, false);
        portraitImage = imgGo.GetComponent<Image>();
        var prt = imgGo.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        portraitImage.preserveAspect = true;
        portraitImage.enabled = false;

        // Name & rarity
        nameText = CreateTMP("NameText", new Vector2(0, -Screen.height * .45f));
        rarityText = CreateTMP("RarityText", new Vector2(0, -Screen.height * .4f));
    }

    TextMeshProUGUI CreateTMP(string objName, Vector2 anchoredPos)
    {
        var go = new GameObject(objName, typeof(TextMeshProUGUI));
        go.transform.SetParent(uiCanvas.transform, false);
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.fontSize = 32;
        txt.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Screen.width * .8f, 50);
        rt.anchoredPosition = anchoredPos;
        return txt;
    }

    #endregion

    #region Lógica de Tirada

    public void Roll()
    {
        if (!db.SpendGoldIfPossible(rollCost))
        {
            SetInfo($"No tienes suficiente oro. Necesitas {rollCost}.");
            return;
        }

        int rarity = SampleRarity();
        var pool = db.GetCharactersByRarity(rarity);
        if (pool.Count == 0)
        {
            db.AddGold(rollCost);
            RefreshGoldUI();
            SetInfo($"No hay personajes de rareza {rarity}.");
            return;
        }

        var picked = pool[rng.Next(pool.Count)];
        rollButton.interactable = false;
        StartCoroutine(PlayThenShow(rarity, picked));
    }

    int SampleRarity()
    {
        float r = (float)rng.NextDouble();
        if (r < p5) return 5;
        if (r < p5 + p4) return 4;
        return 3;
    }

    IEnumerator PlayThenShow(int rarity, Character picked)
    {
        skipRequested = false;

        // — Vídeo —
        string path = rarity == 5 ? pathE5 : (rarity == 4 ? pathE4 : pathE3);
        var clip = Resources.Load<VideoClip>(path);
        if (clip != null)
        {
            videoRaw.gameObject.SetActive(true);
            portraitImage.enabled = false;
            vp.clip = clip;
            vp.Prepare();
            yield return new WaitUntil(() => vp.isPrepared);
            vp.Play();
            yield return new WaitUntil(() => vp.isPlaying || skipRequested);
            yield return new WaitUntil(() => !vp.isPlaying);
        }

        // — Retrato full-screen —
        var spr = Resources.Load<Sprite>($"Portraits/{picked.Id}");
        videoRaw.gameObject.SetActive(false);
        if (spr != null)
        {
            portraitImage.sprite = spr;
            portraitImage.enabled = true;
        }

        // — Texto —
        nameText.text = picked.Name;
        rarityText.text = new string('*', picked.Rarity);

        // — Guardar y UI —
        db.IncrementOwned(picked.Name, picked.Rarity, 1);
        RefreshGoldUI();
        rollButton.interactable = true;
    }

    #endregion

    #region Callbacks & Helpers

    private void OnVideoFinished(VideoPlayer src)
    {
        videoRaw.gameObject.SetActive(false);
    }

    void RefreshGoldUI()
    {
        if (goldText != null)
            goldText.text = $"Gold: {db.GetGold()}";
    }

    void SetInfo(string msg)
    {
        if (infoText != null) infoText.text = msg;
        else Debug.Log(msg);
    }

    void ClearInfo() => SetInfo("");

    #endregion
}
