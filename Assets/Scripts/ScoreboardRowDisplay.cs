using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class ScoreboardRowDisplay : MonoBehaviour
{
    public List<Text> scoreTexts;

    public void FillScoreTexts(List<int> scores)
    {
        Assert.AreEqual(scoreTexts.Count, scores.Count);

        for (var i = 0; i < scoreTexts.Count; i++)
        {
            scoreTexts[i].text = scores[i].ToString();
        }
    }
}
