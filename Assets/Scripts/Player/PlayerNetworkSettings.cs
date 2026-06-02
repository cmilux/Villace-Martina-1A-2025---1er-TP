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

        if (IsServer && _spawnPoints.Length > 0)
        {
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
