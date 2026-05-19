using Unity.Netcode;
using UnityEngine;

public class PickUpObject : NetworkBehaviour
{
    public static event System.Action OnPickUpCollected;        //attached to spawn manager

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            OnPickUpCollected?.Invoke();                //call event from spawn manager
            NetworkObject.Despawn(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        //"destroy" picked up objs
        base.OnNetworkDespawn();
        gameObject.SetActive(false);
    }
}
