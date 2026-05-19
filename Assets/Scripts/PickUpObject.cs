using Unity.Netcode;
using UnityEngine;

public class PickUpObject : NetworkBehaviour
{
    public static event System.Action OnPickUpCollected;        //attached to spawnmanager

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            OnPickUpCollected?.Invoke();
            NetworkObject.Despawn(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        gameObject.SetActive(false);
    }
}
