using UnityEngine;
using Mirror;

public class FinishLine : NetworkBehaviour
{
    [Header("Settings")]
    public float delaiEntreTours = 2f; // Empêche les détections trop rapides
    private float dernierPassage = 0f;

    void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est un joueur qui traverse la ligne
        if (other.CompareTag("Player"))
        {
            // Éviter les détections trop rapprochées
            if (Time.time < dernierPassage + delaiEntreTours) return;
            dernierPassage = Time.time;

            string nomJoueur = other.gameObject.name;
            Debug.Log(nomJoueur + " a traverse la ligne d arrivee!");

            // Seul le serveur peut ajouter des tours
            if (isServer)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AjouterTour(nomJoueur);
                }
                else
                {
                    Debug.LogError("GameManagerReseau.Instance est null sur le serveur!");
                }
            }

            // Effet visuel pour le joueur local
            var networkIdentity = other.GetComponent<NetworkIdentity>();
            if (networkIdentity != null && networkIdentity.isOwned)
            {
                Debug.Log("Tu as passe la ligne d arrivee!");
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}