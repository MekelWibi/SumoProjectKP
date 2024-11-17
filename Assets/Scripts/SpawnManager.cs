using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] public GameObject powerupPrefab;
    private float spawnRange = 9;
    private bool powerupRoutineStarted = false;
    [SerializeField] private List<GameObject> spawnedPowerUp = new List<GameObject>();

    // Method to start the power-up spawning routine
    [ServerRpc(RequireOwnership = false)]
    public void StartPowerupRoutineServerRpc()
    {
        if (IsServer && !powerupRoutineStarted)
        {
            StartCoroutine(SpawnPowerupRoutine());
            powerupRoutineStarted = true;
        }
    }


    // Coroutine to spawn power-up every 3 seconds
    IEnumerator SpawnPowerupRoutine()
    {
        while (true)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(3); // Wait for 3 seconds

                // Check if the number of power-ups in the area is less than 1 before spawning
                if (CountPowerupsInArea() <= 1)
                {
                    // Spawn a power-up only on the server
                    GameObject powerUp = Instantiate(powerupPrefab, GenerateSpawnPosition(), Quaternion.identity);
                    spawnedPowerUp.Add(powerUp);                    
                    powerUp.GetComponent<NetworkObject>().Spawn();
                }
            }
        }
    }

    // Add this method in SpawnManager.cs
    [ServerRpc(RequireOwnership = false)]
    public void DestroyServerRpc(ulong networkObjectId)
    {
        foreach (var powerup in spawnedPowerUp)
        {
            if (powerup.GetComponent<NetworkObject>().NetworkObjectId == networkObjectId)
            {
                powerup.GetComponent<NetworkObject>().Despawn();
                spawnedPowerUp.Remove(powerup);
                Destroy(powerup);
                break; // Exit loop once the power-up is found and destroyed
            }
        }
    }


    // Function to generate random spawn position
    private Vector3 GenerateSpawnPosition()
    {
        float spawnPosX = Random.Range(-spawnRange, spawnRange);
        float spawnPosZ = Random.Range(-spawnRange, spawnRange);
        Vector3 randomPos = new Vector3(spawnPosX, 0, spawnPosZ);
        return randomPos;
    }

    // Method to count the number of power-ups in the area
    private int CountPowerupsInArea()
    {
        int count = 0;
        GameObject[] powerups = GameObject.FindGameObjectsWithTag("Powerup"); // Assuming power-ups have a "Powerup" tag
        foreach (GameObject powerup in powerups)
        {
            // Assuming the spawn area is within the range of 'spawnRange'
            if (Vector3.Distance(powerup.transform.position, Vector3.zero) < spawnRange)
            {
                count++;
            }
        }
        return count;
    }
}