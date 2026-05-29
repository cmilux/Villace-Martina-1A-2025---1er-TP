using TMPro;
using Unity.Netcode;
using System;
using UnityEngine;

public class PlayerPickUp : NetworkBehaviour
{
    [SerializeField] NetworkVariable <int> _objCollected = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] NetworkVariable <int> _score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] TextMeshProUGUI _pickedUpText;
    [SerializeField] TextMeshProUGUI _scoreText;

    public override void OnNetworkSpawn()
    {
        //owner listen and update their own UI
        if (!IsOwner) return;

        _objCollected.OnValueChanged += OnPickUpChanged;     //subscribe to event
        _score.OnValueChanged += OnScoreChanged;            //subscribe to event
        UpdatePickUpUI(_objCollected.Value);                //update ui w the value of a pickup
        UpdateScoreUI(_score.Value);                        //update ui w the score
        GameManager.OnGameStarted += ResetStats;            //reset own variables when a new game starts
    }

    public override void OnNetworkDespawn()
    {
        //desuscribe to events
        _objCollected.OnValueChanged -= OnPickUpChanged;
        _score.OnValueChanged -= OnScoreChanged;            
        GameManager.OnGameStarted -= ResetStats;
    }

    private void OnPickUpChanged(int previous, int current)
    {
        UpdatePickUpUI(current);                                  //update ui whenever we pick up objects
    }

    private void OnScoreChanged(int previous, int current)
    {
        UpdateScoreUI(current);                                 //update score ui
    }

    void ResetStats()
    {
        if (IsOwner) ResetStatsServerRpc();
    }

    [ServerRpc]
    void ResetStatsServerRpc()
    {
        _score.Value = 0;
        _objCollected.Value = 0;
    }

    private void UpdatePickUpUI(int value)
    {
        if (_pickedUpText != null)
            _pickedUpText.SetText($"Picked up: {value}");       //set picked up obj text from ui
    }

    void UpdateScoreUI(int value)
    {

        if (_score != null)
            _scoreText.SetText($"Score: {value}");          //set score ui
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return; // only owner triggers pickup

        if (other.CompareTag("PickUp"))                                             //if player triggers pick up
            AddPickedUpServerRpc();                                                     //call the method

        if (other.CompareTag("TriggerZone") && (_objCollected.Value >= 1))          //if player triggers zone and has more than a pick up obj
            DropAndScoreServerRpc(_objCollected.Value);                                 //call the method
    }

    [ServerRpc]     //run this on server, called from client
    void AddPickedUpServerRpc()
    {
        _objCollected.Value++;          //add 1 to variable
    }

    [ServerRpc]
    void DropAndScoreServerRpc(int amount)
    {
        _score.Value += amount;             //add score points the same amount as picked up objects
        _objCollected.Value = 0;          //reset variable

        GameManager.Instance?.AddScoreRpc(amount, OwnerClientId);       //add score (gameManager)
    }

    private void Update()
    {
        //block all input until game is running
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying()) return;
    }
}
