using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Button GetC;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private TMP_InputField joinCodeClient;
    Text CodeHostC;
    string textCode;

    void Update()
    {
        CodeHostC = GameObject.Find("CodeHostt").GetComponent<Text>();

        ShowCodeServerRpc();
    }

    private void Awake()
    {
        serverBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });
        hostBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            spawnManager.StartPowerupRoutineServerRpc();
            // Update UI text immediately after updating textCode
        });
        clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient(); 
        });
    }
    [ServerRpc]
    void ShowCodeServerRpc()
    {
        GetC.onClick.AddListener(() =>
        {
            {
                textCode = "192.168.0.101";
                CodeHostC.text = textCode;
            }

        });
    }
}