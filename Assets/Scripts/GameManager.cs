using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton { get; private set; }

    #region Changeable Constants

    [Header("Constant Parameters")] [SerializeField]
    private int secondsPauseAfterStich = 4;

    #endregion

    #region General

    public enum GameState
    {
        Waiting, // Waiting until 4 players are connected 
        GameRunning, // 4 players are connected, players can enter names, host can click button to deal cards
        PreRound, // Players take turns deciding the game mode, i.e. Sauspiel, Solo, etc.
        Round, // Active during a round (use in combination with CurrentRoundMode)
        RoundFinished // We enter this state after the last "Stich". Here we can show/count scores
    }

    [field: SyncVar] public GameState CurrentGameState { get; private set; }
    [field: SyncVar] private RoundMode CurrentRoundMode { get; set; }

    // private Dictionary<Player, PreRoundChoice> CurrentPreRoundChoices { get; } =
    //     new Dictionary<Player, PreRoundChoice>();

    [SyncVar(hook = nameof(OnGameStateTextChanged))]
    private string _gameStateText;

    [Header("General")] [SerializeField] private TextMeshProUGUI gameStateTextField;

    public enum RoundMode
    {
        Ramsch,
        Sauspiel,
        FarbSolo,
        FarbWenz,
        Wenz
    }

    private readonly List<Player> _players = new List<Player>();

    public List<Button> localPlayerCardButtons;
    public Button dealCardsButton;

    #endregion

    #region Card Deck

    private List<PlayingCard.PlayingCardInfo> _cardDeck;

    public class SyncListPlayingCard : SyncListStruct<PlayingCard.PlayingCardInfo>
    {
    }

    private readonly SyncListPlayingCard _syncListCardDeck = new SyncListPlayingCard();

    #endregion

    #region (Pre-) Round Management

    /// <summary>
    /// Relevant for both PreRound and Round.
    /// </summary>
    private Player _currentTurnPlayer;

    [Header("Pre-Round")] public GameObject preRoundButtonPanel;
    public Dropdown preRoundSauspielDropdown;
    public Dropdown preRoundSoloDropdown;
    public Dropdown preRoundWenzDropdown;
    public Button preRoundWeiterButton;

    private Player _roundStartingPlayer;

    /// <summary>
    /// The player making the strongest choice in the pre-round, thereby deciding the round mode 
    /// </summary>
    private Player CurrentPreRoundWinner { get; set; }

    /// <summary>
    /// round groups share their points and "play together"
    /// </summary>
    private List<List<Player>> _roundGroups = new List<List<Player>>();

    private class SyncListScoreBoard : SyncListStruct<Extensions.ScoreBoardRow>
    {
    }

    private readonly SyncListScoreBoard _scoreBoard = new SyncListScoreBoard();
    [SerializeField] private ScoreboardDisplay scoreboardDisplay;

    /// <summary>
    /// The Suit determined in the pre-round.
    /// For FarbSolo and FarbWenz, this is the Trump Suit chosen by the player.
    /// For Sauspiel, this is the chosen Suit that determines the teams.
    /// </summary>
    private PlayingCard.Suit CurrentRoundSuit { get; set; }


    [SerializeField] private Image[] playedCardSlots = new Image[4];

    private readonly SyncListPlayingCard _currentStich = new SyncListPlayingCard();
    
    [SyncVar(hook = nameof(OnStichWinnerChanged))]
    private PlayingCard.PlayingCardInfo _currentStichWinner;

    /// <summary>
    /// Holds the 8 stiches of a round. Is cleared before the start of every round.
    /// </summary>
    private readonly Dictionary<PlayingCard.Stich, Player> _completedStiches =
        new Dictionary<PlayingCard.Stich, Player>();

    [Header("Sounds")] public AudioClip[] soundPlayCardArray;
    public AudioClip soundShuffle;
    private AudioSource _audioSource;

    #endregion


    ///////////////////////////////////////////////
    /////////////////// Methods ///////////////////
    ///////////////////////////////////////////////

    // Start is called before the first frame update
    private void Awake()
    {
        Singleton = this;
        _cardDeck = PlayingCard.InitializeCardDeck();
        _currentStich.Callback = OnStichCardsChanged;
        _audioSource = GetComponent<AudioSource>();
    }

    public override void OnStartServer()
    {
        foreach (PlayingCard.PlayingCardInfo cardInfo in _cardDeck)
        {
            _syncListCardDeck.Add(cardInfo);
        }

        EnterStateWaiting();
    }

    #region Game State Transitions

    /// <summary>
    /// Used to enter the "WaitingForPlayers" state:
    /// * we are simply waiting until we have enough players
    /// </summary>
    private void EnterStateWaiting()
    {
        CurrentGameState = GameState.Waiting;
    }

    /// <summary>
    /// Used to enter the "Game Running" state
    /// * players can enter custom names
    /// * host can start a new round
    /// </summary>
    private void EnterStateGameRunning()
    {
        // check previous game state
        if (CurrentGameState == GameState.Waiting)
        {
            // set starting player to the last one so that before the first round the player 0 gets selected
            _roundStartingPlayer = _players[3];
        }

        // update the game state
        CurrentGameState = GameState.GameRunning;

        _gameStateText = "Bereit zum spielen";
        dealCardsButton.gameObject.SetActive(true);
        dealCardsButton.onClick.AddListener(EnterStatePreRound);
    }

    /// <summary>
    /// Used to enter the "Pre Round" state
    /// * Cards are dealt to the players
    /// * All the players are asked whether they want to "play" or "pass" (startingPlayer is asked first)
    /// * (if everybody passes, a Ramsch has to be initialized)
    /// </summary>
    private void EnterStatePreRound()
    {
        RpcHideScoreBoard();

        CurrentGameState = GameState.PreRound;
        _gameStateText = "Runde vorbereiten...";

        DealCards();

        dealCardsButton.gameObject.SetActive(false);
        _roundStartingPlayer = _players.CycleNext(_roundStartingPlayer);

        // the starting player decides first
        _currentTurnPlayer = _roundStartingPlayer;

        // We want to use Ramsch as the initial mode:
        // If everyone selects "Weiter", Ramsch is the correct round mode and we won't have to do anything
        CurrentRoundMode = RoundMode.Ramsch;

        // Display the Pre Round Buttons for the currently deciding player
        _currentTurnPlayer.RpcDisplayPreRoundButtons(CurrentRoundMode);
    }

    /// <summary>
    /// Used to enter the "Round" state
    /// </summary>
    private void EnterStateRound()
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"entering round with {nameof(CurrentRoundMode)}={CurrentRoundMode}.");

        CurrentGameState = GameState.Round;
        _currentTurnPlayer = _roundStartingPlayer;
        _completedStiches.Clear();

        // find out who is playing with whom, for easy scoring later
        _roundGroups = CalculateRoundGroups(_players, CurrentPreRoundWinner, CurrentRoundMode, CurrentRoundSuit);

        StartStich();
    }

    /// <summary>
    /// Used to enter the "Round Finished" state
    /// * count scores
    /// * show scoreboard
    /// * host can start next round
    /// </summary>
    private void EnterStateRoundFinished()
    {
        CurrentGameState = GameState.RoundFinished;
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  "Round finished!");

        _players.ForEach(player => player.handCards.Clear());

        UpdateScoreBoard();

        _gameStateText = $"Runde {_scoreBoard.Count} beendet";

        var playerNames = _players.Select(player => player.playerName).ToArray();
        RpcUpdateAndShowScoreBoard(playerNames);

        dealCardsButton.gameObject.SetActive(true);
    }

    [Server]
    private void UpdateScoreBoard()
    {
        // TODO maybe use a cumulative score, i.e. add the last row to this new row

        var roundScore = new Extensions.ScoreBoardRow();

        foreach (var player in _players)
        {
            int playerRoundScore = _completedStiches
                .Where(pair => pair.Value.Equals(player))
                .Sum(pair => pair.Key.Worth);

            // add the player's round score to each member of their group, including themselves
            foreach (var groupPlayer in _roundGroups.Find(group => group.Contains(player)))
            {
                roundScore.AddEntry(groupPlayer.playerName, playerRoundScore);
            }
        }

        _scoreBoard.Add(roundScore);
    }

    #endregion

    #region Server Stuff / Commands

    [Command]
    public void CmdHandlePreRoundChoice(NetworkInstanceId playerId, RoundMode playerChoiceRoundMode,
        PlayingCard.Suit playerChoiceRoundSuit)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"player {playerId} chose mode {playerChoiceRoundMode} + suit {playerChoiceRoundSuit}");

        // Compute the remaining options for next player
        ///////////////////////////////////////////////////////

        // we can essentially set the round mode to the player's choice, relying on the UI to not offer invalid choices
        // BUT... this does not apply for "Weiter": if the player chose "Weiter", we don't change the round mode
        if (playerChoiceRoundMode != RoundMode.Ramsch)
        {
            CurrentPreRoundWinner = _players.Find(player => player.netId == playerId);
            CurrentRoundMode = playerChoiceRoundMode;
            CurrentRoundSuit = playerChoiceRoundSuit;
        }

        // the round starting player is the first to choose, so if he is next, then all players have chosen
        bool allPlayersHaveChosen = _roundStartingPlayer.Equals(_players.CycleNext(_currentTurnPlayer));

        // it is not necessary that all players choose if this player chose Wenz
        bool preRoundFinished = (playerChoiceRoundMode == RoundMode.Wenz
                                 || playerChoiceRoundMode == RoundMode.FarbWenz
                                 || allPlayersHaveChosen);

        if (preRoundFinished)
        {
            // we don't have to do anything else here: the CurrentRoundMode is already set correctly (including Ramsch)
            EnterStateRound();
        }
        else
        {
            // otherwise display buttons to the next player
            _currentTurnPlayer = _players.CycleNext(_currentTurnPlayer);
            _currentTurnPlayer.RpcDisplayPreRoundButtons(CurrentRoundMode);
        }
    }

    /// <summary>
    /// Calculates the list of trump cards for this round, sorted by increasing precedence.
    /// The trump list depends on the current round mode, and the current round suit, both chosen during the pre-round.
    /// </summary>
    /// <param name="roundMode">The current round mode</param>
    /// <param name="roundSuit">The current round suit,
    ///     i.e. extra trump suit for FarbWenz/FarbSolo, or the "sought" Ace-Suit for Sauspiel</param>
    /// <returns>The list of cards that are currently trumps, sorted by increasing precedence</returns>
    /// <seealso cref="CurrentRoundSuit"/>
    public static List<PlayingCard.PlayingCardInfo> GetTrumpList(RoundMode roundMode, PlayingCard.Suit roundSuit)
    {
        List<PlayingCard.PlayingCardInfo> trumps = new List<PlayingCard.PlayingCardInfo>();

        // The "Unter"s are always trump
        trumps.AddRange(
            from PlayingCard.Suit suit in Enum.GetValues(typeof(PlayingCard.Suit))
            select new PlayingCard.PlayingCardInfo(suit, PlayingCard.Rank.Unter)
        );

        // For Wenz, we are already done
        if (roundMode == RoundMode.Wenz) return trumps;

        // For all other modes, add the additional Trump Suit BELOW the other trumps
        // (the user specified suit in case of FarbSolo/FarbWenz, or Herz in case of Sauspiel/Ramsch)
        PlayingCard.Suit additionalTrumpSuit = 
            (roundMode == RoundMode.FarbSolo || roundMode == RoundMode.FarbWenz) ? roundSuit : PlayingCard.Suit.Herz;
        trumps.InsertRange(0,
            from PlayingCard.Rank rank in Enum.GetValues(typeof(PlayingCard.Rank))
            where rank < PlayingCard.Rank.Unter
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
    /// Calculates the Teams/Groups for the current Round, i.e. the players whose scores will be shared.
    /// </summary>
    /// <param name="players">The list of players</param>
    /// <param name="roundModeDecider">The player that "won" the pre round decision, i.e. the player that decided the round mode</param>
    /// <param name="roundMode">The current round mode</param>
    /// <param name="roundSuit">The current round suit,
    ///     i.e. extra trump suit for FarbWenz/FarbSolo, or the "sought" Ace-Suit for Sauspiel</param>
    /// <returns>A List of Groups of Players</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static List<List<Player>> CalculateRoundGroups(List<Player> players, Player roundModeDecider,
        RoundMode roundMode, PlayingCard.Suit roundSuit)
    {
        List<List<Player>> roundGroups = new List<List<Player>>();

        // it all depends on the current round mode
        switch (roundMode)
        {
            case RoundMode.Ramsch:
                // everyone plays on their own, so each player is in a separate group
                foreach (var player in players)
                {
                    roundGroups.Add(new List<Player> {player});
                }

                break;

            case RoundMode.Sauspiel:
                // find the player that has the respective Sau
                Player sauOwner = players.Find(player => player.handCards.Contains(
                    new PlayingCard.PlayingCardInfo(roundSuit, PlayingCard.Rank.Ass)
                ));

                // the round mode decider is the one who was "seeking" the sau, so they are playing together
                var sauGroup = new List<Player> {sauOwner, roundModeDecider};
                roundGroups.Add(sauGroup);
                roundGroups.Add(players.Except(sauGroup).ToList());
                break;

            case RoundMode.FarbSolo:
            case RoundMode.FarbWenz:
            case RoundMode.Wenz:
                // the player who chose the solo/wenz is alone playing against the other 3 players
                var alonePlayerGroup = new List<Player> {roundModeDecider};
                roundGroups.Add(alonePlayerGroup);
                roundGroups.Add(players.Except(alonePlayerGroup).ToList());

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return roundGroups;
    }

    [Command]
    public void CmdHandlePlayCard(PlayingCard.PlayingCardInfo cardInfo)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"someone wants me (the server) to play {cardInfo}");

        // add the card to the played cards
        _currentStich.Add(cardInfo);

        bool stichComplete = _currentStich.Count == 4;

        if (stichComplete)
        {
            OnStichCompleted();
        }
        else
        {
            // initiate the next player's turn
            _currentTurnPlayer = _players.CycleNext(_currentTurnPlayer);
            _gameStateText = $"Runde läuft ({CurrentRoundMode})\n{_currentTurnPlayer.playerName} ist dran";
            _currentTurnPlayer.RpcStartTurn();
        }
    }

    [Server]
    private void OnStichCompleted()
    {
        PlayingCard.Stich currentStichStruct = new PlayingCard.Stich();
        currentStichStruct.AddAll(_currentStich.ToArray());

        // determine who won the stich
        _currentStichWinner = currentStichStruct.CalculateWinningCard(CurrentRoundMode, CurrentRoundSuit);
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"Server {netId} setting Stich Winner = {_currentStichWinner}");
        Player winningPlayer = _players.Cycle(_currentTurnPlayer, _currentStich.IndexOf(_currentStichWinner) + 1);

        // let the players know
        _gameStateText = $"{winningPlayer.playerName} gewinnt mit {_currentStichWinner}...";

        // add the stich to the completed stiches
        _completedStiches[currentStichStruct] = winningPlayer;

        // the winning player starts with the next stich
        _currentTurnPlayer = winningPlayer;

        // finish the stich after a small delaySecs, so that everyone can understand what happened
        StartCoroutine(StartNextStichWithDelay(secondsPauseAfterStich));
    }

    [Server]
    private IEnumerator StartNextStichWithDelay(int seconds)
    {
        // wait for the specified amount of time
        yield return new WaitForSeconds(seconds);

        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"Finished Stich {_completedStiches.Count}");

        // clear the stiches table and the current stich
        _currentStich.Clear();

        bool roundFinished = _completedStiches.Count == 8;
        if (roundFinished)
        {
            EnterStateRoundFinished();
        }
        else
        {
            StartStich();
        }
    }

    [Server]
    private void StartStich()
    {
        // notify the current player that it's their turn
        _currentTurnPlayer.RpcStartTurn();
        _gameStateText = $"Runde läuft ({CurrentRoundMode})\n{_currentTurnPlayer.playerName} ist dran";
    }

    [Server]
    public void AddPlayer(Player player)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"adding Player {player.netId}");
        _players.Add(player);

        _gameStateText = $"Warte auf {4 - _players.Count} Spieler... ";

        if (_players.Count == 4)
        {
            EnterStateGameRunning();
        }
    }

    /// <summary>
    /// Gives out the cards from the deck:
    /// card 00-07 to player 1, card 08-15 to player 2, card 16-23 to player 3, card 24-31 to player 4
    /// </summary>
    [Server]
    private void DealCards()
    {
        _syncListCardDeck.Shuffle();

        int dealtCards = 0;
        foreach (Player player in _players)
        {
            Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                      $"Player {player.netId} should get cards {dealtCards} to {dealtCards + 7}");
            // this updates a SyncList on the player server object, which then notifies his respective client object
            for (int i = dealtCards; i < dealtCards + 8; i++)
            {
                player.handCards.Add(_syncListCardDeck[i]);
            }

            dealtCards += 8;
        }

        // the following has to happen on (all) the clients, hence the RPC
        RpcOnDealCards();
    }

    #endregion

    #region SyncVar Callbacks/Hooks (Client)

    private void OnStichCardsChanged(SyncList<PlayingCard.PlayingCardInfo>.Operation op, int i)
    {
        switch (op)
        {
            case SyncList<PlayingCard.PlayingCardInfo>.Operation.OP_ADD:
                Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                          $"the server notified me that {_currentStich[i]} was played");

                // play a random card sound
                soundPlayCardArray.Shuffle();
                _audioSource.clip = soundPlayCardArray[0];
                _audioSource.Play();

                // put the correct image in the position of the just played card
                playedCardSlots[i].gameObject.SetActive(true);
                playedCardSlots[i].sprite = PlayingCard.SpriteDict[_currentStich[i]];
                break;
            case SyncList<PlayingCard.PlayingCardInfo>.Operation.OP_CLEAR:
                foreach (var cardSlot in playedCardSlots)
                {
                    cardSlot.gameObject.SetActive(false);
                    cardSlot.sprite = PlayingCard.DefaultCardSprite;
                    
                    // reset size back to normal to undo the "Highlighting" of the winner
                    cardSlot.rectTransform.localScale = Vector3.one;
                }

                break;
            default:
                throw new InvalidOperationException($"{nameof(op)}={op}");
        }
    }

    private void OnStichWinnerChanged(PlayingCard.PlayingCardInfo newStichWinner)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"Client {netId} was notified of Stich winner '{newStichWinner}'");
        Assert.IsTrue(_currentStich.Count == 4);

        int i = _currentStich.IndexOf(newStichWinner);
        
        // we expect the stich winner to be one of the played cards!
        Assert.AreNotEqual(-1, i);

        // slightly enlarge the card
        StartCoroutine(HighlightStichCard(i, secondsPauseAfterStich * 1f/3));
    }

    /// <summary>
    /// Highlights the card at the given index after a given time delaySecs in seconds
    /// </summary>
    /// <param name="index"></param>
    /// <param name="delaySecs"></param>
    /// <returns></returns>
    private IEnumerator HighlightStichCard(int index, float delaySecs)
    {
        yield return new WaitForSeconds(delaySecs);

        Image winnerImage = playedCardSlots[index];
        
        // slightly enlarge the card
        winnerImage.rectTransform.localScale = Vector3.one * 1.2f;
    }

    private void OnGameStateTextChanged(string newText)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"new text = \"{_gameStateText}\"");
        _gameStateText = newText;
        gameStateTextField.text = _gameStateText;
    }

    [ClientRpc]
    private void RpcOnDealCards()
    {
        // play the shuffle sound
        _audioSource.clip = soundShuffle;
        _audioSource.Play();
    }

    [ClientRpc]
    private void RpcUpdateAndShowScoreBoard(string[] playerNames)
    {
        scoreboardDisplay.AddScoreBoardRow(playerNames, _scoreBoard.Last());
        scoreboardDisplay.gameObject.SetActive(true);
    }

    [ClientRpc]
    private void RpcHideScoreBoard()
    {
        scoreboardDisplay.gameObject.SetActive(false);
    }

    #endregion
}