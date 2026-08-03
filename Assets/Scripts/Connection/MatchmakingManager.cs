using UnityEngine;
using PlayFab;
using PlayFab.MultiplayerModels;
using System.Collections;
using System.Collections.Generic;

public class MatchmakingManager: MonoBehaviour
{
    [Header("Configuration")]
    public string queueName = "RaceQueue";

    public string ticketId;
    private Coroutine pollCoroutine;

    public ClientStartUp cSU;

    void Start()
    {
        cSU.LoginRemoteUser();
        // Vérifier que le joueur est bien connecté à PlayFab
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
        // Arrêter tout polling précédent
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
        }

        Debug.Log("Démarrage du matchmaking...");

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
                        { "skill", 100 }
                    }
                }
                // Pas de RegionPreference pour éviter les problèmes de latence
            },
            GiveUpAfterSeconds = 120,
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
        const int maxPolls = 20;

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
                Debug.Log("Match trouvé !");
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
        Debug.Log($"Détails du match trouvé: {result.MatchId}");
        Debug.Log($"Nombre de joueurs: {result.Members.Count}");

        foreach (var member in result.Members)
        {
            Debug.Log($"Joueur: {member.Entity.Id}");
        }

        StartGame(result);
    }

    private void StartGame(GetMatchResult matchResult)
    {
        Debug.Log("Démarrage du jeu...");
        // Votre logique de démarrage de jeu ici
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError($"Erreur PlayFab [{error.Error}]: {error.ErrorMessage}");
        Debug.LogError($"Détails: {error.GenerateErrorReport()}");

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
}