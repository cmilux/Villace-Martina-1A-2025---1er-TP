using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

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

        //clean input spaces
        _howManyPlayers.text = "";
        _codeIPInput.text = "";
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

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        //set ip to connect client player
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, 7777);

        NetworkManager.Singleton.OnTransportFailure += TransportFail;

        NetworkManager.Singleton.StartClient(); //start client
        TurnCanvasOff();    //turn canvas off
    }

    void TransportFail()
    {
        NetworkManager.Singleton.OnTransportFailure -= TransportFail;

        NetworkManager.Singleton.Shutdown();

        StartCoroutine(ResetUI());
    }

    IEnumerator ResetUI()
    {
        yield return null;

        if(_waitingPanel) _waitingPanel.SetActive(false);
        _canvas.SetActive(true);
    }

    public void ShowCanvas() => _canvas.SetActive(true);

    void TurnCanvasOff()
    {
        //canvas is off
        _canvas.SetActive(false);
    }

    void Exit()
    {
        NetworkManager.Singleton.Shutdown();

        //exit the game inside of unity or if it's a build, exit the build
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
    }
}