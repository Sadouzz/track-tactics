    using UnityEngine;
using PlayFab;
using PlayFab.MultiplayerModels;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using PlayFab.Networking;

public class ServerMatchmakingManager : MonoBehaviour
{
    [Header("Configuration")]
    public string queueName = "RaceQueue";

    [Header("Game Scene")]
    public string gameSceneName = "City";

    [Header("Network Manager")]
    public NetworkManager networkManager; // Référence à votre NetworkManager Mirror

    private string ticketId;
    private Coroutine pollCoroutine;
    private GetMatchResult currentMatch;
    public ClientStartUp cSU;



    void Start()
    {
        // S'assurer que le NetworkManager persiste entre les scènes
        if (networkManager != null)
        {
            DontDestroyOnLoad(networkManager.gameObject);
        }

        cSU.LoginRemoteUser();
        StartCoroutine(WaitLogin());
    }

    IEnumerator WaitLogin()
    {
        yield return new WaitForSeconds(3);
        if (string.IsNullOrEmpty(PlayFabSettings.staticPlayer.EntityId))
        {
            Debug.LogError("Le joueur doit être connecté à PlayFab avant de faire du matchmaking!");
            yield return null;
        }

        Debug.Log("MatchmakingManager initialisé. Entity ID: " + PlayFabSettings.staticPlayer.EntityId);
    }

    public void StartMatchmaking()
    {
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
        }

        Debug.Log("Démarrage du matchmaking avec serveur dédié...");

        var request = new CreateMatchmakingTicketRequest
        {
            Creator = new MatchmakingPlayer
            {
                Entity = new PlayFab.MultiplayerModels.EntityKey
                {
                    Id = PlayFabSettings.staticPlayer.EntityId,
                    Type = PlayFabSettings.staticPlayer.EntityType
                },
                Attributes = new MatchmakingPlayerAttributes
                {
                    DataObject = new Dictionary<string, object>
                    {
                        { "skill", UnityEngine.Random.Range(80, 120) }, // Skill aléatoire pour test
                        {
                        "latency", new List<Dictionary<string, object>> {
                            new Dictionary<string, object> {
                                { "Region", "NorthEurope" },
                                { "Value", 100 }
                            }
                        }
                    }
                    }
                }
            },
            GiveUpAfterSeconds = 30,
            QueueName = queueName
        };

