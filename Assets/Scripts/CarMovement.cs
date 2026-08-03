using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CarMovement : NetworkBehaviour
{
    [Header("Car Settings")]
    public float speed = 50f;
    public float tempSpeed;
    public float drag = 0.95f;
    public float angleSpeed = 5f;
    public float traction = 1f;

    [Header("Steering")]
    public float currentTurnAngle = 0f;
    public float maxTurnAngle = 5f;
    public float turnAcceleration = 10f;

    [Header("Components")]
    public Rigidbody rb;

    // Variables synchronisées
    [SyncVar]
    private Vector3 networkPosition;

    [SyncVar]
    private Quaternion networkRotation;

    [SyncVar]
    private Vector3 networkMoveForce;

    // Variables locales
    private Vector3 moveForce;
    private float steerInput, accelInput;

    // Pour l'interpolation smooth
    private float lerpRate = 15f;

    void Start()
    {
        // Initialiser la position réseau
        if (isServer)
        {
            networkPosition = transform.position;
            networkRotation = transform.rotation;
        }

        // Attendre que le client soit ready avant d'activer les contrôles
        if (isOwned && !NetworkClient.ready)
        {
            StartCoroutine(WaitForClientReady());
        }
    }

    private IEnumerator WaitForClientReady()
    {
        // Attendre que le client soit ready
        yield return new WaitUntil(() => NetworkClient.ready);

        Debug.Log("Client is now ready, waiting for countdown...");
    }

    void FixedUpdate()
    {
        // Seulement le propriétaire peut contrôler la voiture
        if (isOwned)
        {
            // VÉRIFIER SI LES JOUEURS PEUVENT BOUGER avant d'accepter les inputs
            if (GameStartCountdown.CanPlayersMove)
            {
                HandleInput();
                HandleMovement();
            }
            else
            {
                // Pendant le countdown, pas d'input mais on garde la synchronisation
                accelInput = 0f;
                steerInput = 0f;
            }

            // TOUJOURS envoyer la position au serveur (même pendant le countdown)
            if (!isServer && NetworkClient.ready && NetworkClient.isConnected)
            {
                CmdUpdateMovement(transform.position, transform.rotation, moveForce);
            }
        }
        else if (!isServer)
        {
            // Les autres clients interpolent TOUJOURS vers la position réseau
            InterpolateMovement();
        }
        else if (isServer && !isOwned)
        {
            // Le serveur met à jour les SyncVars pour les joueurs non-locaux
            networkPosition = transform.position;
            networkRotation = transform.rotation;
            networkMoveForce = moveForce;
        }
    }

    void HandleInput()
    {
        accelInput = 1;//Input.GetAxis("Vertical");
        steerInput = GetSteerInput();
    }

    void HandleMovement()
    {
        // Logique de mouvement (identique à votre code original)
        moveForce += transform.forward * speed * accelInput * Time.fixedDeltaTime;
        transform.position += moveForce * Time.fixedDeltaTime;
        moveForce *= drag;
        moveForce = Vector3.ClampMagnitude(moveForce, speed / 3);

        moveForce = Vector3.Lerp(moveForce.normalized, transform.forward, traction * Time.fixedDeltaTime) * moveForce.magnitude;

        if (steerInput != 0)
        {
            if (steerInput < 0)
            {
                MoveLeft();
            }
            else
            {
                MoveRight();
            }
        }

        // Mettre à jour les variables réseau si on est le serveur
        if (isServer)
        {
            networkPosition = transform.position;
            networkRotation = transform.rotation;
            networkMoveForce = moveForce;
        }
    }

    // Commande pour envoyer le mouvement au serveur
    [Command]
    void CmdUpdateMovement(Vector3 position, Quaternion rotation, Vector3 force)
    {
        // Vérifier que la commande vient bien d'un client connecté et ready
        if (!NetworkServer.connections.ContainsValue(connectionToClient))
        {
            Debug.LogWarning("Command from invalid connection");
            return;
        }

        // Validation anti-triche SEULEMENT si le jeu a commencé
        if (GameStartCountdown.CanPlayersMove)
        {
            float distance = Vector3.Distance(networkPosition, position);
            if (distance > speed * Time.fixedDeltaTime * 2f)
            {
                // Position suspecte, rejeter
                Debug.LogWarning($"Suspicious movement detected: {distance} units in one frame");
                TargetForcePosition(connectionToClient, networkPosition, networkRotation);
                return;
            }
        }

        // Mettre à jour la position sur le serveur (toujours, même pendant countdown)
        networkPosition = position;
        networkRotation = rotation;
        networkMoveForce = force;

        transform.position = position;
        transform.rotation = rotation;
        moveForce = force;
    }

    // Forcer une position côté client (anti-triche)
    [TargetRpc]
    void TargetForcePosition(NetworkConnection target, Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    void InterpolateMovement()
    {
        // Interpolation smooth vers la position réseau
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.fixedDeltaTime * lerpRate);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.fixedDeltaTime * lerpRate);
        moveForce = Vector3.Lerp(moveForce, networkMoveForce, Time.fixedDeltaTime * lerpRate);
    }

    private float GetSteerInput()
    {
        float steer = Input.GetAxis("Horizontal");

        // Support tactile/mobile
        if (Input.GetMouseButton(0))
        {
            float x = Input.mousePosition.x;
            if (x < Screen.width / 2f)
                steer = -1f;
            else if (x > Screen.width / 2f)
                steer = 1f;
        }

        return steer;
    }

    public void MoveLeft()
    {
        transform.Rotate(-Vector3.up * moveForce.magnitude * angleSpeed * Time.fixedDeltaTime);
    }

    public void MoveRight()
    {
        transform.Rotate(Vector3.up * moveForce.magnitude * angleSpeed * Time.fixedDeltaTime);
    }
}