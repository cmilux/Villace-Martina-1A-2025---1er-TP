using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class ServerController : MonoBehaviour
{
    public static ServerController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] GameObject _canvas;
    [SerializeField] Button _createGame;
    [SerializeField] Button _joinGame;
    [SerializeField] TMP_InputField _howManyPlayers;
    [SerializeField] TMP_InputField _codeIPInput;
    [SerializeField] TextMeshProUGUI _tryAgainIP;
    [SerializeField] GameObject _waitingPanel;
    [SerializeField] Button _exitGame;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        //listen to buttons
        _createGame.onClick.AddListener(CreateGameClicked);
        _joinGame.onClick.AddListener(JoinGameClicked);
        _exitGame.onClick.AddListener(Exit);
    }

    void CreateGameClicked()
    {
        int min = 1;        //by default 2 players (1 cause is one client)

        if (!string.IsNullOrEmpty(_howManyPlayers.text))        //checks if input field has something typed
        {
            if (int.TryParse(_howManyPlayers.text, out int parsed) && parsed > 0)       //convert string to int (has to be bigger than 0)
            {
                //if host types 2, subtracts 1 because host is alredy in it
                min = parsed - 1;
            }
        }

        NetworkManager.Singleton.StartHost();   //start host
        GameManager._minClients = min;          //send the value to game manager so the game starts

        TurnCanvasOff();
    }

    void JoinGameClicked()
    {
        //get the text fromm input text
        string ip = _codeIPInput.text;

        //if a previous connection attempt is still active
        if (NetworkManager.Singleton.IsListening)
        {
            //shut it down before trying again
            NetworkManager.Singleton.Shutdown();
        }

        //set ip to connect client player
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, 7777);
        NetworkManager.Singleton.OnTransportFailure += TransportFail;       //if client didnt connect correctly goes here
        NetworkManager.Singleton.StartClient(); //start client

        TurnCanvasOff();    //turn canvas off
    }

    void TransportFail()
    {
        NetworkManager.Singleton.OnTransportFailure -= TransportFail;   //desuscribes
        NetworkManager.Singleton.Shutdown();        //shuts network down

        StartCoroutine(ResetUI());      //resets ui
        _tryAgainIP.gameObject.SetActive(true);
    }

    IEnumerator ResetUI()
    {
        yield return null;  //waits one frama

        if (_waitingPanel) _waitingPanel.SetActive(false);      //forces to waiting panel to turn off
        TurnCanvasOn();

    }

    public void TurnCanvasOn()
    {
        _canvas.SetActive(true);
    }

    void TurnCanvasOff()
    {
        //canvas is off
        _canvas.SetActive(false);
    }

    void Exit()
    {
        //shut network down
        NetworkManager.Singleton.Shutdown();

        //exit the game inside of unity or if it's a build, exit the build
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
    }
}