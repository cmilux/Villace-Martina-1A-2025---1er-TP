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
    }

    public override void OnNetworkDespawn()
    {
        _objCollected.OnValueChanged -= OnPickUpChanged;     //desubscribe to event
        _score.OnValueChanged -= OnScoreChanged;
    }

    private void OnPickUpChanged(int previous, int current)
    {
        UpdatePickUpUI(current);                                  //update ui whenever we pick up objects
    }

    private void OnScoreChanged(int previous, int current)
    {
        UpdateScoreUI(current);
    }

    private void UpdatePickUpUI(int value)
    {
        if (_pickedUpText != null)
            _pickedUpText.SetText($"Picked up: {value}");       //update picked up obj text from ui
    }

    void UpdateScoreUI(int value)
    {

        if (_score != null)
            _scoreText.SetText($"Score: {value}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return; // only owner triggers pickup

        if (other.CompareTag("PickUp"))
            AddPickedUpServerRpc();

        if (other.CompareTag("TriggerZone") && (_objCollected.Value >= 1))
            DropAndScoreServerRpc(_objCollected.Value);
    }

    [ServerRpc]     //run this on server, called from client
    void AddPickedUpServerRpc()
    {
        _objCollected.Value++;          //add 1 to variable
    }

    [ServerRpc]
    void DropAndScoreServerRpc(int amount)
    {
        _score.Value += amount;
        _objCollected.Value = 0;          //rest 1 to variable
    }
}
