using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#pragma warning disable 618

// ReSharper disable once InconsistentNaming
public class RPSManager : NetworkBehaviour
{
    private int _playedHands;
    private string _playerName;

    enum Rps
    {
        Schere,
        Stein,
        Papier
    }

    [SerializeField] private Button buttonSchere;
    [SerializeField] private Button buttonStein;
    [SerializeField] private Button buttonPapier;
    [SerializeField] private Button buttonReset;
    [SerializeField] private Text playedHandsText;

    // Start is called before the first frame update
    void Start()
    {
        buttonSchere.onClick.AddListener(() => CmdPlayHand(Rps.Schere));
        buttonStein.onClick.AddListener(() => CmdPlayHand(Rps.Stein));
        buttonPapier.onClick.AddListener(() => CmdPlayHand(Rps.Papier));

        buttonReset.onClick.AddListener(CmdReset);
    }

    [Command]
    private void CmdReset()
    {
        playedHandsText.text = "";
    }

    [Command]
    private void CmdPlayHand(Rps playedHand)
    {
        playedHandsText.text += playedHand;
    }
}