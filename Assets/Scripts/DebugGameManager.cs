using UnityEngine;
using Mirror;

public class DebugGameManager : MonoBehaviour
{
    [Header("Debug Controls")]
    public KeyCode simulerTour = KeyCode.T;
    public string nomJoueurTest = "JoueurTest";

    void Update()
    {
        // Appuyer sur T pour simuler un tour (serveur seulement)
        if (Input.GetKeyDown(simulerTour))
        {
            if (NetworkServer.active && GameManager.Instance != null)
            {
                GameManager.Instance.SimulerTour(nomJoueurTest);
            }
        }

        // Appuyer sur R pour réinitialiser (serveur seulement)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (NetworkServer.active)
            {
                Debug.Log("Reinitialisation du jeu...");
                // Tu peux ajouter une logique de réinitialisation ici
            }
        }
    }
}