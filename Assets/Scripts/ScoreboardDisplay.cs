using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardDisplay : MonoBehaviour
{
    public Transform scoreboardContent;
    public List<Text> playerNameTextFields;
    public ScoreboardRowDisplay rowTemplate;
    private readonly List<string> orderedPlayerNames = new(); // to keep track of the player names in order
    
    public void AddScoreBoardRow(Extensions.ScoreBoardRow rowToAdd)
    {
        if (orderedPlayerNames.Count == 0)
        {
            // first call: remember the order in which we see the player names...
            orderedPlayerNames.AddRange(rowToAdd.Entries.Select(e => e.name.Value));
            
            // ...and set the headings accordingly
            for (var i = 0; i < orderedPlayerNames.Count; i++)
            {
                playerNameTextFields[i].text = orderedPlayerNames[i];
            }
        }

        // every call: add the row of scores
        AddScoresAtCorrectName(rowToAdd);
    }

    /// <summary>
    ///     Fills the Score Board UI with the entries, ensuring that each entry is displayed under the correct player
    ///     (The order of the ScoreBoardRow is independent from the playerNames in the UI)
    /// </summary>
    private void AddScoresAtCorrectName(Extensions.ScoreBoardRow rowToAdd)
    {
        // create a new List of scores which will have the correct order
        var scoresInCorrectOrder = new List<int>();
        foreach (var playerName in orderedPlayerNames) // the LINQ expression suggested by Rider is too wild for me
        {
            var playerScore = rowToAdd.Entries.ToDictionary(entry => entry.name)
                [playerName].score;
            scoresInCorrectOrder.Add(playerScore);
        }

        var rowDisplay = Instantiate(rowTemplate, scoreboardContent);

        rowDisplay.FillScoreTexts(scoresInCorrectOrder);
    }

    public void ClearRows()
    {
        foreach (Transform row in scoreboardContent.transform) Destroy(row.gameObject);
    }
}