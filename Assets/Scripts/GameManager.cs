using UnityEngine;
using System;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { WaitingForPlayer, Playing, GameOver }

    public static event Action OnGameStarted;
    public static event Action OnGameOver;

    [Header("Game Settings")]
    [SerializeField] float _gameDuration = 60f;
    public static int _minClients = 1;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI _timerText;
    [SerializeField] GameObject _waitingPanel;
    [SerializeField] TextMeshProUGUI _playerCountText;
    [SerializeField] GameObject _gameOverPanel;
    [SerializeField] TextMeshProUGUI _winnerText;
    [SerializeField] TextMeshProUGUI _scoreboardText;
    [SerializeField] Button _playAgain;

    private NetworkVariable<float> _timeRemaining = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<GameState> _state = new(
        GameState.WaitingForPlayer,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Dictionary<ulong, int> _scores = new();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        _state.OnValueChanged += OnStateChanged;
        _timeRemaining.OnValueChanged += OnTimeChanged;

        if (IsServer)
        {
            _timeRemaining.Value = _gameDuration;
            _state.Value = GameState.WaitingForPlayer;

            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        if (_playAgain)
        {
            _playAgain.onClick.AddListener(OnPlayAgainClicked);
        }

        RefreshUI(_state.Value);
    }

    public override void OnNetworkDespawn()
    {
        _state.OnValueChanged -= OnStateChanged;
        _timeRemaining.OnValueChanged -= OnTimeChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    void HandleClientConnected(ulong clientId)
    {
        if (!IsServer || _state.Value != GameState.WaitingForPlayer) return;

        _scores[clientId] = 0;
        UpdatePlayerCountClientRpc(
            NetworkManager.Singleton.ConnectedClientsList.Count,
            _minClients + 1);

        int connectedClients = NetworkManager.Singleton.ConnectedClientsList.Count - 1;
        if (connectedClients >= _minClients)
        {
            StartGame();
        }
    }

    void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        _scores.Remove(clientId);
    }

    void StartGame()
    {
        _scores[NetworkManager.Singleton.LocalClientId] = 0;
        _state.Value = GameState.Playing;
    }
    private void Update()
    {
        if (!IsServer || _state.Value != GameState.Playing) return;

        _timeRemaining.Value -= Time.deltaTime;

        if (_timeRemaining.Value <= 0)
        {
            _timeRemaining.Value = 0f;
            EndGame();
        }
    }

    void EndGame()
    {
        _state.Value = GameState.GameOver;
        SendPersonalizedResults();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddScoreRpc(int points, ulong clientId)
    {
        if (!_scores.ContainsKey(clientId)) return;
        
        _scores[clientId] += points;

        //send scoreboard to everyplayer everytime score changes
        Scoreboard();
    }

    void Scoreboard()
    {
        //show score of players
        var lines = new List<string>();
        foreach (var kpv in _scores)
        {
            int playerNum = (int)kpv.Key + 1;
            string label = $"Player {kpv.Key + 1}";
            lines.Add($"{label}: {kpv.Value} pts");
        }

        UpdateScoreboardClientRpc(string.Join("\n", lines));
    }
    
    void SendPersonalizedResults()
    {
        // Find the actual winner
        ulong winnerId = 0;
        int topScore = -1;
        bool isTie = false;

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

            //target this RPC to one specific client
            ShowWinnerRpc(message, RpcTarget.Single(kvp.Key, RpcTargetUse.Temp));
        }
    }

    void OnPlayAgainClicked()
    {
        //any player can request but host decides
        RequestRestartRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestRestartRpc()
    {
        if (_state.Value != GameState.GameOver) return;

        //reset score for all connected players
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            _scores[clientId] = 0;

        //send empty scoreboard and restart time before going to waiting room
        Scoreboard();
        _timeRemaining.Value = _gameDuration;

        //check if enough players are connected
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


    //Rpc to clients--------------------
    [Rpc(SendTo.SpecifiedInParams)]
    void ShowWinnerRpc(string message, RpcParams rpcParams = default)
    {
        if (_winnerText) _winnerText.text = message;
        if (_gameOverPanel) _gameOverPanel.SetActive(true);
    }

    [ClientRpc]
    void UpdateScoreboardClientRpc(string scoreboard)
    {
        if (_scoreboardText) _scoreboardText.text = scoreboard;
    }

    [ClientRpc]
    void UpdatePlayerCountClientRpc(int connected, int required)
    {
        if (_playerCountText)
            _playerCountText.text = $"{connected}/{required} player connected";
    }

    //State -> UI------------------------
    void OnStateChanged(GameState previous, GameState current)
    {
        RefreshUI(current);

        if (current == GameState.Playing) OnGameStarted?.Invoke();
        if (current == GameState.GameOver) OnGameOver?.Invoke();
    }

    void OnTimeChanged(float previous, float current)
    {
        UpdateTimerText(current);
    }

    void RefreshUI(GameState state)
    {
        if (_waitingPanel) _waitingPanel.SetActive(state == GameState.WaitingForPlayer);
        if (_gameOverPanel) _gameOverPanel.SetActive(state == GameState.GameOver);
        if (_timerText) _timerText.gameObject.SetActive(state == GameState.Playing);
        UpdateTimerText(_timeRemaining.Value);
    }

    void UpdateTimerText(float time)
    {
        if (_timerText == null) return;

        int m = Mathf.FloorToInt(time / 60f);
        int s = Mathf.FloorToInt(time % 60f);
        _timerText.text = $"{m:00}:{s:00}";
    }

    public bool IsPlaying() => _state.Value == GameState.Playing;
}