        PlayFabMultiplayerAPI.CreateMatchmakingTicket(request, OnTicketCreated, OnError);
    }


    public void CancelMatchmaking()
    {
        if (!string.IsNullOrEmpty(ticketId))
        {
            var request = new CancelMatchmakingTicketRequest
            {
                TicketId = ticketId,
                QueueName = queueName
            };

            PlayFabMultiplayerAPI.CancelMatchmakingTicket(request, OnTicketCancelled, OnError);
        }

        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }
    }

    private void OnTicketCreated(CreateMatchmakingTicketResult result)
    {
        Debug.Log("Ticket de matchmaking créé: " + result.TicketId);
        ticketId = result.TicketId;
        pollCoroutine = StartCoroutine(PollTicket(ticketId));
    }

    private void OnTicketCancelled(CancelMatchmakingTicketResult result)
    {
        Debug.Log("Matchmaking annulé");
        ticketId = null;
    }

    private IEnumerator PollTicket(string ticketId)
    {
        int pollCount = 0;
        const int maxPolls = 30; // 3 minutes maximum

        while (pollCount < maxPolls)
        {
            var request = new GetMatchmakingTicketRequest
            {
                TicketId = ticketId,
                QueueName = queueName
            };

            PlayFabMultiplayerAPI.GetMatchmakingTicket(request, OnTicketStatus, OnError);

            yield return new WaitForSeconds(6f);
            pollCount++;
        }

        Debug.Log("Timeout du matchmaking atteint");
        CancelMatchmaking();
    }

    private void OnTicketStatus(GetMatchmakingTicketResult result)
    {
        Debug.Log($"Statut du ticket: {result.Status}");

        switch (result.Status)
        {
            case "WaitingForPlayers":
                Debug.Log("En attente d'autres joueurs...");
                break;

            case "WaitingForMatch":
                Debug.Log("Recherche d'un match...");
                break;

            case "Matched":
                Debug.Log("Match trouvé ! Récupération des détails...");
                if (pollCoroutine != null)
                {
                    StopCoroutine(pollCoroutine);
                    pollCoroutine = null;
                }
                GetMatch(result.MatchId);
                break;

            case "Canceled":
                Debug.Log("Matchmaking annulé");
                if (pollCoroutine != null)
                {
                    StopCoroutine(pollCoroutine);
                    pollCoroutine = null;
                }
                break;
        }
    }

    private void GetMatch(string matchId)
    {
        var request = new GetMatchRequest
        {
            MatchId = matchId,
            QueueName = queueName
        };

        PlayFabMultiplayerAPI.GetMatch(request, OnMatchFound, OnError);
    }

    private void OnMatchFound(GetMatchResult result)
    {
        Debug.Log($"Match trouvé: {result.MatchId}");
        Debug.Log($"Nombre de joueurs: {result.Members.Count}");

        currentMatch = result;

        // Afficher les informations des joueurs
        foreach (var member in result.Members)
        {
            Debug.Log($"Joueur: {member.Entity.Id}");
        }

        // Récupérer les informations du serveur alloué
        if (result.ServerDetails != null)
        {
            Debug.Log("=== INFORMATIONS DU SERVEUR ===");
            Debug.Log($"IP: {result.ServerDetails.IPV4Address}");
            Debug.Log($"Port: {result.ServerDetails.Ports}");
            Debug.Log($"Région: {result.ServerDetails.Region}");

            // Se connecter au serveur dédié
            ConnectToServer(result.ServerDetails);
        }
        else
        {
            Debug.LogError("Aucun serveur alloué trouvé !");
        }
    }

    private void ConnectToServer(ServerDetails serverDetails)
    {
        Debug.Log($"Connexion au serveur: {serverDetails.IPV4Address}:{serverDetails.Ports}");

        // Stocker les infos de connexion
        PlayerPrefs.SetString("ServerIP", serverDetails.IPV4Address);
        int serverPort = 7777;
        if (serverDetails.Ports != null && serverDetails.Ports.Count > 0)
        {
            serverPort = (int)serverDetails.Ports[0].Num;
        }
        PlayerPrefs.SetInt("ServerPort", serverPort);
        PlayerPrefs.SetString("MatchId", currentMatch.MatchId);
        PlayerPrefs.SetString("PlayerId", PlayFabSettings.staticPlayer.EntityId);

        string playersJson = JsonUtility.ToJson(new PlayersList { players = currentMatch.Members });
        PlayerPrefs.SetString("MatchPlayers", playersJson);
        PlayerPrefs.Save();

        // CONNEXION IMMÉDIATE - Modification principale
        ConnectImmediately(serverDetails.IPV4Address, serverPort);
    }

    private void ConnectImmediately(string ip, int port)
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.singleton;
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager non trouvé!");
                return;
            }
        }

        if (NetworkServer.active || NetworkClient.isConnected)
        {
            Debug.Log("Arrêt de l’instance réseau locale avant connexion distante...");
            networkManager.StopHost();
        }


        // Configurer la connexion
        networkManager.networkAddress = ip;
        networkManager.GetComponent<TelepathyTransport>().port = (ushort)port;

        // Démarrer le client
        networkManager.StartClient();

        // Démarrer une coroutine pour envoyer l'authentification une fois connecté
        StartCoroutine(SendAuthenticationAfterConnection());
    }

    private IEnumerator SendAuthenticationAfterConnection()
    {
        float timeout = 10f; // 10s max pour se connecter
        yield return new WaitUntil(() => NetworkClient.isConnected || (timeout -= Time.deltaTime) <= 0);

        if (!NetworkClient.isConnected)
        {
            Debug.LogError("Impossible de se connecter au serveur PlayFab. Arrêt du client.");
            networkManager.StopClient();
            yield break;
        }

        Debug.Log("Client connecté au serveur, envoi de l'authentification...");
        SendMessageToServer();
    }


    public void SendMessageToServer()
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogWarning("Tentative d'envoi d'authentification sans connexion");
            return;
        }

        string playfabId = PlayFabSettings.staticPlayer.PlayFabId;
        var msg = new ReceiveAuthenticateMessage { PlayFabId = playfabId };
        NetworkClient.Send(msg);

        Debug.Log($"Sent authentication message with PlayFabId: {playfabId}");
    }



    private void OnError(PlayFabError error)
    {
        Debug.LogError($"Erreur PlayFab [{error.Error}]: {error.ErrorMessage}");
        Debug.LogError($"Détails: {error.GenerateErrorReport()}");
        Debug.LogError($"Détails: {error.HttpCode}");

        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }

        ticketId = null;
    }

    void OnDestroy()
    {
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
        }
    }

    // Classes pour sérialiser les données des joueurs
    [System.Serializable]
    public class PlayersList
    {
        public List<MatchmakingPlayerWithTeamAssignment> players;
    }
}

// Script pour récupérer les infos de connexion dans la scène de jeu
public class GameConnectionManager : MonoBehaviour
{
    void Start()
    {
        // Récupérer les infos stockées
        string serverIP = PlayerPrefs.GetString("ServerIP", "");
        int serverPort = PlayerPrefs.GetInt("ServerPort", 0);
        string matchId = PlayerPrefs.GetString("MatchId", "");
        string playerId = PlayerPrefs.GetString("PlayerId", "");
        string playersJson = PlayerPrefs.GetString("MatchPlayers", "");

        if (!string.IsNullOrEmpty(serverIP) && serverPort > 0)
        {
            Debug.Log($"Connexion au serveur de jeu: {serverIP}:{serverPort}");
            Debug.Log($"Match ID: {matchId}");
            Debug.Log($"Player ID: {playerId}");

            // Ici, initialisez votre client réseau (Mirror, Netcode, etc.)
            // Exemple avec Mirror :
            // NetworkManager.singleton.networkAddress = serverIP;
            // NetworkManager.singleton.GetComponent<TelepathyTransport>().port = (ushort)serverPort;
            // NetworkManager.singleton.StartClient();

            StartGameClient(serverIP, serverPort, matchId);
        }
        else
        {
            Debug.LogError("Informations de serveur manquantes !");
        }
    }

    private void StartGameClient(string serverIP, int serverPort, string matchId)
    {
        // Implémentez ici votre logique de connexion réseau
        Debug.Log("Démarrage du client de jeu...");

        // Exemple générique :
        // 1. Configurer votre NetworkManager avec l'IP/Port
        // 2. Se connecter au serveur
        // 3. Envoyer l'ID du joueur au serveur pour authentification
        // 4. Attendre la confirmation et démarrer le jeu
    }
}