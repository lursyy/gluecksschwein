using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton;

    #region Game State Stuff

    public enum GameState
    {
        Waiting,        // Waiting until 4 players are connected 
        GameRunning,    // 4 players are connected, players can enter names, host can click button to deal cards
        PreRound,       // Players take turns deciding the game mode, i.e. Sauspiel, Solo, etc.
        Round,          // Active during a round (use in combination with CurrentRoundMode)
        RoundFinished   // We enter this state after the last "Stich". Here we can show/count scores
    }

    [field: SyncVar] public GameState CurrentGameState { get; private set; }
    [field: SyncVar] public RoundMode CurrentRoundMode { get; private set; }

    private Dictionary<NetworkInstanceId, PreRoundChoice> CurrentPreRoundChoices { get; } =
        new Dictionary<NetworkInstanceId, PreRoundChoice>();

    [SyncVar(hook = nameof(OnGameStateTextChanged))] [SerializeField]
    private string gameStateText;

    [SerializeField] private Text gameStateTextField;
    
    public enum RoundMode
    {
        Ramsch,
        SauspielBlatt,
        SauspielEichel,
        SauspielSchelln,
        Solo,
        Wenz,
    }
    
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum PreRoundChoice
    {
        Weiter,
        SauspielBlatt,
        SauspielEichel,
        SauspielSchelln,
        Solo,
        Wenz,
    }

    #endregion

    #region Player Management
    
    [Header("Players")]
    public List<Player> players = new List<Player>();
    public List<Button> localPlayerCardButtons;
    public Button dealCardsButton;
    private Player _roundStartingPlayer; 
    
    [Header("Pre-Round")]
    public GameObject preRoundButtonPanel; 
    public Dropdown preRoundSauspielDropdown;
    public Button preRoundSoloButton;
    public Button preRoundWenzButton;
    public Button preRoundWeiterButton;

    #endregion

    #region Deck Management

    private List<PlayingCard.PlayingCardInfo> _cardDeck;
    public class SyncListPlayingCard : SyncListStruct<PlayingCard.PlayingCardInfo> { }
    private readonly SyncListPlayingCard _syncListCardDeck = new SyncListPlayingCard();

    #endregion

    #region Round Management
    
    [SerializeField] private List<GameObject> playedCardSlots;
    private readonly SyncListPlayingCard _playedCards = new SyncListPlayingCard();
    private Player _currentPreRoundDecider;

    #endregion

    
    ///////////////////////////////////////////////
    /////////////////// Methods ///////////////////
    ///////////////////////////////////////////////

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
    /// * show scoreboard
    /// * host can start a new round
    /// </summary>
    private void EnterStateGameRunning()
    {
        // check previous game state
        if (CurrentGameState == GameState.Waiting)
        {
            // set starting player to the last one so that before the first round the player 0 gets selected
            _roundStartingPlayer = players[3];
        }

        // update the game state
        CurrentGameState = GameState.GameRunning;

        gameStateText = "Bereit zum spielen";
        dealCardsButton.gameObject.SetActive(true);
        dealCardsButton.onClick.AddListener(EnterStatePreRound);
        // TODO: show scoreboard
    }

    /// <summary>
    /// Used to enter the "Pre Round" state
    /// * Cards are dealt to the players
    /// * All the players are asked whether they want to "play" or "pass" (startingPlayer is asked first)
    /// * (if everybody passes, a Ramsch has to be initialized)
    /// </summary>
    private void EnterStatePreRound()
    {
        // update the game state
        CurrentGameState = GameState.PreRound;
        gameStateText = "Runde vorbereiten...";

        // deal the cards to the players
        DealCards();
        
        // disable the dealCards Button
        dealCardsButton.gameObject.SetActive(false);

        // select the next starting player
        _roundStartingPlayer = players.CycleNext(_roundStartingPlayer);
        
        // the starting player decides first
        _currentPreRoundDecider = _roundStartingPlayer;
        
        // reset the Round Mode
        CurrentRoundMode = RoundMode.Ramsch;
        
        // reset the player choices
        CurrentPreRoundChoices.Clear();
        
        // Display the Pre Round Buttons for the currently deciding player
        _currentPreRoundDecider.RpcDisplayPreRoundButtons(CurrentRoundMode);
    }
    
    /// <summary>
    /// Used to enter the "Round" state
    /// </summary>
    private void EnterStateRound()
    {
        CurrentGameState = GameState.Round;
        gameStateText = $"Runde läuft ({CurrentRoundMode})";
        
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"entering round with {nameof(CurrentRoundMode)}={CurrentRoundMode}.");
        
        
    }
    
    /// <summary>
    /// Used to enter the "Round Finished" state
    /// </summary>
    private void EnterStateRoundFinished()
    {
        CurrentGameState = GameState.RoundFinished;
    }

    [Command]
    public void CmdHandlePreRoundChoice(NetworkInstanceId playerId, PreRoundChoice playerChoice)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"player {playerId} chose {playerChoice}");
        
        // Now we compute the remaining options for next player
        ///////////////////////////////////////////////////////
         
        // we can essentially set the round mode to the player's choice, we only chooses from the remaining (legal) choices
        // BUT... this does not apply for "Weiter": if the player chose "Weiter", we don't change the round mode
        if (playerChoice != PreRoundChoice.Weiter) 
        {
            CurrentRoundMode = (RoundMode) playerChoice;
        }

        // maybe we want to show the choices to all players at some point
        CurrentPreRoundChoices[playerId] = playerChoice;

        bool preRoundFinished = playerChoice == PreRoundChoice.Wenz || CurrentPreRoundChoices.Count == players.Count;
        
        if (preRoundFinished)
        {
            // we don't have to do anything else here: the CurrentRoundMode is already set correctly (including Ramsch)
            EnterStateRound();
        }
        else 
        {
            // otherwise display buttons to the next player
            _currentPreRoundDecider = players.CycleNext(_currentPreRoundDecider);
            _currentPreRoundDecider.RpcDisplayPreRoundButtons(CurrentRoundMode);
        }
    }
    
    #endregion
    
    // Start is called before the first frame update
    private void Awake()
    {
        Singleton = this;
        _cardDeck = PlayingCard.InitializeCardDeck();
        _playedCards.Callback = OnCardPlayed;
    }

    public override void OnStartServer()
    {
        foreach (PlayingCard.PlayingCardInfo cardInfo in _cardDeck)
        {
            _syncListCardDeck.Add(cardInfo);
        }

        EnterStateWaiting();
    }

    #region Server Stuff / Commands

    [Command]
    public void CmdPlayCard(PlayingCard.PlayingCardInfo cardInfo)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"someone wants me (the server) to play {cardInfo}");

        // add the card to the played cards
        _playedCards.Add(cardInfo);
    }
    
    [Server]
    public void AddPlayer(Player player)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"adding Player {player.netId}");
        players.Add(player);
        
        gameStateText = $"Warte auf Spieler... ({players.Count})";

        if (players.Count == 4)
        {
            EnterStateGameRunning();
        }
    }

    /// <summary>
    /// Gives out the cards from the deck:
    /// card 00-07 to player 1, card 08-15 to player 2, card 16-23 to player 3, card 24-31 to player 4
    /// </summary>
    [Server]
    public void DealCards()
    {
        int handedCards = 0;
        foreach (Player player in players)
        {
            Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                      $"Player {player.netId} should get cards {{handedCards}} to {{handedCards + 7}}");
            // this updates the player's cards on the server object, which then notifies his respective client object
            for (int i = handedCards; i < handedCards + 8; i++)
            {
                player.handCards.Add(_syncListCardDeck[i]);
            }

            handedCards += 8;
        }
    }

    #endregion

    #region SyncVar Callbacks/Hooks

    private void OnCardPlayed(SyncList<PlayingCard.PlayingCardInfo>.Operation op, int i)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"the server notified me that {_playedCards[i]} was played");

        // put the correct image in the position of the just played card
        playedCardSlots[i].SetActive(true);
        playedCardSlots[i].GetComponent<Image>().sprite = PlayingCard.SpriteDict[_playedCards[i]];
    }

    private void OnGameStateTextChanged(string newText)
    {
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"new text = \"{gameStateText}\"");
        gameStateText = newText;
        gameStateTextField.text = gameStateText;
    }

    #endregion
}