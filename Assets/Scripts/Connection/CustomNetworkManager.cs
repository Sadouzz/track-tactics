using Mirror;
using UnityEngine;
using System.Collections;
using PlayFab;
using PlayFab.MultiplayerModels;
using Edgegap;

public class CustomNetworkManager : NetworkManager
{
    [Header("Scene Settings")]
    public string lobbyScene = "Server";
    public string gameScene = "City";

    [Header("Game Settings")]
    public int requiredPlayers = 2;

    // État du jeu
    private bool hasGameStarted = false;
    private bool isInGameScene = false;

    // Compteur pour éviter les boucles
    private int sceneChangeCount = 0;
    private const int MAX_SCENE_CHANGES = 1;

    public ServerStartUp serverStartUp;

    // Pour suivre les connexions en attente de création de joueur
    private System.Collections.Generic.HashSet<NetworkConnectionToClient> pendingPlayerCreation = new System.Collections.Generic.HashSet<NetworkConnectionToClient>();

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"OnServerAddPlayer called. Scene: {currentScene}");
        Debug.Log($"hasGameStarted: {hasGameStarted}, isInGameScene: {isInGameScene}");

        // NE PAS créer de joueur dans le lobby - juste compter les connexions
        if (currentScene == lobbyScene)
        {
            Debug.Log($"Player connected to lobby. Total connections: {NetworkServer.connections.Count}");

            // Ajouter à la liste des connexions en attente
            pendingPlayerCreation.Add(conn);

            // Logique de démarrage SEULEMENT si on n'a pas encore démarré
            if (!hasGameStarted && sceneChangeCount < MAX_SCENE_CHANGES)
            {
                CheckForGameStart();
            }

            // Ne PAS appeler base.OnServerAddPlayer() dans le lobby !
            return;
        }

        // Créer le joueur SEULEMENT dans la scène de jeu ET seulement si pas déjà créé
        if (currentScene == gameScene)
        {
            // Vérifier si cette connexion n'a pas déjà un joueur
            if (conn.identity == null)
            {
                Debug.Log($"Creating player for connection {conn.connectionId} in game scene");
                base.OnServerAddPlayer(conn);

                // Retirer de la liste des connexions en attente
                pendingPlayerCreation.Remove(conn);

                // MAINTENANT notifier PlayFab - un vrai joueur a été créé !
                NotifyPlayFabPlayerAdded(conn);
            }
            else
            {
                Debug.Log($"Connection {conn.connectionId} already has a player - skipping creation");
            }
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Nettoyer la liste des connexions en attente
        pendingPlayerCreation.Remove(conn);

        // Notifier votre système PlayFab existant
        NotifyPlayFabPlayerRemoved(conn);

        base.OnServerDisconnect(conn);

        Debug.Log($"Player disconnected. Remaining: {NetworkServer.connections.Count}");
        StartCoroutine(DelayedPlayFabUpdate());
    }

    private void NotifyPlayFabPlayerAdded(NetworkConnectionToClient conn)
    {
        if (serverStartUp != null)
        {
            // IMPORTANT: Récupérer le VRAI PlayFabId depuis UnityNetworkServer
            string realPlayFabId = GetRealPlayFabId(conn);

            if (!string.IsNullOrEmpty(realPlayFabId))
            {
                Debug.Log($"Notifying PlayFab: REAL Player {realPlayFabId} added");
                serverStartUp.ManualPlayerAdded(realPlayFabId);
            }
            else
            {
                Debug.LogWarning($"No authenticated PlayFabId found for connection {conn.connectionId}");
                // Fallback avec ID temporaire si nécessaire
                // string tempId = $"Player_{conn.connectionId}";
                // serverStartUp.ManualPlayerAdded(tempId);
            }
        }
    }

    private void NotifyPlayFabPlayerRemoved(NetworkConnectionToClient conn)
    {
        if (serverStartUp != null)
        {
            // IMPORTANT: Récupérer le VRAI PlayFabId depuis UnityNetworkServer
            string realPlayFabId = GetRealPlayFabId(conn);

            if (!string.IsNullOrEmpty(realPlayFabId))
            {
                Debug.Log($"Notifying PlayFab: REAL Player {realPlayFabId} removed");
                serverStartUp.ManualPlayerRemoved(realPlayFabId);
            }
            else
            {
                Debug.LogWarning($"No authenticated PlayFabId found for disconnecting connection {conn.connectionId}");
            }
        }
    }

    // NOUVELLE MÉTHODE: Récupérer le vrai PlayFabId depuis UnityNetworkServer
    private string GetRealPlayFabId(NetworkConnectionToClient conn)
    {
        if (serverStartUp?.UNetServer != null)
        {
            // Chercher dans les connexions authentifiées d'UnityNetworkServer
            var authenticatedConnection = serverStartUp.UNetServer.Connections.Find(c =>
                c.Connection == conn && c.IsAuthenticated && !string.IsNullOrEmpty(c.PlayFabId));

            if (authenticatedConnection != null)
            {
                return authenticatedConnection.PlayFabId;
            }
        }

        return null; // Pas d'authentification trouvée
    }

    private IEnumerator DelayedPlayFabUpdate()
    {
        // Attendre une frame pour que les connexions soient mises à jour
        yield return new WaitForEndOfFrame();

        Debug.Log($"PlayFab update triggered - current connections: {NetworkServer.connections.Count}");

        // Le système existant ServerStartUp gère déjà les updates PlayFab
        // Cette méthode est juste pour le debug
    }

    private void CheckForGameStart()
    {
        // Compter les CONNEXIONS, pas les joueurs (puisqu'on n'en crée pas dans le lobby)
        int connectionCount = NetworkServer.connections.Count;
        Debug.Log($"Checking for game start. Connections: {connectionCount}, Required: {requiredPlayers}");

        if (connectionCount >= requiredPlayers)
        {
            Debug.Log("Starting game sequence...");
            hasGameStarted = true;
            StartCoroutine(StartGameSequence());
        }
    }

    private IEnumerator StartGameSequence()
    {
        Debug.Log("Game starting in 3 seconds...");
        yield return new WaitForSeconds(3f);

        // Incrémenter le compteur AVANT le changement
        sceneChangeCount++;

        Debug.Log($"Changing to game scene (attempt {sceneChangeCount})...");
        ServerChangeScene(gameScene);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        Debug.Log($"Scene changed to: {sceneName}");

        // Marquer qu'on est dans la scène de jeu
        isInGameScene = (sceneName == gameScene);

        if (sceneName == gameScene)
        {
            Debug.Log("Successfully loaded game scene!");
            // Maintenant que nous sommes dans la scène de jeu, créer les joueurs
            StartCoroutine(CreatePlayersInGameScene());
        }
        else if (sceneName == lobbyScene)
        {
            // Reset si on retourne au lobby
            Debug.Log("Back in lobby - resetting game state");
            ResetGameState();
        }
    }

    /*private IEnumerator CreatePlayersInGameScene()
    {
        // Attendre que la scène soit complètement chargée
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Creating players in game scene...");

        // Créer un joueur SEULEMENT pour les connexions en attente (venues du lobby)
        var connectionsToProcess = new System.Collections.Generic.List<NetworkConnectionToClient>(pendingPlayerCreation);

        foreach (var conn in connectionsToProcess)
        {
            if (conn != null && conn.identity == null) // Pas encore de joueur assigné
            {
                Debug.Log($"Creating player for pending connection {conn.connectionId}");

                Transform startPos = GetStartPosition();
                GameObject player = startPos != null
                    ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                    : Instantiate(playerPrefab);

                NetworkServer.AddPlayerForConnection(conn, player);

                // Retirer de la liste des connexions en attente
                pendingPlayerCreation.Remove(conn);

                // MAINTENANT notifier PlayFab - un vrai joueur a été créé !
                NotifyPlayFabPlayerAdded(conn);
            }
        }

        OnGameSceneLoaded();
    }*/

    // Ajoutez cette méthode dans votre CustomNetworkManager.cs existant

    private IEnumerator CreatePlayersInGameScene()
    {
        // Attendre que la scène soit complètement chargée
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Creating players in game scene...");

        // Créer un joueur SEULEMENT pour les connexions en attente (venues du lobby)
        var connectionsToProcess = new System.Collections.Generic.List<NetworkConnectionToClient>(pendingPlayerCreation);

        foreach (var conn in connectionsToProcess)
        {
            if (conn != null && conn.identity == null) // Pas encore de joueur assigné
            {
                Debug.Log($"Creating player for pending connection {conn.connectionId}");

                Transform startPos = GetStartPosition();
                GameObject player = startPos != null
                    ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                    : Instantiate(playerPrefab);

                NetworkServer.AddPlayerForConnection(conn, player);

                // Retirer de la liste des connexions en attente
                pendingPlayerCreation.Remove(conn);

                // MAINTENANT notifier PlayFab - un vrai joueur a été créé !
                NotifyPlayFabPlayerAdded(conn);
            }
        }

        OnGameSceneLoaded();
    }

    private void OnGameSceneLoaded()
    {
        Debug.Log($"Game scene loaded with {numPlayers} players");

        StartCoroutine(NotifyPlayersGameReady());
    }

    private IEnumerator NotifyPlayersGameReady()
    {
        // Attendre que tous les joueurs soient connectés et prêts
        yield return new WaitForSeconds(2f);

        int playerCount = 0;
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn?.identity != null) playerCount++;
        }

        Debug.Log($"Game is ready to play with {playerCount} players!");

        // NOUVEAU: Démarrer le countdown
        if (GameStartCountdown.Instance != null)
        {
            Debug.Log("Starting countdown...");
            GameStartCountdown.Instance.StartCountdown();
        }
        else
        {
            Debug.LogError("GameStartCountdown.Instance is null! Make sure it exists in the scene.");
        }
    }

    /*private IEnumerator NotifyPlayersGameReady()
    {
        // Attendre que tous les joueurs soient connectés et prêts
        yield return new WaitForSeconds(2f);

        int playerCount = 0;
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn?.identity != null) playerCount++;
        }

        Debug.Log($"Game is ready to play with {playerCount} players!");
        // Envoyer un RPC aux clients pour les informer
        // RpcGameReady();
    }*/

    // Méthode pour retourner au lobby (appelée quand la partie se termine)
    public void ReturnToLobby()
    {
        if (isInGameScene && NetworkServer.active)
        {
            Debug.Log("Returning to lobby...");

            // Pas besoin de RPC - le changement de scène se synchronise automatiquement
            StartCoroutine(DelayedReturnToLobby());
        }
    }

    private IEnumerator DelayedReturnToLobby()
    {
        yield return new WaitForSeconds(1f);
        ServerChangeScene(lobbyScene);
    }

    private void ResetGameState()
    {
        hasGameStarted = false;
        isInGameScene = false;
        sceneChangeCount = 0;
        pendingPlayerCreation.Clear(); // Nettoyer les connexions en attente
        Debug.Log("Game state reset");
    }

    // ===== INTÉGRATION AVEC VOTRE SYSTÈME PLAYFAB EXISTANT =====

    private void UpdatePlayFabPlayerCount()
    {
        // Ne rien faire ici - votre ServerStartUp.cs gère déjà cela parfaitement
        // via OnPlayerAdded/OnPlayerRemoved qui appellent PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers

        Debug.Log($"Player count updated - handled by ServerStartUp.cs");
    }

    // Méthodes utilitaires pour l'état
    public bool IsGameStarted() => hasGameStarted;
    public bool IsInGameScene() => isInGameScene;
    public int GetSceneChangeCount() => sceneChangeCount;
    public int GetPendingPlayerCount() => pendingPlayerCreation.Count;

    // Override pour débugger
    public override void OnStopServer()
    {
        Debug.Log("Server stopping - cleaning up");
        ResetGameState();
        base.OnStopServer();
    }

    // ===== GESTION ARRÊT SERVEUR =====

    // Méthode pour arrêter proprement le serveur (à appeler depuis votre UI ou code)
    public void ShutdownServer()
    {
        if (NetworkServer.active)
        {
            Debug.Log("Initiating server shutdown...");
            StartCoroutine(DelayedServerShutdown());
        }
    }

    private IEnumerator DelayedServerShutdown()
    {
        // Notifier les clients via déconnexion forcée
        Debug.Log("Shutting down server - clients will be disconnected");

        // Attendre un peu
        yield return new WaitForSeconds(1f);

        // Arrêter le serveur (les clients seront automatiquement déconnectés)
        StopServer();
    }

    // Enregistrer le handler côté client - supprimé pour éviter l'erreur
    public override void OnStartClient()
    {
        base.OnStartClient();
        // Plus de handler personnalisé pour éviter les erreurs
    }

    // Gérer la déconnexion côté client
    public override void OnClientDisconnect()
    {
        Debug.Log("Client disconnected from server");

        // Nettoyer l'état du client
        CleanupClientState();

        base.OnClientDisconnect();

        ShowServerDisconnectedMessage();
    }

    private void CleanupClientState()
    {
        Debug.Log("Cleaning up client state");
    }

    private void ShowServerDisconnectedMessage()
    {
        Debug.Log("Déconnecté du serveur");

        // Ici vous pouvez :
        // - Afficher un message UI
        // - Retourner au menu principal
        // - Sauvegarder l'état avant déconnexion

        // Exemple pour retourner au menu :
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}