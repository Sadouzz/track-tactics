using Mirror;
using UnityEngine;

public class GameSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;

    private void Start()
    {
      
      
         
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Exemple : spawn pour chaque joueur
        foreach (var conn in NetworkServer.connections.Values)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-5, 5), 1, Random.Range(-5, 5));
            GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(player, conn); // Spawn et associe au client
        }
    }
}
