using UnityEngine;
using System;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { WaitingForPlayer, Playing, GameOver }       //game states

    //events so other scripts can suscribe
    public static event Action OnGameStarted;
    public static event Action OnGameOver;

    [Header("Game Settings")]
    [SerializeField] float _gameDuration = 60f;
    public static int _minClients = 1;              //min clients for game to start (connected to server controller script)

    [Header("UI")]
    [SerializeField] TextMeshProUGUI _timerText;
    [SerializeField] GameObject _waitingPanel;
    [SerializeField] TextMeshProUGUI _playerCountText;
    [SerializeField] GameObject _gameOverPanel;
    [SerializeField] TextMeshProUGUI _winnerText;
    [SerializeField] TextMeshProUGUI _scoreboardText;
    [SerializeField] Button _playAgain;
    [SerializeField] Button _backToMenu;

    //net variable to know game time
    private NetworkVariable<float> _timeRemaining = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    //net variable to check in what state the game is
    private NetworkVariable<GameState> _state = new(
        GameState.WaitingForPlayer,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    //dictionary to store scores (key is client ID, value their score)
    private Dictionary<ulong, int> _scores = new();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        //suscribe to events
        _state.OnValueChanged += OnStateChanged;
        _timeRemaining.OnValueChanged += OnTimeChanged;

        if (IsServer)
        {
            _timeRemaining.Value = _gameDuration;           //set game duration to time remaining
            _state.Value = GameState.WaitingForPlayer;      //game didnt start yet

            //suscribe to events for connection and disconnection
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        if (_playAgain)
        {
            //listen to button
            _playAgain.onClick.AddListener(OnPlayAgainClicked);
        }

        if (_backToMenu)
        {
            //listen to button
            _backToMenu.onClick.AddListener(OnBackToMenuClicked);
        }

        RefreshUI(_state.Value);        //update ui
    }

    public override void OnNetworkDespawn()
    {
        //desuscribe to event
        _state.OnValueChanged -= OnStateChanged;
        _timeRemaining.OnValueChanged -= OnTimeChanged;

        if (IsServer)
        {
            //desuscribe to event
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    void HandleClientConnected(ulong clientId)
    {
        if (!IsServer || _state.Value != GameState.WaitingForPlayer) return;        //ignore if game didnt start

        _scores[clientId] = 0;      //score to 0 for new player

        //tell everyone how many player are connected (add one to the prev list)
        UpdatePlayerCountClientRpc(
            NetworkManager.Singleton.ConnectedClientsList.Count,
            _minClients + 1);

        //subtract 1 cause host is included in ConnectedClientsList.Count but is not a client
        int connectedClients = NetworkManager.Singleton.ConnectedClientsList.Count - 1;
        
        if (connectedClients >= _minClients)
        {
            StartGame();
        }
    }

    void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        _scores.Remove(clientId);       //remove score from player that disconnected
    }

    void StartGame()
    {
        _scores[NetworkManager.Singleton.LocalClientId] = 0;    //sets host score slot
        _state.Value = GameState.Playing;                       //update game state to playing (triggers OnGameStateChanged for all)
    }

    private void Update()
    {
        //only server runs timer
        if (!IsServer || _state.Value != GameState.Playing) return;

        _timeRemaining.Value -= Time.deltaTime;     //countdown

        if (_timeRemaining.Value <= 0)
        {
            _timeRemaining.Value = 0f;
            EndGame();
        }
    }

    void EndGame()
    {
        _state.Value = GameState.GameOver;      //update game state to game over
        SendPersonalizedResults();              //send results to each player with their message
    }

    //called from playerPickUp when a player scores (everyone can call)
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddScoreRpc(int points, ulong clientId)
    {
        if (!_scores.ContainsKey(clientId)) return;         //ignore if player isnt registered

        _scores[clientId] += points;

        //send scoreboard to everyplayer everytime score changes
        Scoreboard();
    }

    void Scoreboard()
    {
        //build a line per player w their score
        var lines = new List<string>();

        foreach (var kpv in _scores)
        {
            string label = $"Player {kpv.Key + 1}";         //clientId 0 = player 1 and so on
            lines.Add($"{label}: {kpv.Value} pts");
        }

        //send scoreboard to every client
        UpdateScoreboardClientRpc(string.Join("\n", lines));
    }

    void SendPersonalizedResults()
    {
        ulong winnerId = 0;
        int topScore = -1;
        bool isTie = false;

        //find the actual winner by looping through scores
        foreach (var kvp in _scores)
        {
            if (kvp.Value > topScore)
            {
                topScore = kvp.Value;
                winnerId = kvp.Key;
                isTie = false;
            }
            else if (kvp.Value == topScore)
            {
                isTie = true;
            }
        }

        //send final scoreboard to everyone
        Scoreboard();

        //send each player their own message
        foreach (var kvp in _scores)
        {
            string message;

            if (isTie)
                message = $"It's a tie!\nScore: {kvp.Value}";
            else if (kvp.Key == winnerId)
                message = $"You won!\nScore: {kvp.Value}";
            else
                message = $"You lost!\nWinner scored: {topScore}";

            //target this RPC to one specific client (sending parameters)
            ShowWinnerRpc(message, RpcTarget.Single(kvp.Key, RpcTargetUse.Temp));
        }
    }

    void OnPlayAgainClicked()
    {
        //any player can request but server handles logic
        RequestRestartRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestRestartRpc()
    {
        if (_state.Value != GameState.GameOver) return;     //restart is only allowed from game over

        //reset score for all connected players
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            _scores[clientId] = 0;

        //send empty scoreboard and restart time before going to waiting room
        Scoreboard();
        _timeRemaining.Value = _gameDuration;

        //check if enough players are connected to go straight to the game or the waiting room
        int connectedClients = NetworkManager.Singleton.ConnectedClientsIds.Count - 1;
        if (connectedClients >= _minClients)
        {
            StartGame();
        }
        else
        {
            _state.Value = GameState.WaitingForPlayer;
        }
    }

    void OnBackToMenuClicked()
    {
        NetworkManager.Singleton.Shutdown();    //shut network down
        StartCoroutine(BackToMenu());
    }

    IEnumerator BackToMenu()
    {
        yield return null; //wait one frame so shutdown callbacks finish before touching UI

        //set objs off
        if (_waitingPanel) _waitingPanel.SetActive(false);
        if (_gameOverPanel) _gameOverPanel.SetActive(false);
        if (_timerText) _timerText.gameObject.SetActive(false);
        if (_scoreboardText) _scoreboardText.gameObject.SetActive(false);

        //turn start canvas on
        ServerController.Instance.TurnCanvasOn();
    }

    //Rpc to clients--------------------

    //send personalized message (win/loss) to one specific client
    [Rpc(SendTo.SpecifiedInParams)]
    void ShowWinnerRpc(string message, RpcParams rpcParams = default)
    {
        if (_winnerText) _winnerText.text = message;                        //show winner text
        if (_gameOverPanel) _gameOverPanel.SetActive(true);         //turn game over on
    }

    [ClientRpc]
    void UpdateScoreboardClientRpc(string scoreboard)
    {
        //update scoreboard on live for everyone
        if (_scoreboardText) _scoreboardText.text = scoreboard;
    }

    //tell all clients how many players are connected vs requierd (c/v) used in waiting room
    [ClientRpc]
    void UpdatePlayerCountClientRpc(int connected, int required)
    {
        if (_playerCountText)
            _playerCountText.text = $"{connected}/{required} player connected";
    }

    //State -> UI------------------------
    void OnStateChanged(GameState previous, GameState current)
    {
        RefreshUI(current);                 //update panels based on new state

        //fire events so other scripts can react
        if (current == GameState.Playing) OnGameStarted?.Invoke();      //PlayerPickUp resets on start
        if (current == GameState.GameOver) OnGameOver?.Invoke();        //SpawnManager stops on gam over
    }

    void OnTimeChanged(float previous, float current)
    {
        UpdateTimerText(current);       //update timer
    }

    void RefreshUI(GameState state)
    {
        //updates ui depending on current state
        if (_waitingPanel) _waitingPanel.SetActive(state == GameState.WaitingForPlayer);
        if (_gameOverPanel) _gameOverPanel.SetActive(state == GameState.GameOver);
        if (_timerText) _timerText.gameObject.SetActive(state == GameState.Playing);
        UpdateTimerText(_timeRemaining.Value);
    }

    void UpdateTimerText(float time)
    {
        if (_timerText == null) return;

        //convert seconds to mm:ss
        int m = Mathf.FloorToInt(time / 60f);
        int s = Mathf.FloorToInt(time % 60f);
        _timerText.text = $"{m:00}:{s:00}";
    }

    //used by SpawnManager and PlayerPickUp to check if game is running
    public bool IsPlaying() => _state.Value == GameState.Playing;
}