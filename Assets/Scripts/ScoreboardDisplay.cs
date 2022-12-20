using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardDisplay : MonoBehaviour
{
    public Transform scoreboardContent;
    public List<Text> playerNameTextFields;
    public ScoreboardRowDisplay rowTemplate;

    public void AddScoreBoardRow(string[] playerNames, Extensions.ScoreBoardRow rowToAdd)
    {
        // display the player names
        for (var i = 0; i < playerNames.Length; i++) playerNameTextFields[i].text = playerNames[i];

        AddScoresAtCorrectName(playerNames, rowToAdd);
    }

    /// <summary>
    ///     Fills the Score Board UI with the entries, ensuring that each entry is displayed under the correct player
    ///     (The order of the ScoreBoardRow is independent from the playerNames in the UI)
    /// </summary>
    private void AddScoresAtCorrectName(IEnumerable<string> playerNames, Extensions.ScoreBoardRow rowToAdd)
    {
        // create a new List of scores which will have the correct order
        var scoresInCorrectOrder = new List<int>();
        foreach (var playerName in playerNames) // the LINQ expression suggested by Rider is too wild for me
        {
            var playerEntry = rowToAdd.entries.ToList()
                .Find(entry => entry.name.Equals(playerName));
            scoresInCorrectOrder.Add(playerEntry.score);
        }

        var rowDisplay = Instantiate(rowTemplate, scoreboardContent);

        rowDisplay.FillScoreTexts(scoresInCorrectOrder);
    }

    public void ClearRows()
    {
        foreach (Transform row in scoreboardContent.transform) Destroy(row.gameObject);
    }
}