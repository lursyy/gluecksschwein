using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private PlayingCard.Suit testSuit;
    [SerializeField] private PlayingCard.Rank testRank;
    
    
    // Start is called before the first frame update
    void Start()
    {
        button.image.sprite = PlayingCard.GetSprite(testSuit, testRank);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
