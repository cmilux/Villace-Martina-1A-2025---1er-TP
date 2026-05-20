using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ServerController : MonoBehaviour
{
    [SerializeField] Button _createGame;
    [SerializeField] Button _joinGame;
    [SerializeField] GameObject _canvasStart;

    void Start()
    {
        _createGame.onClick.AddListener(CreateGameClicked);
        _joinGame.onClick.AddListener(JoinGameClicked);
    }

    void CreateGameClicked()
    {
        NetworkManager.Singleton.StartHost(); //start host
        TurnCanvasOff();
    }

    void JoinGameClicked()
    {
        NetworkManager.Singleton.StartClient(); //start client
        TurnCanvasOff();
    }

    void TurnCanvasOff()
    {
        _canvasStart.SetActive(false);
    }
}