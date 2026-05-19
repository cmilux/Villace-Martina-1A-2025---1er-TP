using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ServerController : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            NetworkManager.Singleton.StartHost(); //start host
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            NetworkManager.Singleton.StartClient(); //start client
        }
    }
}