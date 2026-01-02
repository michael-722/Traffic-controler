using System.Text;
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
        if (scoreText == null || PlayerScoreManager.Instance == null)
            return;

        if (showAllPlayers)
        {
            StringBuilder sb = new StringBuilder();
            var scores = PlayerScoreManager.Instance.GetAllPlayerScores();
            var usernames = PlayerScoreManager.Instance.GetAllPlayerUsernames();

            if (scores.Count == 0) return;

            foreach (var player in scores)
            {
                ulong playerId = player.Key;
                int score = player.Value;
                string playerName = usernames.ContainsKey(playerId) ? usernames[playerId] : $"Player {playerId}";

                sb.AppendLine($"{playerName}: {score}");
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