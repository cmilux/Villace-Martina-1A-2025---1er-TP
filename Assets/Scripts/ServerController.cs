using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class ServerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject _canvas;
    [SerializeField] Button _createGame;
    [SerializeField] Button _joinGame;
    [SerializeField] TMP_InputField _codeIPInput;

    void Start()
    {
        //listen to buttons
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
        //get the text fromm input text
        string ip = _codeIPInput.text;

        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("Please enter an IP address");
            return;
        }

        Debug.Log("Connecting to: " + ip);

        //set ip to connect client player
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, 7777);

        NetworkManager.Singleton.StartClient(); //start client
        TurnCanvasOff();    //turn canvas off
    }

    void TurnCanvasOff()
    {
        //canvas is off
        _canvas.SetActive(false);
    }
}