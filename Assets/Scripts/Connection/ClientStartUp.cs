using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using Mirror;
using PlayFab.Networking;

public class ClientStartUp : MonoBehaviour
{
    public Configuration configuration;
    public ServerStartUp serverStartUp;
    public NetworkManager networkManager;
    public TelepathyTransport telepathyTransport;
    public ApathyTransport apathyTransport;

    // Événement pour notifier la fin du login
    public static System.Action OnLoginCompleted;

    public void OnLoginUserButtonClick()
    {
        if (configuration.buildType == BuildType.REMOTE_CLIENT)
        {
            if (configuration.buildId == "")
            {
                throw new Exception("A remote client build must have a buildId. Add it to the Configuration. Get this from your Multiplayer Game Manager in the PlayFab web console.");
            }
            else
            {
                LoginRemoteUser();
            }
        }
        else if (configuration.buildType == BuildType.LOCAL_CLIENT)
        {
            networkManager.StartClient();
        }
    }

    public void LoginRemoteUser()
    {
        Debug.Log("[ClientStartUp].LoginRemoteUser");

        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CreateAccount = true,
            CustomId = GUIDUtility.getUniqueID()
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnPlayFabLoginSuccess, OnLoginError);
    }

    private void OnLoginError(PlayFabError response)
    {
        Debug.Log(response.ToString());
    }

    private void OnPlayFabLoginSuccess(LoginResult response)
    {
        Debug.Log("Login PlayFab réussi: " + response.PlayFabId);

        // Notifier que le login est complet
        OnLoginCompleted?.Invoke();
    }

    // NOUVELLE MÉTHODE - Pour être appelée par ServerMatchmakingManager
    public void ConnectToServer(string ipAddress, int port)
    {
        Debug.Log($"Connexion au serveur: {ipAddress}:{port}");

        networkManager.networkAddress = ipAddress;
        telepathyTransport.port = (ushort)port;
        apathyTransport.port = (ushort)port;

        networkManager.StartClient();
    }

    private void RequestMultiplayerServer()
    {
        Debug.Log("[ClientStartUp].RequestMultiplayerServer");
        RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest();
        requestData.BuildId = configuration.buildId;
        requestData.SessionId = System.Guid.NewGuid().ToString();
        requestData.PreferredRegions = new List<string>() { AzureRegion.NorthEurope.ToString() };
        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
    }

    private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
    {
        Debug.Log(response.ToString());
        ConnectRemoteClient(response);
    }

    private void ConnectRemoteClient(RequestMultiplayerServerResponse response = null)
    {
        if (response == null)
        {
            networkManager.networkAddress = configuration.ipAddress;
            telepathyTransport.port = configuration.port;
            apathyTransport.port = configuration.port;
        }
        else
        {
            Debug.Log("**** ADD THIS TO YOUR CONFIGURATION **** -- IP: " + response.IPV4Address + " Port: " + (ushort)response.Ports[0].Num);
            networkManager.networkAddress = response.IPV4Address;
            telepathyTransport.port = (ushort)response.Ports[0].Num;
            apathyTransport.port = (ushort)response.Ports[0].Num;
        }

        networkManager.StartClient();
    }

    private void OnRequestMultiplayerServerError(PlayFabError error)
    {
        Debug.Log(error.ToString());
    }
}