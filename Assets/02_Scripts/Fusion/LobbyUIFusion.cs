using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIFusion : MonoBehaviour
{
    [Header("Referencias")]
    public NetworkGameLauncher launcher;  // arrástralo desde NetworkSystems
    public Canvas mainCanvas;             // tu Canvas principal

    [Header("UI")]
    public Button hostButton;
    public Button joinButton;
    public Button quickJoinButton;

    [Header("Opciones")]
    public string defaultRoomName = "Room-01";

    private TMP_InputField roomNameInputInstance;
    private GameObject inputBackground;

    void Awake()
    {
        if (launcher == null) launcher = FindObjectOfType<NetworkGameLauncher>(includeInactive: true);
        if (mainCanvas == null) mainCanvas = FindObjectOfType<Canvas>(includeInactive: true);
    }

    void Start()
    {
        if (hostButton)
        {
            hostButton.onClick.RemoveAllListeners();
            hostButton.onClick.AddListener(async () => await Host());
        }

        if (joinButton)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinButtonPressed);
        }

        if (quickJoinButton)
        {
            quickJoinButton.onClick.RemoveAllListeners();
            quickJoinButton.onClick.AddListener(async () => await QuickJoin());
        }
    }

    // ===== Flujo Join =====
    private void OnJoinButtonPressed()
    {
        if (roomNameInputInstance == null)
        {
            GenerateRoomNameInput();
            return;
        }
        _ = Join();
    }

    private void GenerateRoomNameInput()
    {
        if (mainCanvas == null)
        {
            Debug.LogError("[LobbyUI] No hay Canvas. Asigna mainCanvas.");
            return;
        }

        // Fondo
        inputBackground = new GameObject("RoomNameBackground");
        inputBackground.transform.SetParent(mainCanvas.transform, false);

        var bgRect = inputBackground.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(250, 50);
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(0, -80);

        var bgImage = inputBackground.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);

        // Input
        var inputGO = new GameObject("RoomNameInput");
        inputGO.transform.SetParent(inputBackground.transform, false);

        var rect = inputGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(230, 40);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        roomNameInputInstance = inputGO.AddComponent<TMP_InputField>();

        // Texto
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(inputGO.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        roomNameInputInstance.textComponent = text;

        // Placeholder
        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(inputGO.transform, false);
        var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholder.fontSize = 22;
        placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.color = new Color(1f, 1f, 1f, 0.4f);
        placeholder.text = "Escribe el nombre de la sala";
        var placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = placeholderRect.offsetMax = Vector2.zero;
        roomNameInputInstance.placeholder = placeholder;

        roomNameInputInstance.characterLimit = 20;
        roomNameInputInstance.contentType = TMP_InputField.ContentType.Standard;
    }

    private string RoomNameOrDefault()
    {
        string name = roomNameInputInstance ? roomNameInputInstance.text : null;
        return string.IsNullOrWhiteSpace(name) ? defaultRoomName : name.Trim();
    }

    // ===== Acciones =====
    private async Task Host()
    {
        if (launcher == null) { Debug.LogError("[LobbyUI] Falta NetworkGameLauncher."); return; }
        SetInteractable(false);
        await launcher.StartHost(RoomNameOrDefault());
        SetInteractable(true);
    }

    private async Task Join()
    {
        if (launcher == null) { Debug.LogError("[LobbyUI] Falta NetworkGameLauncher."); return; }
        if (roomNameInputInstance == null) { Debug.LogWarning("[LobbyUI] Pulsa Join una vez para crear el campo."); return; }
        SetInteractable(false);
        await launcher.StartClientAndJoin(RoomNameOrDefault());
        SetInteractable(true);
    }

    private async Task QuickJoin()
    {
        if (launcher == null) { Debug.LogError("[LobbyUI] Falta NetworkGameLauncher."); return; }
        SetInteractable(false);
        await launcher.QuickJoinOrCreate(RoomNameOrDefault());
        SetInteractable(true);
    }

    private void SetInteractable(bool value)
    {
        if (hostButton) hostButton.interactable = value;
        if (joinButton) joinButton.interactable = value;
        if (quickJoinButton) quickJoinButton.interactable = value;
        if (roomNameInputInstance) roomNameInputInstance.interactable = value;
    }
}
