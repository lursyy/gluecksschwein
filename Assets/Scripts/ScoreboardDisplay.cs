using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardDisplay : MonoBehaviour
{
    public Transform scoreboardContent;
    public List<Text> playerNameTextFields;
    public ScoreboardRowDisplay rowTemplate;
    
    private void Start()
    {
        // test
        List<string> playerTestNames = new List<string>{"Luis", "Nuria", "Lena", "Lukian"};
        for (var i = 0; i < playerTestNames.Count; i++)
        {
            playerNameTextFields[i].text = playerTestNames[i];
        }

        // insert test rows
        for (int i = 0; i < 20; i++)
        {
            Extensions.ScoreBoardRow testRow = new Extensions.ScoreBoardRow();
            
            // insert random score for each of the players
            for (int j = 0; j < 4; j++)
            {
                testRow.AddEntry(playerTestNames[j], Random.Range(0, 100));
            }

            ScoreboardRowDisplay rowDisplay = Instantiate(rowTemplate, scoreboardContent);
            float rowOffset = rowDisplay.GetComponent<RectTransform>().rect.height + 5;
            rowDisplay.GetComponent<RectTransform>().anchoredPosition = Vector2.down * rowOffset * i;
            
            rowDisplay.FillScoreTexts(testRow);
        }
    }
    
}