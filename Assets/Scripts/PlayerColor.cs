using System;
using UnityEngine;
using Unity.Netcode;

public class PlayerColor : NetworkBehaviour
{
    [SerializeField] Renderer _renderer;
    [SerializeField] Color[] _playerColors = new Color[]
    {
        Color.orange,
        Color.red,
        Color.green,
        Color.blue
    };

    public override void OnNetworkSpawn()
    {
        int colorIndex = (int)OwnerClientId % _playerColors.Length;
        SetColorServerRpc(colorIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void SetColorServerRpc(int color)
    {
        SetColorClientRpc(color);
    }

    [ClientRpc]
    void SetColorClientRpc(int color)
    {
        if(_renderer)
            _renderer.material.color = _playerColors[color];
    }
}
