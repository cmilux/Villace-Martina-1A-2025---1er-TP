using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerNetworkSettings : NetworkBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private GameObject _camera;

    private void Awake()
    {
        //PlayerInput
        _playerInput.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        _playerInput.enabled = IsOwner;
        _camera.SetActive(IsOwner);
    }

    public override void OnNetworkDespawn()
    {
        _playerInput.enabled = false;
        _camera.SetActive(false);
    }
}
