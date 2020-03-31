using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton;

    #region General

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

    [Header("Players")]
    public List<Player> players = new List<Player>();
    public List<Button> localPlayerCardButtons;
    public Button dealCardsButton;

    #endregion

    #region Card Deck

    private List<PlayingCard.PlayingCardInfo> _cardDeck;
    public class SyncListPlayingCard : SyncListStruct<PlayingCard.PlayingCardInfo> { }
    private readonly SyncListPlayingCard _syncListCardDeck = new SyncListPlayingCard();

    #endregion

    #region (Pre-) Round Management

    /// <summary>
    /// Relevant for both PreRound and Round.
    /// </summary>
    private Player _currentTurnPlayer;
    
    [Header("Pre-Round")]
    public GameObject preRoundButtonPanel; 
    public Dropdown preRoundSauspielDropdown;
    public Button preRoundSoloButton;
    public Button preRoundWenzButton;
    public Button preRoundWeiterButton;

    private Player _roundStartingPlayer;
    
    [SerializeField] private Image[] playedCardSlots = new Image[4];

    private readonly SyncListPlayingCard _currentStich = new SyncListPlayingCard();

    // public class SyncListStich : SyncListStruct<PlayingCard.Stich> {}
    private readonly List<PlayingCard.Stich> _completedStiches = new List<PlayingCard.Stich>();

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
        _currentTurnPlayer = _roundStartingPlayer;
        
        // reset the Round Mode
        CurrentRoundMode = RoundMode.Ramsch;
        
        // reset the player choices
        CurrentPreRoundChoices.Clear();
        
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
        StartStich();
    }
    
    /// <summary>
    /// Used to enter the "Round Finished" state
    /// </summary>
    private void EnterStateRoundFinished()
    {
        CurrentGameState = GameState.RoundFinished;
        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"Round finished!");
        
        // TODO show scoreboard?
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
            _currentTurnPlayer = players.CycleNext(_currentTurnPlayer);
            _currentTurnPlayer.RpcDisplayPreRoundButtons(CurrentRoundMode);
        }
    }
    
    #endregion
    
    // Start is called before the first frame update
    private void Awake()
    {
        Singleton = this;
        _cardDeck = PlayingCard.InitializeCardDeck();
        _currentStich.Callback = OnStichCardsChanged;
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
            gameStateText = $"Runde läuft ({CurrentRoundMode})\n{_currentTurnPlayer.playerName} ist dran";
            _currentTurnPlayer = players.CycleNext(_currentTurnPlayer);
            _currentTurnPlayer.RpcStartTurn();
        }
        
        
    }

    [Server]
    private void OnStichCompleted()
    {
        PlayingCard.Stich currentStichStruct = new PlayingCard.Stich();
        currentStichStruct.AddAll(_currentStich.ToArray());

        // determine who won the stich
        PlayingCard.PlayingCardInfo winningCard = currentStichStruct.CalculateWinningCard();
        Player winningPlayer = players.Cycle(_currentTurnPlayer, _currentStich.IndexOf(winningCard));
        
        // let the players know
        gameStateText = $"{winningPlayer.playerName} gewinnt mit {winningCard}!";
        
        // add the stich to the completed stiches
        _completedStiches.Add(currentStichStruct);
        
        // the winning player starts with the next stich
        _currentTurnPlayer = winningPlayer;
        
        // finish the stich after a small delay, so that everyone can understand what happened
        StartCoroutine(StartNextStichWithDelay(3));
    }

    [Server]
    private IEnumerator StartNextStichWithDelay(int seconds)
    {
        // wait for the specified amount of time
        yield return new WaitForSeconds(seconds);

        Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                  $"Finished Stich {_completedStiches.Count}");

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
        // clear the stiches table and the current stich
        _currentStich.Clear();

        // notify the current player that it's their turn
        _currentTurnPlayer.RpcStartTurn();
        gameStateText = $"Runde läuft ({CurrentRoundMode})\n{_currentTurnPlayer.playerName} ist dran";
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
                      $"Player {player.netId} should get cards {handedCards} to {handedCards + 7}");
            // this updates a SyncList on the player server object, which then notifies his respective client object
            for (int i = handedCards; i < handedCards + 8; i++)
            {
                player.handCards.Add(_syncListCardDeck[i]);
            }

            handedCards += 8;
        }
    }

    #endregion

    #region SyncVar Callbacks/Hooks

    private void OnStichCardsChanged(SyncList<PlayingCard.PlayingCardInfo>.Operation op, int i)
    {
        switch (op)
        {
            case SyncList<PlayingCard.PlayingCardInfo>.Operation.OP_ADD:
                Debug.Log($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}: " +
                          $"the server notified me that {_currentStich[i]} was played");

                // put the correct image in the position of the just played card
                playedCardSlots[i].gameObject.SetActive(true);
                playedCardSlots[i].sprite = PlayingCard.SpriteDict[_currentStich[i]];
                break;
            case SyncList<PlayingCard.PlayingCardInfo>.Operation.OP_CLEAR:
                foreach (var cardSlot in playedCardSlots)
                {
                    cardSlot.gameObject.SetActive(false);
                    cardSlot.sprite = PlayingCard.DefaultCardSprite;
                }
                break;
            default:
                throw new InvalidOperationException($"{nameof(op)}={op}");
        }
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