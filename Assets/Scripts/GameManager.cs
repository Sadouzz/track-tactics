using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [Header("Course Settings")]
    public int toursTotaux = 3;

    // Stocke les tours de chaque joueur - SYNCHRONISÉ
    [SyncVar(hook = nameof(OnInfoToursChanged))]
    public string infoTours = "En attente de joueurs...";

    // Dictionnaire des tours (côté serveur seulement)
    private Dictionary<string, int> toursJoueurs = new Dictionary<string, int>();

    // Singleton
    public static GameManager Instance;

    [Header("Debug")]
    public bool debugMode = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // S'ASSURER que le GameManager reste activé
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log("GameManager activé");
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("GameManager démarré sur le serveur");

        // Forcer la mise à jour initiale
        MettreAJourInfoTours();

        // Vérifier toutes les 2 secondes l'état du jeu
        InvokeRepeating(nameof(VerifierEtatJeu), 2f, 2f);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("GameManager démarré sur le client");

        // Forcer l'affichage initial côté client
        StartCoroutine(InitialiserAffichageClient());
    }

    private System.Collections.IEnumerator InitialiserAffichageClient()
    {
        // Attendre que DisplayLaps soit initialisé
        yield return new WaitForSeconds(0.5f);

        if (DisplayLaps.Instance != null)
        {
            Debug.Log("Initialisation affichage client avec: " + infoTours);
            DisplayLaps.Instance.MettreAJourAffichage(infoTours);
        }
        else
        {
            Debug.LogError("DisplayLaps.Instance est NULL sur le client!");
        }
    }

    [Server]
    void VerifierEtatJeu()
    {
        if (debugMode)
        {
            Debug.Log($"État du jeu - Joueurs: {toursJoueurs.Count}, Info: {infoTours}");
        }
    }

    // Méthode appelée quand un joueur passe la ligne
    [Server]
    public void AjouterTour(string nomJoueur)
    {
        if (!toursJoueurs.ContainsKey(nomJoueur))
        {
            toursJoueurs[nomJoueur] = 0;
        }

        toursJoueurs[nomJoueur]++;
        Debug.Log($"{nomJoueur} a complété un tour! Total: {toursJoueurs[nomJoueur]}/{toursTotaux}");

        // Mettre à jour l'info synchronisée
        MettreAJourInfoTours();

        // Dire à tous les clients qu'un tour a été ajouté
        RpcAnnoncerTour(nomJoueur, toursJoueurs[nomJoueur]);

        // Vérifier victoire
        if (toursJoueurs[nomJoueur] >= toursTotaux)
        {
            RpcAnnoncerVainqueur(nomJoueur);
        }
    }

    [Server]
    void MettreAJourInfoTours()
    {
        if (toursJoueurs.Count == 0)
        {
            infoTours = "En attente de joueurs...";
            Debug.Log("Mise à jour info tours: " + infoTours);
            return;
        }

        string nouvelleInfo = "Tours: ";
        foreach (var joueur in toursJoueurs)
        {
            nouvelleInfo += $"{joueur.Key}: {joueur.Value}/{toursTotaux} | ";
        }
        infoTours = nouvelleInfo;
        Debug.Log($"Mise à jour info tours: {infoTours}");
    }

    // Cette méthode est appelée automatiquement quand infoTours change
    void OnInfoToursChanged(string ancienneValeur, string nouvelleValeur)
    {
        Debug.Log($"[Hook SyncVar] Info tours changée: '{ancienneValeur}' -> '{nouvelleValeur}'");

        // Mettre à jour l'UI pour tous les clients
        if (DisplayLaps.Instance != null)
        {
            DisplayLaps.Instance.MettreAJourAffichage(nouvelleValeur);
            Debug.Log("UI mise à jour via hook SyncVar");
        }
        else
        {
            Debug.LogError("DisplayLaps.Instance est NULL dans le hook!");
        }
    }

    // Envoyer à tous les clients
    [ClientRpc]
    void RpcAnnoncerTour(string nomJoueur, int tours)
    {
        Debug.Log($"[CLIENT] {nomJoueur} a {tours} tours");

        // Effet spécial si c'est notre joueur
        if (NetworkClient.localPlayer != null)
        {
            // Vérifier si c'est notre joueur en comparant le nom
            if (NetworkClient.localPlayer.name.Contains(nomJoueur))
            {
                Debug.Log($"C'est toi! Tu as {tours} tours!");
            }
        }
    }

    [ClientRpc]
    void RpcAnnoncerVainqueur(string nomJoueur)
    {
        Debug.Log($"{nomJoueur} A GAGNÉ LA COURSE!");

        // Mettre à jour l'UI pour tous les joueurs
        if (DisplayLaps.Instance != null)
        {
            DisplayLaps.Instance.AfficherVainqueur(nomJoueur);
        }
        else
        {
            Debug.LogError("DisplayLaps.Instance est NULL dans RpcAnnoncerVainqueur!");
        }
    }

    // Pour récupérer les tours d'un joueur
    public int GetToursJoueur(string nomJoueur)
    {
        if (toursJoueurs.ContainsKey(nomJoueur))
        {
            return toursJoueurs[nomJoueur];
        }
        return 0;
    }

    // Méthode pour debug - TEST MANUEL
    [Server]
    public void SimulerTour(string nomJoueur)
    {
        Debug.Log($"Simulation d'un tour pour {nomJoueur}");
        AjouterTour(nomJoueur);
    }

    // TEST: Appuyez sur T pour simuler un tour (SERVEUR SEULEMENT)
    void Update()
    {
        if (isServer && Input.GetKeyDown(KeyCode.T))
        {
            string testPlayerName = "TestPlayer";
            Debug.Log($"[TEST] Simulation d'un tour pour {testPlayerName}");
            SimulerTour(testPlayerName);
        }
    }
}