// FusionBootstrap.cs
// Arranque de Fusion 2, spawn de jugadores y callbacks completos.
// - Implementa OnPlayerLeft y OnInputMissing.
// - Elimina cualquier uso de SimulationConfig.Topologies.*
// - Condición de spawn: Host/Server o GameMode.Shared.

using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class FusionBootstrap : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Config")]
    [Tooltip("Modo por defecto: crea host si no existe una sesión, o se une si ya hay.")]
    public GameMode gameMode = GameMode.AutoHostOrClient;

    [Tooltip("Nombre de la sesión. Todas las instancias deben usar el mismo para encontrarse.")]
    public string sessionName = "dev-room";

    [Header("Prefabs")]
    [Tooltip("Prefab del jugador con NetworkObject (+ opcional NetworkTransform)")]
    public NetworkObject playerPrefab;

    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneManager;

    // Mapa que ya generas de forma determinista y expone HasGenerated + GetSpawnWorldFor(PlayerRef)
    private PlaceMazeOnPlace _map;

    private async void Start()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Este cliente provee input local al Runner
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var args = new StartGameArgs
        {
            GameMode = gameMode,
            SessionName = sessionName,
            SceneManager = _sceneManager,
            Scene = SceneRef.FromIndex(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex)
        };

        var result = await _runner.StartGame(args);
        if (!result.Ok)
        {
            Debug.LogError($"Fusion StartGame falló: {result.ShutdownReason}");
            return;
        }

        _map = FindObjectOfType<PlaceMazeOnPlace>();
        if (_map == null)
            Debug.LogWarning("No se encontró PlaceMazeOnPlace en la escena.");
    }

    // =========================
    // INetworkRunnerCallbacks
    // =========================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Permitir spawn si:
        // - Somos servidor/host, o
        // - Estamos en GameMode.Shared (no hay servidor autoritativo)
        bool canSpawn = runner.IsServer || runner.GameMode == GameMode.Shared;
        if (!canSpawn) return;

        if (playerPrefab == null)
        {
            Debug.LogError("Asigna 'playerPrefab' en FusionBootstrap.");
            return;
        }

        if (_map == null) _map = FindObjectOfType<PlaceMazeOnPlace>();

        Vector3 spawnPos = new Vector3(0, 1, 0);
        Quaternion rot = Quaternion.identity;

        // Si el mapa ya está generado, pedimos un spawn estable para este jugador
        if (_map != null && _map.HasGenerated)
            spawnPos = _map.GetSpawnWorldFor(player);

        // Importante: el Host spawnea al jugador; se replica para todos los peers
        runner.Spawn(playerPrefab, spawnPos, rot, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Si necesitas limpiar objetos del jugador manualmente, hazlo aquí.
        // Normalmente los objetos con InputAuthority del player serán despawneados automáticamente.
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Único punto de lectura de input local por tick
        var data = new PlayerInputData
        {
            move = new Vector2(
                (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0)
            ),
            jump = Input.GetKey(KeyCode.Space)
        };
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // Rellenar input por defecto si falta (opcional: dejar en cero)
        input.Set(default(PlayerInputData));
    }

    // —— Callbacks no usados pero requeridos ——
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Re-ubica referencias tras cambios de escena si usas SceneManager
        _map = FindObjectOfType<PlaceMazeOnPlace>();
    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
