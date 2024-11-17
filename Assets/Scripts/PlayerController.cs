using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    private Rigidbody playerRb;
    public float speed = 5.0f;
    public bool hasPowerup = false;
    private float powerupStrength = 15;
    public GameObject powerupIndicator;
    public float normal = 1.0f;

    private SpawnManager spawnManager;

    // Start is called before the first frame update
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        spawnManager = FindObjectOfType<SpawnManager>(); // Find SpawnManager once at start

        // Find or instantiate the power-up indicator
        if (powerupIndicator == null)
        {
            Debug.LogError("Powerup Indicator is not assigned. Please assign it in the inspector.");
        }
        else
        {
            // Check if the power-up indicator exists in the scene
            if (!powerupIndicator.activeInHierarchy)
            {
                // Instantiate the power-up indicator if not found
                powerupIndicator = Instantiate(powerupIndicator, transform.position, Quaternion.identity);
                powerupIndicator.GetComponent<NetworkObject>().Spawn();
                powerupIndicator.SetActive(false); // Set it inactive initially
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        float forwardInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");
        Vector3 moveDirection = new Vector3(horizontalInput, 0.0f, forwardInput).normalized;
        playerRb.AddForce(moveDirection * speed * Time.deltaTime, ForceMode.VelocityChange);


        if (hasPowerup && powerupIndicator != null)
        {
            UpdatePowerupIndicator();
            UpdatePowerupIndicatorServerRpc();
            SpawnPowerupIndicatorServerRpc();
        }

        if (NetworkManager.Singleton.IsServer)
        {
            UpdatePowerupStatusServerRpc(hasPowerup); // Update status only on server
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Powerup"))
        {
            // Send request to the server to despawn power-up
            hasPowerup = true;
            if (powerupIndicator != null)
            {
                powerupIndicator.SetActive(true);
                SpawnPowerupIndicatorServerRpc();
                spawnManager.DestroyServerRpc(other.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
                StartCoroutine(PowerupCountdownRoutine());
            }
            else
            {
                Debug.Log("Powerup Indicator not found");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void UpdatePowerupStatusServerRpc(bool status)
    {
        UpdatePowerupStatusClientRpc(status);
    }

    // Methode to update powerup status in All clients
    [ClientRpc(RequireOwnership = false)]
    void UpdatePowerupStatusClientRpc(bool status)
    {
        hasPowerup = status;
            if (powerupIndicator != null)
            {
                powerupIndicator.SetActive(status);// Set active powerupIndicator based on status
                if (status)
                {
                    UpdatePowerupIndicator(); // Update the power-up indicator position
                }
            }
    }

    IEnumerator PowerupCountdownRoutine()
    {
        Debug.Log("Powerup countdown started");
        yield return new WaitForSeconds(7);
        hasPowerup = false;        
        
            if (powerupIndicator != null)
            {
                // Deactivate power-up indicator after a certain time
                DespawnPowerupIndicatorServerRpc();
            }
         // Deactivate power-up indicator after a certain time
        Debug.Log("Powerup countdown finished");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && hasPowerup)
        {
            Rigidbody otherPlayerRb = collision.gameObject.GetComponent<Rigidbody>();
            Vector3 awayFromPlayer = collision.gameObject.transform.position - transform.position;
            otherPlayerRb.AddForce(awayFromPlayer.normalized * powerupStrength, ForceMode.Impulse);
            Debug.Log("Player collided with another player with powerup set to " + hasPowerup);

            ApplyCollisionForceServerRpc(collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId, awayFromPlayer.normalized * powerupStrength);
        }
        else if (collision.gameObject.CompareTag("Player") && !hasPowerup)
        {
            Rigidbody otherPlayerRb = collision.gameObject.GetComponent<Rigidbody>();
            Vector3 awayFromHostPlayer = collision.gameObject.transform.position - transform.position;
            otherPlayerRb.AddForce(awayFromHostPlayer.normalized, ForceMode.Impulse);
            Debug.Log("Player collided with the other player without powerup");

            ApplyCollisionForceServerRpc(collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId, awayFromHostPlayer.normalized);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    void ApplyCollisionForceServerRpc(ulong playerObjectId, Vector3 force)
    {
        if (IsServer)
            ApplyCollisionForceClientRpc(playerObjectId, force);
    }

    [ClientRpc(RequireOwnership = false)]
    void ApplyCollisionForceClientRpc(ulong playerObjectId, Vector3 force)
    {
        if (IsClient && !IsOwner)
        {
            // Get the instance of NetworkSpawnManager from NetworkManager.Singleton
            NetworkSpawnManager spawnManager = NetworkManager.Singleton.SpawnManager;
            if (spawnManager != null)
            {
                // Access SpawnedObjects through the instance
                NetworkObject playerObject = spawnManager.SpawnedObjects[playerObjectId];
                if (playerObject != null)
                {
                    Rigidbody otherPlayerRb = playerObject.GetComponent<Rigidbody>();
                    otherPlayerRb.AddForce(force, ForceMode.Impulse);
                }
            }
        }
    }
    void UpdatePowerupIndicator()
    {
        // Update the position of the power-up indicator to follow the player locally
        powerupIndicator.transform.position = transform.position + new Vector3(0, 0, 0);
    }

    [ServerRpc(RequireOwnership = false)]
    void UpdatePowerupIndicatorServerRpc()
    {
        // Update the position of the power-up indicator on all clients
        UpdatePowerupIndicatorClientRpc();
    }

    [ClientRpc(RequireOwnership = false)]
    void UpdatePowerupIndicatorClientRpc()
    {
        // Update the position of the power-up indicator on all clients
        if (powerupIndicator != null)
        {
            powerupIndicator.transform.position = transform.position + new Vector3(0, 0, 0);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnPowerupIndicatorServerRpc()
    {
        SpawnPowerupIndicatorClientRpc();
    }

    [ClientRpc(RequireOwnership = false)]
    void SpawnPowerupIndicatorClientRpc()
    {
        powerupIndicator.SetActive(true);
        powerupIndicator.transform.position = transform.position + new Vector3(0, 0, 0);
    }

    [ServerRpc(RequireOwnership = false)]
    void DespawnPowerupIndicatorServerRpc()
    {
        UpdatePowerupStatusServerRpc(hasPowerup);
        DespawnPowerupIndicatorClientRpc();
        DespawnPowerupIndicator();
    }

    [ClientRpc(RequireOwnership = false)]
    void DespawnPowerupIndicatorClientRpc()
    {
        // Call the method to destroy the indicator on all clients
        DespawnPowerupIndicator();
        UpdatePowerupStatusClientRpc(hasPowerup);
    }

    void DespawnPowerupIndicator()
    {
        // Deactivate the power-up indicator on all clients
        if (powerupIndicator != null && !IsOwner)
        {
            powerupIndicator.SetActive(false);
        }
    }
}