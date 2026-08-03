using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchmakingUI : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    public Button cancelButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI ticketText;

    [Header("Manager Reference")]
    public ServerMatchmakingManager matchmakingManager;

    void Start()
    {
        // Configuration initiale des boutons
        playButton.onClick.AddListener(StartMatchmaking);
        cancelButton.onClick.AddListener(CancelMatchmaking);

        // État initial
        SetUIState(false);
        UpdateStatus("Prêt à jouer");
    }

    public void StartMatchmaking()
    {
        if (matchmakingManager != null)
        {
            matchmakingManager.StartMatchmaking();
            SetUIState(true);
            UpdateStatus("Recherche d'un match...");
        }
    }

    public void CancelMatchmaking()
    {
        if (matchmakingManager != null)
        {
            matchmakingManager.CancelMatchmaking();
            SetUIState(false);
            UpdateStatus("Matchmaking annulé");
        }
    }

    private void SetUIState(bool isMatchmaking)
    {
        playButton.interactable = !isMatchmaking;
        cancelButton.interactable = isMatchmaking;
    }

    public void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }   
        Debug.Log("UI Status: " + message);
    }

    // Méthodes appelables par le MatchmakingManager pour mettre à jour l'UI
    public void OnMatchmakingStarted()
    {
        SetUIState(true);
        UpdateStatus("Recherche en cours...");
    }

    public void OnMatchFound()
    {
        SetUIState(false);
        UpdateStatus("Match trouvé ! Chargement...");
    }

    public void OnMatchmakingCancelled()
    {
        SetUIState(false);
        UpdateStatus("Recherche annulée");
    }

    public void OnMatchmakingError(string error)
    {
        SetUIState(false);
        UpdateStatus($"Erreur: {error}");
    }
}