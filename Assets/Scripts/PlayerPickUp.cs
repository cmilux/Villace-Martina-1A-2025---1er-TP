using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerPickUp : NetworkBehaviour
{
    [SerializeField] NetworkVariable <int> _objCollected = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    [SerializeField] TextMeshProUGUI _pickedUpText;

    public override void OnNetworkSpawn()
    {
        //owner listen and update their own UI
        if (!IsOwner) return;

        _objCollected.OnValueChanged += OnScoreChanged;     //subscribe to event
        UpdateUI(_objCollected.Value);                      //update ui w the value of a pickup
    }

    public override void OnNetworkDespawn()
    {
        _objCollected.OnValueChanged -= OnScoreChanged;     //desubscribe to event
    }

    private void OnScoreChanged(int previous, int current)
    {
        UpdateUI(current);                                  //update ui whenever we pick up objects
    }

    private void UpdateUI(int value)
    {
        if (_pickedUpText != null)
            _pickedUpText.SetText($"Picked up: {value}");       //update picked up obj text from ui
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return; // only owner triggers pickup

        if (other.CompareTag("PickUp"))
            AddPickedUpServerRpc();
    }

    [ServerRpc]     //run this on server, called from client
    private void AddPickedUpServerRpc()
    {
        _objCollected.Value++;          //add 1 to variable
    }
}
