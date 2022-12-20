using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class GameManager : NetworkBehaviour
{
    #region Changeable Constants

    [Header("Constant Parameters")] [SerializeField]
    private int secondsPauseAfterStich = 4;

    #endregion

    public static GameManager Singleton { get; private set; }
    private GameManager() { }
    
    ///////////////////////////////////////////////
    /////////////////// Methods ///////////////////
    ///////////////////////////////////////////////

    // Start is called before the first frame update
    private void Awake()
    {
        Singleton = this;
        SyncListCardDeck = new NetworkList<PlayingCard.PlayingCardInfo>();
        ScoreBoard = new NetworkList<Extensions.ScoreBoardRow>();
        CurrentStich = new NetworkList<PlayingCard.PlayingCardInfo>();
        CurrentStich.OnListChanged += OnStichCardsChanged;
        ScoreBoard.OnListChanged += OnScoreBoardChanged;
        _gameStateText.OnValueChanged = OnGameStateTextChanged;
        _currentStichWinner.OnValueChanged = OnStichWinnerChanged;
        
        _cardDeck = PlayingCard.InitializeCardDeck();
        _audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        foreach (var cardInfo in _cardDeck) SyncListCardDeck.Add(cardInfo);
        EnterStateWaiting();
    }

    #region General

    private string _joinCode = "";
    private bool _joinCodeSet;

    public void SetJoinCode(string joinCode)
    {
        if (_joinCodeSet) return;
        _joinCode = joinCode;
        _joinCodeSet = true;
    }
    
    public enum GameState
    {
        Waiting, // Waiting until 4 players are connected 
        GameRunning, // 4 players are connected, players can enter names, host can click button to deal cards
        PreRound, // Players take turns deciding the game mode, i.e. Sauspiel, Solo, etc.
        Round, // Active during a round (use in combination with CurrentRoundMode)
        RoundFinished // We enter this state after the last "Stich". Here we can show/count scores
    }

    public NetworkVariable<GameState> CurrentGameState { get; private set; } = new();
    private NetworkVariable<RoundMode> CurrentRoundMode { get; set; } = new();

    // private Dictionary<Player, PreRoundChoice> CurrentPreRoundChoices { get; } =
    //     new Dictionary<Player, PreRoundChoice>();

    private NetworkVariable<FixedString128Bytes> _gameStateText = new();

    [Header("General")] [SerializeField] private TextMeshProUGUI gameStateTextField;

    public enum RoundMode
    {
        Ramsch,
        Sauspiel,
        FarbSolo,
        FarbWenz,
        Wenz
    }

    private readonly List<Player> _players = new();
    
    public List<Button> localPlayerCardButtons;
    public TMP_InputField playerNameInput;
    public Button dealCardsButton;

    #endregion

    #region Card Deck

    private List<PlayingCard.PlayingCardInfo> _cardDeck;

    public NetworkList<PlayingCard.PlayingCardInfo> SyncListCardDeck { get; private set; }

    #endregion

    #region (Pre-) Round Management

    /// <summary>
    ///     Relevant for both PreRound and Round.
    /// </summary>
    private Player _currentTurnPlayer;

    [Header("Pre-Round")] public GameObject preRoundButtonPanel;
    public Dropdown preRoundSauspielDropdown;
    public Dropdown preRoundSoloDropdown;
    public Dropdown preRoundWenzDropdown;
    public Button preRoundWeiterButton;

    private Player _roundStartingPlayer;

    /// <summary>
    ///     The player making the strongest choice in the pre-round, thereby deciding the round mode
    /// </summary>
    private Player CurrentPreRoundWinner { get; set; }

    /// <summary>
    ///     round groups share their points and "play together"
    /// </summary>
    private List<List<Player>> _roundGroups = new();

    public NetworkList<Extensions.ScoreBoardRow> ScoreBoard { get; private set; }
    [SerializeField] private ScoreboardDisplay scoreboardDisplay;

    /// <summary>
    ///     The Suit determined in the pre-round.
    ///     For FarbSolo and FarbWenz, this is the Trump Suit chosen by the player.
    ///     For Sauspiel, this is the chosen Suit that determines the teams.
    /// </summary>
    private PlayingCard.Suit CurrentRoundSuit { get; set; }


    [SerializeField] private Image[] playedCardSlots = new Image[4];

    public NetworkList<PlayingCard.PlayingCardInfo> CurrentStich { get; private set; }

    private readonly NetworkVariable<PlayingCard.PlayingCardInfo> _currentStichWinner = new();

    /// <summary>
    ///     Holds the 8 stiches of a round. Is cleared before the start of every round.
    /// </summary>
    private readonly Dictionary<PlayingCard.Stich, Player> _completedStiches = new();

    [Header("Sounds")] public AudioClip[] soundPlayCardArray;
    public AudioClip soundShuffle;
    private AudioSource _audioSource;

    #endregion

    #region Game State Transitions

    /// <summary>
    ///     Used to enter the "WaitingForPlayers" state:
    ///     * we are simply waiting until we have enough players
    /// </summary>
    private void EnterStateWaiting()
    {
        CurrentGameState.Value = GameState.Waiting;
    }

    /// <summary>
    ///     Used to enter the "Game Running" state
    ///     * players can enter custom names
    ///     * host can start a new round
    /// </summary>
    private void EnterStateGameRunning()
    {
        // check previous game state
        if (CurrentGameState.Value == GameState.Waiting)
            // set starting player to the last one so that before the first round the player 0 gets selected
            _roundStartingPlayer = _players[3];

        // update the game state
        CurrentGameState.Value = GameState.GameRunning;

        _gameStateText.Value = "Bereit zum spielen";
        dealCardsButton.gameObject.SetActive(true);
        dealCardsButton.onClick.AddListener(EnterStatePreRound);
    }

    /// <summary>
    ///     Used to enter the "Pre Round" state
    ///     * Cards are dealt to the players
    ///     * All the players are asked whether they want to "play" or "pass" (startingPlayer is asked first)
    ///     * (if everybody passes, a Ramsch has to be initialized)
    /// </summary>
    private void EnterStatePreRound()
    {
        HideScoreBoardClientRpc();

        _roundStartingPlayer = _players.CycleNext(_roundStartingPlayer);
        // the starting player decides first
        _currentTurnPlayer = _roundStartingPlayer;
        
        CurrentGameState.Value = GameState.PreRound;
        _gameStateText.Value = $"Runde vorbereiten...\n{_currentTurnPlayer.PlayerName} ist dran";

        DealCards();

        dealCardsButton.gameObject.SetActive(false);

        // We want to use Ramsch as the initial mode:
        // If everyone selects "Weiter", Ramsch is the correct round mode and we won't have to do anything
        CurrentRoundMode.Value = RoundMode.Ramsch;

        // Display the Pre Round Buttons for the currently deciding player
        _currentTurnPlayer.DisplayPreRoundButtonsClientRpc(CurrentRoundMode.Value);
    }

    /// <summary>
    ///     Used to enter the "Round" state
    /// </summary>
    private void EnterStateRound()
    {
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"entering round with {nameof(CurrentRoundMode)}={CurrentRoundMode}.");

        CurrentGameState.Value = GameState.Round;
        _currentTurnPlayer = _roundStartingPlayer;
        _completedStiches.Clear();

        // find out who is playing with whom, for easy scoring later
        _roundGroups = CalculateRoundGroups(_players, CurrentPreRoundWinner, CurrentRoundMode.Value, CurrentRoundSuit);

        StartStich();
    }

    /// <summary>
    ///     Used to enter the "Round Finished" state
    ///     * count scores
    ///     * show scoreboard
    ///     * host can start next round
    /// </summary>
    private void EnterStateRoundFinished()
    {
        CurrentGameState.Value = GameState.RoundFinished;
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  "Round finished!");

        _players.ForEach(player => player.HandCards.Clear());

        UpdateScoreBoard();

        _gameStateText.Value = $"Runde {ScoreBoard.Count} beendet";

        dealCardsButton.gameObject.SetActive(true);
    }

    private void UpdateScoreBoard()
    {
        // TODO maybe use a cumulative score, i.e. add the last row to this new row
        var roundScores = _players.ToDictionary(player => player.PlayerName, player => 0);

        foreach (var player in _players)
        {
            var playerRoundScore = _completedStiches
                .Where(pair => pair.Value.Equals(player))
                .Sum(pair => pair.Key.Worth);

            // add the player's round score to each member of their group, including themselves
            foreach (var groupPlayer in _roundGroups.Find(group => group.Contains(player)))
                roundScores[groupPlayer.PlayerName] += playerRoundScore;
        }

        var entries = new Extensions.ScoreBoardEntry[4];
        for (var i = 0; i < entries.Length; i++)
        {
            var (playerName, score) = roundScores.ToArray()[i];
            entries[i] = new Extensions.ScoreBoardEntry(playerName, score);
        }

        ScoreBoard.Add(new Extensions.ScoreBoardRow(entries));
    }

    #endregion

    #region Server Stuff / Commands

    [ServerRpc]
    public void HandlePreRoundChoiceServerRpc(ulong playerId, RoundMode playerChoiceRoundMode,
        PlayingCard.Suit playerChoiceRoundSuit)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"player {playerId} chose mode {playerChoiceRoundMode} + suit {playerChoiceRoundSuit}");

        // Compute the remaining options for next player
        ///////////////////////////////////////////////////////

        // we can essentially set the round mode to the player's choice, relying on the UI to not offer invalid choices
        // BUT... this does not apply for "Weiter": if the player chose "Weiter", we don't change the round mode
        if (playerChoiceRoundMode != RoundMode.Ramsch)
        {
            CurrentPreRoundWinner = _players.Find(player => player.NetworkObjectId == playerId);
            CurrentRoundMode.Value = playerChoiceRoundMode;
            CurrentRoundSuit = playerChoiceRoundSuit;
        }

        // the round starting player is the first to choose, so if he is next, then all players have chosen
        var allPlayersHaveChosen = _roundStartingPlayer.Equals(_players.CycleNext(_currentTurnPlayer));

        // it is not necessary that all players choose if this player chose Wenz
        var preRoundFinished = playerChoiceRoundMode == RoundMode.Wenz
                               || playerChoiceRoundMode == RoundMode.FarbWenz
                               || allPlayersHaveChosen;

        if (preRoundFinished)
        {
            // we don't have to do anything else here: the CurrentRoundMode is already set correctly (including Ramsch)
            EnterStateRound();
        }
        else
        {
            // otherwise display buttons to the next player
            _currentTurnPlayer = _players.CycleNext(_currentTurnPlayer);
            _gameStateText.Value = $"Runde vorbereiten...\n{_currentTurnPlayer.PlayerName} ist dran";
            _currentTurnPlayer.DisplayPreRoundButtonsClientRpc(CurrentRoundMode.Value);
        }
    }

    /// <summary>
    ///     Calculates the list of trump cards for this round, sorted by increasing precedence.
    ///     The trump list depends on the current round mode, and the current round suit, both chosen during the pre-round.
    /// </summary>
    /// <param name="roundMode">The current round mode</param>
    /// <param name="roundSuit">
    ///     The current round suit,
    ///     i.e. extra trump suit for FarbWenz/FarbSolo, or the "sought" Ace-Suit for Sauspiel
    /// </param>
    /// <returns>The list of cards that are currently trumps, sorted by increasing precedence</returns>
    /// <seealso cref="CurrentRoundSuit" />
    public static List<PlayingCard.PlayingCardInfo> GetTrumpList(RoundMode roundMode, PlayingCard.Suit roundSuit)
    {
        var trumps = new List<PlayingCard.PlayingCardInfo>();

        // The "Unter"s are always trump
        trumps.AddRange(
            from PlayingCard.Suit suit in Enum.GetValues(typeof(PlayingCard.Suit))
            select new PlayingCard.PlayingCardInfo(suit, PlayingCard.Rank.Unter)
        );

        // For Wenz, we are already done
        if (roundMode == RoundMode.Wenz) return trumps;

        // For all other modes, add the additional Trump Suit BELOW the other trumps
        // (the user specified suit in case of FarbSolo/FarbWenz, or Herz in case of Sauspiel/Ramsch)
        var additionalTrumpSuit = roundMode == RoundMode.FarbSolo || roundMode == RoundMode.FarbWenz
            ? roundSuit
            : PlayingCard.Suit.Herz;

        // we insert every card except the "Unter"s (and except the "Ober"s if we are not in Wenz)

        trumps.InsertRange(0,
            from PlayingCard.Rank rank in Enum.GetValues(typeof(PlayingCard.Rank))
            where rank != PlayingCard.Rank.Unter // Exclude "Unter"s
            where rank != PlayingCard.Rank.Ober ||
                  roundMode == RoundMode.FarbWenz // Exclude "Ober"s if we are in Sauspiel/Solo/Ramsch
            select new PlayingCard.PlayingCardInfo(additionalTrumpSuit, rank)
        );

        // For FarbWenz, we are done here
        if (roundMode == RoundMode.FarbWenz) return trumps;

        // For FarbSolo, Sauspiel, Ramsch add the "Ober"s at the top
        trumps.AddRange(
            from PlayingCard.Suit suit in Enum.GetValues(typeof(PlayingCard.Suit))
            select new PlayingCard.PlayingCardInfo(suit, PlayingCard.Rank.Ober)
        );

        return trumps;
    }

    /// <summary>
    ///     Calculates the Teams/Groups for the current Round, i.e. the players whose scores will be shared.
    /// </summary>
    /// <param name="players">The list of players</param>
    /// <param name="roundModeDecider">
    ///     The player that "won" the pre round decision, i.e. the player that decided the round
    ///     mode
    /// </param>
    /// <param name="roundMode">The current round mode</param>
    /// <param name="roundSuit">
    ///     The current round suit,
    ///     i.e. extra trump suit for FarbWenz/FarbSolo, or the "sought" Ace-Suit for Sauspiel
    /// </param>
    /// <returns>A List of Groups of Players</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static List<List<Player>> CalculateRoundGroups(List<Player> players, Player roundModeDecider,
        RoundMode roundMode, PlayingCard.Suit roundSuit)
    {
        var roundGroups = new List<List<Player>>();

        // it all depends on the current round mode
        switch (roundMode)
        {
            case RoundMode.Ramsch:
                // everyone plays on their own, so each player is in a separate group
                foreach (var player in players) roundGroups.Add(new List<Player> { player });

                break;

            case RoundMode.Sauspiel:
                // find the player that has the respective Sau
                var sauOwner = players.Find(player => player.HandCards.Contains(
                    new PlayingCard.PlayingCardInfo(roundSuit, PlayingCard.Rank.Ass)
                ));

                // the round mode decider is the one who was "seeking" the sau, so they are playing together
                var sauGroup = new List<Player> { sauOwner, roundModeDecider };
                roundGroups.Add(sauGroup);
                roundGroups.Add(players.Except(sauGroup).ToList());
                break;

            case RoundMode.FarbSolo:
            case RoundMode.FarbWenz:
            case RoundMode.Wenz:
                // the player who chose the solo/wenz is alone playing against the other 3 players
                var alonePlayerGroup = new List<Player> { roundModeDecider };
                roundGroups.Add(alonePlayerGroup);
                roundGroups.Add(players.Except(alonePlayerGroup).ToList());

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return roundGroups;
    }

    [ServerRpc]
    public void HandlePlayCardServerRpc(PlayingCard.PlayingCardInfo cardInfo)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"someone wants me (the server) to play {cardInfo}");

        // add the card to the played cards
        CurrentStich.Add(cardInfo);

        var stichComplete = CurrentStich.Count == 4;

        if (stichComplete)
        {
            OnStichCompleted();
        }
        else
        {
            // initiate the next player's turn
            _currentTurnPlayer = _players.CycleNext(_currentTurnPlayer);
            _gameStateText.Value = $"Runde läuft ({CurrentRoundMode.Value})\n{_currentTurnPlayer.PlayerName} ist dran";
            _currentTurnPlayer.StartTurnClientRpc();
        }
    }

    private void OnStichCompleted()
    {
        var currentStichStruct = new PlayingCard.Stich();
        foreach (PlayingCard.PlayingCardInfo cardInfo in CurrentStich)
        {
            currentStichStruct.AddCard(cardInfo);
        }

        // determine who won the stich
        _currentStichWinner.Value = currentStichStruct.CalculateWinningCard(CurrentRoundMode.Value, CurrentRoundSuit);
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"Server {NetworkObjectId} setting Stich Winner = {_currentStichWinner}");
        var winningPlayer = _players.Cycle(_currentTurnPlayer, CurrentStich.IndexOf(_currentStichWinner.Value) + 1);

        // let the players know
        _gameStateText.Value = $"{winningPlayer.PlayerName} gewinnt mit {_currentStichWinner.Value}...";

        // add the stich to the completed stiches
        _completedStiches[currentStichStruct] = winningPlayer;

        // the winning player starts with the next stich
        _currentTurnPlayer = winningPlayer;

        // finish the stich after a small delaySecs, so that everyone can understand what happened
        StartCoroutine(StartNextStichWithDelay(secondsPauseAfterStich));
    }

    private IEnumerator StartNextStichWithDelay(int seconds)
    {
        // wait for the specified amount of time
        yield return new WaitForSeconds(seconds);

        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"Finished Stich {_completedStiches.Count}");

        // clear the stiches table and the current stich
        CurrentStich.Clear();

        var roundFinished = _completedStiches.Count == 8;
        if (roundFinished)
            EnterStateRoundFinished();
        else
            StartStich();
    }

    private void StartStich()
    {
        if (!IsServer) throw new InvalidOperationException(); // sanity check
        // notify the current player that it's their turn
        _currentTurnPlayer.StartTurnClientRpc();
        _gameStateText.Value = $"Runde läuft ({CurrentRoundMode.Value})\n{_currentTurnPlayer.PlayerName} ist dran";
    }

    public void AddPlayer(Player player)
    {
        if (!NetworkManager.Singleton.IsServer) throw new InvalidOperationException(); // sanity check TODO not sure if this checks what I want
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"adding Player {player.NetworkObjectId}");
        _players.Add(player);
        
        _gameStateText.Value = $"Warte auf {4 - _players.Count} Spieler...\n" +
                               $"Spiel-ID: {_joinCode}";

        if (_players.Count == 4) EnterStateGameRunning();
    }

    /// <summary>
    ///     Gives out the cards from the deck:
    ///     card 00-07 to player 1, card 08-15 to player 2, card 16-23 to player 3, card 24-31 to player 4
    /// </summary>
    private void DealCards()
    {
        if (!IsServer) throw new InvalidOperationException(); // sanity check
        SyncListCardDeck.Shuffle();

        var dealtCards = 0;
        foreach (var player in _players)
        {
            Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                      $"Player {player.NetworkObjectId} should get cards {dealtCards} to {dealtCards + 7}");
            // this updates a SyncList on the player server object, which then notifies his respective client object
            for (var i = dealtCards; i < dealtCards + 8; i++) player.HandCards.Add(SyncListCardDeck[i]);

            dealtCards += 8;
        }

        // the following has to happen on (all) the clients, hence the RPC
        OnDealCardsClientRpc();
    }

    #endregion

    #region SyncVar Callbacks/Hooks (Client)

    private void OnStichCardsChanged(NetworkListEvent<PlayingCard.PlayingCardInfo> changeEvent)
    {
        int i = changeEvent.Index;
        switch (changeEvent.Type)
        {
            case NetworkListEvent<PlayingCard.PlayingCardInfo>.EventType.Add:
                Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                          $"the server notified me that {CurrentStich[i]} was played");

                // play a random card sound
                soundPlayCardArray.Shuffle();
                _audioSource.clip = soundPlayCardArray[0];
                _audioSource.Play();

                // put the correct image in the position of the just played card
                playedCardSlots[i].gameObject.SetActive(true);
                playedCardSlots[i].sprite = PlayingCard.SpriteDict[CurrentStich[i]];
                break;
            case NetworkListEvent<PlayingCard.PlayingCardInfo>.EventType.Clear:
                foreach (var cardSlot in playedCardSlots)
                {
                    cardSlot.gameObject.SetActive(false);
                    cardSlot.sprite = PlayingCard.DefaultCardSprite;

                    // reset size back to normal to undo the "Highlighting" of the winner
                    cardSlot.rectTransform.localScale = Vector3.one;
                }

                break;
            default:
                throw new InvalidOperationException($"changeEvent={changeEvent.Type}");
        }
    }

    private void OnStichWinnerChanged(PlayingCard.PlayingCardInfo oldValue, PlayingCard.PlayingCardInfo newValue)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"Client {NetworkObjectId} was notified of Stich winner '{newValue}'");
        Assert.IsTrue(CurrentStich.Count == 4);

        var i = CurrentStich.IndexOf(newValue);

        // we expect the stich winner to be one of the played cards!
        Assert.AreNotEqual(-1, i);

        // slightly enlarge the card
        StartCoroutine(HighlightStichCard(i, secondsPauseAfterStich * 1f / 3));
    }
    
    /// <summary>
    ///     Highlights the card at the given index after a given time delaySecs in seconds
    /// </summary>
    /// <param name="index"></param>
    /// <param name="delaySecs"></param>
    /// <returns></returns>
    private IEnumerator HighlightStichCard(int index, float delaySecs)
    {
        yield return new WaitForSeconds(delaySecs);

        var winnerImage = playedCardSlots[index];

        // slightly enlarge the card
        winnerImage.rectTransform.localScale = Vector3.one * 1.2f;
    }

    private void OnGameStateTextChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod()?.DeclaringType}::{MethodBase.GetCurrentMethod()?.Name}: " +
                  $"new text = \"{newValue}\"");
        gameStateTextField.text = newValue.ToString();
    }
    
    private void OnScoreBoardChanged(NetworkListEvent<Extensions.ScoreBoardRow> changeevent)
    {
        if (changeevent.Type != NetworkListEvent<Extensions.ScoreBoardRow>.EventType.Add) return;
        // var playerNames = _players.Select(p => new SerializableString(p.PlayerName)).ToArray();
        scoreboardDisplay.AddScoreBoardRow(changeevent.Value);
        scoreboardDisplay.gameObject.SetActive(true);
    }

    [ClientRpc]
    private void OnDealCardsClientRpc()
    {
        // play the shuffle sound
        _audioSource.clip = soundShuffle;
        _audioSource.Play();
    }

    [ClientRpc]
    private void HideScoreBoardClientRpc()
    {
        scoreboardDisplay.gameObject.SetActive(false);
    }

    #endregion
}