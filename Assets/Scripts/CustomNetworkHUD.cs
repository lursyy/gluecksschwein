using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;
using Random = UnityEngine.Random;

#pragma warning disable 618

[RequireComponent(typeof(NetworkManager))]
// ReSharper disable once InconsistentNaming
public class CustomNetworkHUD : MonoBehaviour
{
    private NetworkManager _manager;

    [SerializeField] private GameObject lobbyPanel;

    [SerializeField] private Button hostGameButton;
    [SerializeField] private TMP_InputField hostGameInputField; // for the match name

    [SerializeField] private GameObject joinGamePanel;
    [SerializeField] private Transform joinGameButtonList;
    [SerializeField] private Button joinGameButtonPrefab;

    [SerializeField] private float refreshIntervalSeconds = 1f;
    private float _nextRefreshTime;
    private bool _uiActive = true;

    private void Awake()
    {
        lobbyPanel.SetActive(true);
        
        _manager = GetComponent<NetworkManager>();
        _manager.StartMatchMaker();
        
        // randomize the default match name
        hostGameInputField.text = $"Spiel {Random.Range(1, 100)}";
        
        // empty match name is not allowed
        hostGameInputField.onValueChanged.AddListener(val => hostGameButton.interactable = val != "");
    }

    /// <summary>
    /// Host a new game with a given name
    /// </summary>
    public void CreateMatch()
    {
        string matchName = hostGameInputField.text;
        _manager.matchMaker.CreateMatch(matchName, _manager.matchSize, true, "", "", "", 0, 0,
            OnMatchCreate);
    }

    private void OnMatchCreate(bool success, string extendedInfo, MatchInfo responseData)
    {
        DisableUi();

        // forward the callback to the network manager
        _manager.OnMatchCreate(success, extendedInfo, responseData);
    }

    private void DisableUi()
    {
        _uiActive = false;
        lobbyPanel.SetActive(false);
    }

    private void JoinMatch(MatchInfoSnapshot matchInfo)
    {
        if (_manager.matchMaker == null)
        {
            _manager.StartMatchMaker();
        }

        _manager.matchMaker.JoinMatch(matchInfo.networkId, "", "", "", 0, 0, OnMatchJoined);
    }

    private void OnMatchJoined(bool success, string extendedInfo, MatchInfo responseData)
    { 
        DisableUi();
        // forward the callback to the network manager
        _manager.OnMatchJoined(success, extendedInfo, responseData);
    }

    private void OnGUI()
    {
        if (Time.time >= _nextRefreshTime)
        {
            RefreshMatches();
        }
    }

    private void RefreshMatches()
    {
        _nextRefreshTime += refreshIntervalSeconds;
        
        if (_manager.matchMaker == null)
        {
            _manager.StartMatchMaker();
        }

        if (!_uiActive) return;

        Debug.Log("Refreshing Match List...");
        _manager.matchMaker.ListMatches(0, 10, "", true, 0, 0, OnMatchList);
    }

    private void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        _manager.OnMatchList(success, extendedInfo, matchList);
        UpdateJoinMatchButtons();
    }

    private void UpdateJoinMatchButtons()
    {
        bool hasMatches = _manager.matches != null && _manager.matches.Count > 0;
        joinGamePanel.SetActive(hasMatches);

        // clear the old buttons
        foreach (Transform button in joinGameButtonList)
        {
            Destroy(button.gameObject);
        }

        if (!hasMatches) return;
        foreach (var matchInfo in _manager.matches)
        {
            var joinMatchButton = Instantiate(joinGameButtonPrefab, joinGameButtonList);
            joinMatchButton.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{matchInfo.name} ({matchInfo.currentSize}P)";
            joinMatchButton.onClick.AddListener(() => JoinMatch(matchInfo));
        }
    }
}