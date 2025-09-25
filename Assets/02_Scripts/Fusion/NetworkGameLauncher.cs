using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkGameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Prefabs de Red")]
    public NetworkObject playerPrefab;

    [Header("Escena de juego (Build Index)")]
    [Tooltip("Build Index de la escena multiplayer (agrega la escena a Build Settings)")]
    public int gameplaySceneBuildIndex = 1; // pon aquí el índice real en File > Build Settings

    [Header("Sesión")]
    public int maxPlayers = 4;

    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneMgr;

    // opcional si usas tu generador procedural
    private RogueLikeMiniMazesFusion _mapGenerator;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // ========================== API pública ==========================

    public async Task StartHost(string sessionName) => await StartRunner(GameMode.Host, sessionName);
    public async Task StartClientAndJoin(string sessionName) => await StartRunner(GameMode.Client, sessionName);

    public async Task QuickJoinOrCreate(string sessionNameIfCreate = "Room-01")
    {
        if (_runner != null) return;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        _sceneMgr = gameObject.AddComponent<NetworkSceneManagerDefault>();
        _runner.AddCallbacks(this);

        // 1) Intentar quick-join
        var quick = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = null,             // null => quick join
            SceneManager = _sceneMgr
        });

        if (!quick.Ok)
        {
            // 2) Crear host si no había sala
            var create = await _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = sessionNameIfCreate,
                SceneManager = _sceneMgr,
                SessionProperties = NewSessionProps()
            });

            if (!create.Ok)
            {
                Debug.LogError($"[Launcher] Falló crear Host: {create.ShutdownReason}");
                return;
            }
        }

        TryLoadGameplayScene(); // <<< cambio de escena sincronizado
    }

    // ========================== Internos ==========================

    private async Task StartRunner(GameMode mode, string sessionName)
    {
        if (_runner != null) return;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        _sceneMgr = gameObject.AddComponent<NetworkSceneManagerDefault>();
        _runner.AddCallbacks(this);

        var args = new StartGameArgs
        {
            GameMode = mode,
            SessionName = string.IsNullOrWhiteSpace(sessionName) ? "Room-01" : sessionName,
            SceneManager = _sceneMgr,
            SessionProperties = (mode == GameMode.Host) ? NewSessionProps() : null
        };

        var result = await _runner.StartGame(args);
        if (!result.Ok)
        {
            Debug.LogError($"[Launcher] StartGame falló: {result.ShutdownReason}");
            return;
        }

        Debug.Log($"[Launcher] Runner como {mode} en '{args.SessionName}'.");
        TryLoadGameplayScene(); // <<< cambio de escena sincronizado
    }

    private Dictionary<string, SessionProperty> NewSessionProps() => new() {
        { "MaxPlayers", (SessionProperty)maxPlayers },
        { "Build",      (SessionProperty)Application.version }
    };

    private void TryLoadGameplayScene()
    {
        if (_runner == null) return;

        // Solo autoridad de escena (Host/Master Client) puede ordenar el load
        if (_runner.IsSceneAuthority == false)
            return;

        if (gameplaySceneBuildIndex < 0)
        {
            Debug.LogWarning("[Launcher] gameplaySceneBuildIndex inválido. Revisa Build Settings.");
            return;
        }

        // Fusion 2: usar LoadScene + SceneRef
        var sceneRef = SceneRef.FromIndex(gameplaySceneBuildIndex);
        _runner.LoadScene(sceneRef, LoadSceneMode.Single);
    }

    // ========================== Callbacks Fusion 2 ==========================

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[Launcher] Desconectado: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[Launcher] Conexión fallida: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogWarning($"[Launcher] Shutdown: {shutdownReason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Host puede inicializar cosas de la escena cargada
        if (runner.IsServer)
        {
            _mapGenerator = FindObjectOfType<RogueLikeMiniMazesFusion>(includeInactive: true);
            if (_mapGenerator != null)
                _mapGenerator.HostBroadcastSeedAndGenerate(); // opcional
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        if (playerPrefab == null)
        {
            Debug.LogError("[Launcher] Falta playerPrefab (y registrarlo en NetworkProjectConfig > Prefabs).");
            return;
        }

        Vector3 spawn = Vector3.zero;
        var map = _mapGenerator ?? FindObjectOfType<RogueLikeMiniMazesFusion>(includeInactive: true);
        if (map != null && map.HasValidStart)
            spawn = map.GetSafePlayerSpawnWorld();

        runner.Spawn(playerPrefab, spawn, Quaternion.identity, player);
        Debug.Log($"[Launcher] Spawn player {player} en {spawn}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Dirección por defecto
        Vector2 move = Vector2.zero;

        // Joystick de la UI local (solo existe en el cliente local)
        var js = UnityEngine.Object.FindFirstObjectByType<SimpleJoystick>();
        if (js != null)
        {
            move = js.Direction;                // -1..1
        }
        else
        {
            // Fallback teclado (útil en editor/PC)
            move.x = (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0);
            move.y = (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0);
            if (move.sqrMagnitude > 1f) move.Normalize();
        }

        // Botones opcionales (si quieres disparar dash/ultimate desde teclado)
        bool dash = Input.GetKey(KeyCode.LeftShift);
        bool ult = Input.GetKeyDown(KeyCode.Q);

        var data = new PlayerInputData
        {
            move = move,
            dash = dash,
            ultimate = ult
        };

        input.Set(data);
    }


    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
}
