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
    [SerializeField] TMP_InputField _howManyPlayers;

    void Start()
    {
        //listen to buttons
        _createGame.onClick.AddListener(CreateGameClicked);
        _joinGame.onClick.AddListener(JoinGameClicked);
    }

    void CreateGameClicked()
    {
        int min = 1;
        if (!string.IsNullOrEmpty(_howManyPlayers.text))        //if howManyPlayers has a value
        {
            if (int.TryParse(_howManyPlayers.text, out int parsed) && parsed > 0)       //convert string to int
            {
                min = parsed - 1;
            }
            else
            {
                Debug.LogWarning("Invalid");
            }
        }

        NetworkManager.Singleton.StartHost(); //start host

        GameManager._minClients = min;

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