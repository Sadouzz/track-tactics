using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.MultiplayerAgent.Model;
using Mirror;
using PlayFab.Networking;
using System;

public class ServerStartUp : MonoBehaviour
{
    [Header("Configuration")]
    public Configuration configuration;
    public float heartbeatInterval = 30f;
    public float shutdownTimeout = 300f;
    public float standbyTimeout = 300f;

    [Header("Dependencies")]
    public UnityNetworkServer UNetServer;
    public NetworkManager networkManager;

    private List<ConnectedPlayer> _connectedPlayers;
    private Coroutine _shutdownCoroutine;
    private Coroutine _heartbeatCoroutine;
    private bool _isInStandby = false;

    // AJOUT: Référence au CustomNetworkManager pour connaître l'état du jeu
    private CustomNetworkManager customNetworkManager;

    void Start()
    {
        // Trouver le CustomNetworkManager
        customNetworkManager = FindObjectOfType<CustomNetworkManager>();

        if (configuration.buildType == BuildType.REMOTE_SERVER)
        {
            StartRemoteServer();
        }
        else if (configuration.buildType == BuildType.LOCAL_SERVER)
        {
            networkManager.StartServer();
        }
    }

    private void StartRemoteServer()
    {
        Debug.Log("[ServerStartUp] Initializing PlayFab Multiplayer Agent");
        _connectedPlayers = new List<ConnectedPlayer>();

        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = configuration.playFabDebugging;

        // Register callbacks
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

        // MODIFICATION: Ne pas écouter les événements UNetServer automatiquement
        UNetServer.OnPlayerAdded.AddListener(OnPlayerAdded);
        UNetServer.OnPlayerRemoved.AddListener(OnPlayerRemoved);

        StartCoroutine(ReadyForPlayers());
    }

    // NOUVELLE MÉTHODE: Appelée manuellement par le CustomNetworkManager
    public void ManualPlayerAdded(string playfabId)
    {
        OnPlayerAdded(playfabId);
    }

    // NOUVELLE MÉTHODE: Appelée manuellement par le CustomNetworkManager
    public void ManualPlayerRemoved(string playfabId)
    {
        OnPlayerRemoved(playfabId);
    }

    private IEnumerator ReadyForPlayers()
    {
        yield return new WaitForSeconds(0.5f);
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
        Debug.Log("[ServerStartUp] Server ready for players");
    }

    private IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(heartbeatInterval);

            // Heartbeat toujours envoyé
            PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
            Debug.Log($"[PlayFab Heartbeat] Players: {_connectedPlayers.Count} | Standby: {_isInStandby}");

            // Afficher les joueurs connectés
            foreach (var player in _connectedPlayers)
            {
                Debug.Log($"  - Player: {player.PlayerId}");
            }

            // Gestion standby avec timer séparé
            if (_connectedPlayers.Count == 0 && !_isInStandby)
            {
                StartCoroutine(StandbyCountdown());
            }
            else if (_connectedPlayers.Count > 0 && _isInStandby)
            {
                ExitStandbyMode();
            }
        }
    }

    private IEnumerator StandbyCountdown()
    {
        yield return new WaitForSeconds(standbyTimeout);

        if (_connectedPlayers.Count == 0 && !_isInStandby)
        {
            EnterStandbyMode();
        }
    }

    private void EnterStandbyMode()
    {
        _isInStandby = true;
        Debug.Log("[ServerStartUp] Entering standby mode");

        // Optional: Reduce server activity
        networkManager.StopServer();

        // Notify PlayFab
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(new List<ConnectedPlayer>());
    }

    private void ExitStandbyMode()
    {
        _isInStandby = false;
        Debug.Log("[ServerStartUp] Exiting standby mode");

        // Restart server if needed
        if (!networkManager.isNetworkActive)
        {
            networkManager.StartServer();
        }
    }

    private void OnServerActive()
    {
        Debug.Log("[ServerStartUp] Server activated - configuring network");
        _heartbeatCoroutine = StartCoroutine(HeartbeatLoop());

        // Configure network ports
        var connectionInfo = PlayFabMultiplayerAgentAPI.GetGameServerConnectionInfo();
        if (connectionInfo != null)
        {
            foreach (var port in connectionInfo.GamePortsConfiguration)
            {
                configuration.port = (ushort)port.ServerListeningPort;
                Debug.Log($"[ServerStartUp] Using assigned port: {configuration.port}");
                break;
            }
        }

        // Initialize transport
        if (networkManager.GetComponent<TelepathyTransport>() is TelepathyTransport telepathy)
        {
            telepathy.port = configuration.port;
        }

        // Start server
        networkManager.StartServer();

        // Start shutdown timer
        _shutdownCoroutine = StartCoroutine(ShutdownCountdown());
    }

    private IEnumerator ShutdownCountdown()
    {
        yield return new WaitForSeconds(shutdownTimeout);

        if (_connectedPlayers.Count == 0)
        {
            Debug.Log("[ServerStartUp] No players connected - initiating shutdown");
            StartShutdownProcess();
        }
        else
        {
            _shutdownCoroutine = StartCoroutine(ShutdownCountdown());
        }
    }

    private void OnPlayerAdded(string playfabId)
    {
        Debug.Log($"[PlayFab] Player {playfabId} added to PlayFab tracking");

        _connectedPlayers.Add(new ConnectedPlayer(playfabId));

        // Notifier PlayFab immédiatement
        try
        {
            PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
            Debug.Log($"[PlayFab] Reported {_connectedPlayers.Count} players to PlayFab");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayFab] Error reporting players: {e.Message}");
        }
    }

    private void OnPlayerRemoved(string playfabId)
    {
        Debug.Log($"[PlayFab] Player {playfabId} removed from PlayFab tracking");

        _connectedPlayers.RemoveAll(p => p.PlayerId == playfabId);

        // Notifier PlayFab immédiatement
        try
        {
            PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
            Debug.Log($"[PlayFab] Reported {_connectedPlayers.Count} players to PlayFab");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayFab] Error reporting players: {e.Message}");
        }
    }

    // Méthodes existantes inchangées...
    private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
    {
        Debug.Log($"[ServerStartUp] Maintenance scheduled: {NextScheduledMaintenanceUtc}");
        foreach (var conn in UNetServer.Connections)
        {
            conn.Connection.Send(new MaintenanceMessage()
            {
                ScheduledMaintenanceUTC = (DateTime)NextScheduledMaintenanceUtc
            });
        }
    }

    private void OnShutdown()
    {
        StartShutdownProcess();
    }

    private void OnAgentError(string error)
    {
        Debug.LogError($"[ServerStartUp] Agent error: {error}");
    }

    private void StartShutdownProcess()
    {
        Debug.Log("[ServerStartUp] Starting shutdown process");
        foreach (var conn in UNetServer.Connections)
        {
            conn.Connection.Send(new ShutdownMessage());
        }
        StartCoroutine(ShutdownServer());
    }

    private IEnumerator ShutdownServer()
    {
        yield return new WaitForSeconds(5f);
        Application.Quit();
    }

    private void OnDestroy()
    {
        // Clean up callbacks
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback -= OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback -= OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback -= OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback -= OnAgentError;

        if (_shutdownCoroutine != null)
            StopCoroutine(_shutdownCoroutine);
    }

    public void OnStartLocalServerButtonClick()
    {
        if (configuration.buildType == BuildType.LOCAL_SERVER)
        {
            networkManager.StartServer();
        }
    }

    // Méthode pour débugger le nombre de joueurs
    public int GetConnectedPlayerCount()
    {
        return _connectedPlayers.Count;
    }
}