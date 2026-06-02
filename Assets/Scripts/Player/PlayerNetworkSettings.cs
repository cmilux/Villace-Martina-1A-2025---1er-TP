using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerNetworkSettings : NetworkBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private GameObject _camera;
    [SerializeField] private Transform[] _spawnPoints;

    private void Awake()
    {
        //PlayerInput starts off
        _playerInput.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        _playerInput.enabled = IsOwner;
        _camera.SetActive(IsOwner);

        //server moves each player to their spawn point based on their ID
        if (IsServer && _spawnPoints.Length > 0)
        {
            //use clientId to pick a spawn point, loops back if more players than points
            int index = (int)(OwnerClientId % (ulong)_spawnPoints.Length);
            transform.position = _spawnPoints[index].position;
        }
    }

    public override void OnNetworkDespawn()
    {
        _playerInput.enabled = false;
        _camera.SetActive(false);
    }
}
