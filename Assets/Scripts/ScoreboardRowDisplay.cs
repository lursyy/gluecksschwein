using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class ScoreboardRowDisplay : MonoBehaviour
{
    public List<Text> scoreTexts;

    public void FillScoreTexts(Extensions.ScoreBoardRow row)
    {
        Assert.AreEqual(scoreTexts.Count, row.EntryCount);

        for (var i = 0; i < scoreTexts.Count; i++)
        {
            scoreTexts[i].text = row.Entries[i].score.ToString();
        }
    }
}
