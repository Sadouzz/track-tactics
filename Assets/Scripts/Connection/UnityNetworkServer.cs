namespace PlayFab.Networking
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Mirror;
    using UnityEngine.Events;

    public class UnityNetworkServer : NetworkBehaviour
    {
        public Configuration configuration;

        public PlayerEvent OnPlayerAdded = new PlayerEvent();
        public PlayerEvent OnPlayerRemoved = new PlayerEvent();

        public int MaxConnections = 100;
        public int Port = 7777;

        public NetworkManager _networkManager;

        public List<UnityNetworkConnection> Connections
        {
            get { return _connections; }
            private set { _connections = value; }
        }
        private List<UnityNetworkConnection> _connections = new List<UnityNetworkConnection>();

        public class PlayerEvent : UnityEvent<string> { }

        void Awake()
        {
            if (configuration.buildType == BuildType.REMOTE_SERVER)
            {
                AddRemoteServerListeners();
            }
        }

        private void AddRemoteServerListeners()
        {
            Debug.Log("[UnityNetworkServer].AddRemoteServerListeners");

            // IMPORTANT: Ajouter les listeners Mirror
            NetworkServer.OnConnectedEvent += OnServerConnect;
            NetworkServer.OnDisconnectedEvent += OnServerDisconnect;

            NetworkServer.RegisterHandler<ReceiveAuthenticateMessage>(OnReceiveAuthenticate);
        }

        public void StartServer()
        {
            NetworkServer.Listen(MaxConnections);
        }

        private void OnApplicationQuit()
        {
            NetworkServer.Shutdown();
        }

        private void OnReceiveAuthenticate(NetworkConnectionToClient nconn, ReceiveAuthenticateMessage message)
        {
            Debug.Log($"[UnityNetworkServer] Received authentication for PlayFabId: {message.PlayFabId}");

            var conn = _connections.Find(c => c.Connection == nconn);
            if (conn != null)
            {
                conn.PlayFabId = message.PlayFabId;
                conn.IsAuthenticated = true;

                Debug.Log($"Player {message.PlayFabId} authenticated successfully");

                // IMPORTANT: Ne pas créer le joueur ici - laissez CustomNetworkManager s'en charger
                // Ne pas appeler NetworkServer.AddPlayerForConnection ici !

                // Optionnel : déclencher un événement si nécessaire
                // OnPlayerAdded.Invoke(message.PlayFabId);
            }
            else
            {
                Debug.LogError($"[UnityNetworkServer] Connection not found for authentication: {message.PlayFabId}");
            }
        }

        public int GetAuthenticatedPlayersCount()
        {
            return _connections.FindAll(c => c.IsAuthenticated).Count;
        }

        private void OnServerConnect(NetworkConnectionToClient connection)
        {
            Debug.Log($"[UnityNetworkServer] Client Connected: {connection.connectionId}");

            var existingConn = _connections.Find(c => c.Connection == connection);
            if (existingConn == null)
            {
                var newConn = new UnityNetworkConnection()
                {
                    Connection = connection,
                    ConnectionId = connection.connectionId,
                    LobbyId = PlayFabMultiplayerAgentAPI.SessionConfig?.SessionId ?? "LocalSession",
                    IsAuthenticated = false,
                    PlayFabId = null
                };

                _connections.Add(newConn);
                Debug.Log($"[UnityNetworkServer] Added connection {connection.connectionId} to tracking");
            }
        }

        private void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            Debug.Log($"[UnityNetworkServer] Client Disconnected: {connection.connectionId}");

            var conn = _connections.Find(c => c.Connection == connection);
            if (conn != null)
            {
                if (!string.IsNullOrEmpty(conn.PlayFabId))
                {
                    Debug.Log($"[UnityNetworkServer] Removing authenticated player: {conn.PlayFabId}");
                    OnPlayerRemoved.Invoke(conn.PlayFabId);
                }

                _connections.Remove(conn);
                Debug.Log($"[UnityNetworkServer] Removed connection {connection.connectionId} from tracking");
            }
        }

        private void OnServerError(NetworkConnection conn, TransportError error, string reason)
        {
            Debug.LogFormat("Unity Network Connection Status: error - {0}, reason: {1}", error.ToString(), reason);
        }

        // Méthode utilitaire pour trouver une connexion par PlayFabId
        public UnityNetworkConnection FindConnectionByPlayFabId(string playFabId)
        {
            return _connections.Find(c => c.PlayFabId == playFabId);
        }

        // Méthode utilitaire pour trouver une connexion par NetworkConnection
        public UnityNetworkConnection FindConnectionByNetworkConnection(NetworkConnectionToClient networkConn)
        {
            return _connections.Find(c => c.Connection == networkConn);
        }

        // Debug : afficher toutes les connexions
        public void LogAllConnections()
        {
            Debug.Log($"[UnityNetworkServer] Total connections: {_connections.Count}");
            foreach (var conn in _connections)
            {
                Debug.Log($"  - Connection {conn.ConnectionId}: PlayFabId={conn.PlayFabId}, Authenticated={conn.IsAuthenticated}");
            }
        }
    }

    [Serializable]
    public class UnityNetworkConnection
    {
        public bool IsAuthenticated;
        public string PlayFabId;
        public string LobbyId;
        public int ConnectionId;
        public NetworkConnection Connection;
    }

    public class CustomGameServerMessageTypes
    {
        public const short ReceiveAuthenticate = 900;
        public const short ShutdownMessage = 901;
        public const short MaintenanceMessage = 902;
    }

    public struct ReceiveAuthenticateMessage : NetworkMessage
    {
        public string PlayFabId;
    }

    public struct ShutdownMessage : NetworkMessage { }

    [Serializable]
    public struct MaintenanceMessage : NetworkMessage
    {
        public DateTime ScheduledMaintenanceUTC;
    }

    public static class MaintenanceMessageFunctions
    {
        public static MaintenanceMessage Deserialize(this NetworkReader reader)
        {
            var json = PlayFab.PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
            DateTime ScheduledMaintenanceUTC = json.DeserializeObject<DateTime>(reader.ReadString());
            MaintenanceMessage value = new MaintenanceMessage
            {
                ScheduledMaintenanceUTC = ScheduledMaintenanceUTC
            };

            return value;
        }

        public static void Serialize(this NetworkWriter writer, MaintenanceMessage value)
        {
            var json = PlayFab.PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
            var str = json.SerializeObject(value.ScheduledMaintenanceUTC);
            writer.Write(str);
        }
    }
}