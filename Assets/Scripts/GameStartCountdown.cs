using UnityEngine;
using Mirror;
using TMPro;
using System.Collections;

public class GameStartCountdown : NetworkBehaviour
{
    [Header("Countdown Settings")]
    public float countdownDuration = 3f;

    [Header("UI References")]
    public TextMeshProUGUI countdownText;
    public GameObject countdownPanel;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip countdownBeep;
    public AudioClip goSound;

    // Synchroniser l'état du countdown
    [SyncVar(hook = nameof(OnCountdownStateChanged))]
    private bool isCountdownActive = false;

    [SyncVar(hook = nameof(OnCountdownValueChanged))]
    private float currentCountdown = 0f;

    // État du jeu
    private bool gameStarted = false;
    public static bool CanPlayersMove { get; private set; } = false;

    // Singleton
    public static GameStartCountdown Instance;

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

        // Cacher le panel au départ
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
    }

    // Appelé par le CustomNetworkManager quand tous les joueurs sont prêts
    [Server]
    public void StartCountdown()
    {
        if (gameStarted) return;

        Debug.Log("Starting countdown...");
        StartCoroutine(CountdownSequence());
    }

    [Server]
    private IEnumerator CountdownSequence()
    {
        gameStarted = true;
        isCountdownActive = true;
        currentCountdown = countdownDuration;

        // Attendre un peu pour que tout le monde charge
        yield return new WaitForSeconds(0.5f);

        int lastSecond = Mathf.CeilToInt(currentCountdown);

        // Countdown
        while (currentCountdown > 0)
        {
            currentCountdown -= Time.deltaTime;

            // Arrondir pour l'affichage
            int displayValue = Mathf.CeilToInt(currentCountdown);

            // Jouer un son à chaque nouvelle seconde
            if (displayValue != lastSecond && displayValue > 0)
            {
                lastSecond = displayValue;
                RpcPlayCountdownSound(false);
            }

            yield return null;
        }

        // Fin du countdown
        currentCountdown = 0f;
        isCountdownActive = false;

        // Jouer le son "GO"
        RpcPlayCountdownSound(true);

        // Attendre un petit délai avant d'activer le mouvement
        yield return new WaitForSeconds(0.1f);

        // Activer le mouvement des joueurs
        RpcEnablePlayerMovement();

        Debug.Log("Countdown finished - GO!");
    }

    // Hook appelé quand l'état du countdown change
    void OnCountdownStateChanged(bool oldValue, bool newValue)
    {
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(newValue);
        }
    }

    // Hook appelé quand la valeur du countdown change
    void OnCountdownValueChanged(float oldValue, float newValue)
    {
        UpdateCountdownDisplay(newValue);
    }

    void UpdateCountdownDisplay(float value)
    {
        if (countdownText == null) return;

        if (value > 0)
        {
            int displayNumber = Mathf.CeilToInt(value);
            countdownText.text = displayNumber.ToString();

            // Animation de scale (optionnel)
            if (countdownText.transform.localScale != Vector3.one)
            {
                StopAllCoroutines();
            }
            StartCoroutine(AnimateCountdownText());
        }
        else
        {
            countdownText.text = "GO!";
            StartCoroutine(HideCountdownAfterDelay());
        }
    }

    private IEnumerator AnimateCountdownText()
    {
        // Animation de pulse
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 1.5f;
        Vector3 endScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            countdownText.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        countdownText.transform.localScale = endScale;
    }

    private IEnumerator HideCountdownAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
    }

    [ClientRpc]
    void RpcPlayCountdownSound(bool isGo)
    {
        if (audioSource != null)
        {
            AudioClip clip = isGo ? goSound : countdownBeep;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    [ClientRpc]
    void RpcEnablePlayerMovement()
    {
        CanPlayersMove = true;
        Debug.Log("[CLIENT] Players can now move!");
    }

    private void PlayGoSound()
    {
        if (audioSource != null && goSound != null)
        {
            audioSource.PlayOneShot(goSound);
        }
    }

    // Méthode utilitaire pour reset
    [Server]
    public void ResetCountdown()
    {
        gameStarted = false;
        isCountdownActive = false;
        currentCountdown = 0f;
        CanPlayersMove = false;
    }
}