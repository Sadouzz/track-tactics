using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisplayLaps : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI texteTours;
    public TextMeshProUGUI texteVainqueur;
    public GameObject panelUI;

    public static DisplayLaps Instance;

    void Awake()
    {
        Instance = this;

        // S'assurer que l'UI est active
        if (panelUI != null && !panelUI.activeSelf)
            panelUI.SetActive(true);
    }

    void Start()
    {
        // Message initial
        if (texteTours != null)
            texteTours.text = "Connexion en cours...";

        if (texteVainqueur != null)
            texteVainqueur.gameObject.SetActive(false);
    }

    public void MettreAJourAffichage(string infoTours)
    {
        if (texteTours != null)
        {
            texteTours.text = infoTours;
            Debug.Log("UI mise a jour: " + infoTours);
        }
    }

    public void AfficherVainqueur(string nomVainqueur)
    {
        if (texteVainqueur != null)
        {
            texteVainqueur.text = "VAINQUEUR: " + nomVainqueur;
            texteVainqueur.color = Color.yellow;
            texteVainqueur.gameObject.SetActive(true);

            Debug.Log("Victoire affichee: " + nomVainqueur);
        }
    }
}