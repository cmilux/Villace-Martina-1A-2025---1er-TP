using System;
using UnityEngine;
using Unity.Netcode;

public class PlayerColor : NetworkBehaviour
{
    [SerializeField] Renderer _renderer;            //player renderer
    [SerializeField]
    Color[] _playerColors = new Color[]        //color array for players
    {
        Color.orange,
        Color.red,
        Color.green,
        Color.blue
    };

    //variable to assign color to players
    private NetworkVariable<int> _colorSlot = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        //this unsuscribes and writes an anon function (see the bottom to check what is replacing)
        _colorSlot.OnValueChanged += (prev, current) => ApplyColor(current);

        if (IsServer)
        {
            //check how many players to determine their slot
            int slot = FindObjectsByType<PlayerColor>(FindObjectsSortMode.None).Length - 1;
            _colorSlot.Value = Mathf.Clamp(slot, 0, _playerColors.Length - 1);      //returns a value between a min and max
        }

        ApplyColor(_colorSlot.Value);       //apply color in case is already set
    }

    public override void OnNetworkDespawn()
    {
        //this unsuscribes and writes an anon function (see the bottom to check what is replacing)
        _colorSlot.OnValueChanged -= (prev, current) => ApplyColor(current);
    }

    void ApplyColor(int slot)
    {
        if (_renderer)
        {
            //apply the right color (following order in array) to each player
            _renderer.material.color = _playerColors[slot];
        }
    }

    /*void OnColorSlotChanged(int prev, int current)
    {
        ApplyColor(current);
    }*/
}
