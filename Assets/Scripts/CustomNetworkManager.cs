using System;
using System.Collections.Generic;
using UnityEngine.Networking;
#pragma warning disable 618

public class CustomNetworkManager : NetworkManager
{ 
    [NonSerialized] private readonly List<Player> _players = new List<Player>();
    
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        base.OnServerAddPlayer(conn, playerControllerId);
        var newPlayer = conn.playerControllers[0].gameObject.GetComponent<Player>();
        _players.Add(newPlayer);
    }

    public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player)
    {
        _players.Remove(player.gameObject.GetComponent<Player>());
        base.OnServerRemovePlayer(conn, player);
    }
    
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        foreach (var p in conn.playerControllers)
        {
            if (p != null && p.gameObject != null)
            {
                _players.Remove(p.gameObject.GetComponent<Player>());
            }
        }
        base.OnServerDisconnect(conn);
    }
}