using System.Text;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private bool showAllPlayers = true;
    [SerializeField] private float updateInterval = 0.5f;

    private float lastUpdateTime;

    private void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateScoreDisplay();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText == null)
            return;
        
        if (PlayerScoreManager.Instance == null)
            return;

        if (showAllPlayers)
        {
            //store player scores
            StringBuilder sb = new StringBuilder();
            var allScores = PlayerScoreManager.Instance.GetAllPlayerScores();

            if (allScores.Count == 0)
                return;
            else
            {
                foreach (var player in allScores)
                {
                    string playerName = $"Player {player.Key}";
                    sb.AppendLine($"{playerName}: {player.Value}");
                }
            }

            scoreText.text = sb.ToString();
        }
        else
        {
            int localScore = PlayerScoreManager.Instance.GetLocalPlayerScore();
            scoreText.text = $"Score: {localScore}";
        }
    }
}