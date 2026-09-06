using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Ashfall.Systems;

namespace Ashfall.UI
{
    // UI for displaying the leaderboard, decoupled from backend logic.
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("The leaderboard panel. IMPORTANT: put this script on an object that is " +
                 "ALWAYS ACTIVE (e.g. the Canvas), not on the panel itself - Unity silently " +
                 "skips button calls aimed at an inactive GameObject.")]
        public GameObject panel;

        [Header("Text")]
        [Tooltip("one TMP text object, we write all rows into it")]
        public TMP_Text entriesText;
        public TMP_Text statusText;

        [Header("Options")]
        public int maxRowsToShow = 10;

        // Column stops, as a percentage of the text field's width. Using <pos=>
        // rather than padded spaces is what makes the columns actually line up -
        // the UI font is proportional, so spaces are not a reliable ruler.
        const string RankStop = "2%";
        const string NameStop = "18%";
        const string ScoreStop = "76%";

        static readonly string[] MedalColours = { "#F2D27C", "#D6D8DC", "#C98A54" };
        const string RowColour = "#E8E0CE";

        bool subscribed;

        void OnDestroy()
        {
            Unsubscribe();
        }

        // Subscribe lazily to avoid race conditions with manager initialization.
        void Subscribe()
        {
            if (subscribed || LeaderboardService.Instance == null) return;

            LeaderboardService.Instance.OnLeaderboardLoaded += HandleLeaderboardLoaded;
            LeaderboardService.Instance.OnScoreSubmitted += HandleScoreSubmitted;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || LeaderboardService.Instance == null) return;

            LeaderboardService.Instance.OnLeaderboardLoaded -= HandleLeaderboardLoaded;
            LeaderboardService.Instance.OnScoreSubmitted -= HandleScoreSubmitted;
            subscribed = false;
        }

        // hook this to a "Leaderboard" button in the main menu
        public void ShowLeaderboard()
        {

            if (panel != null) panel.SetActive(true);
            else Debug.LogWarning("[LeaderboardUI] no panel assigned in the inspector");

            if (entriesText == null)
                Debug.LogWarning("[LeaderboardUI] entriesText is NOT assigned - rows have nowhere to display");
            if (statusText == null)
                Debug.LogWarning("[LeaderboardUI] statusText is NOT assigned - status messages have nowhere to display");

            if (LeaderboardService.Instance == null)
            {
                Debug.LogWarning("[LeaderboardUI] LeaderboardService.Instance is null - is the " +
                                 "component actually on the Systems prefab?");
                SetStatus("Leaderboard service not available.");
                return;
            }

            Subscribe();

            SetStatus("Loading...");
            if (entriesText != null) entriesText.text = string.Empty;

            LeaderboardService.Instance.FetchLeaderboard();
        }

        public void HideLeaderboard()
        {
            if (panel != null) panel.SetActive(false);
        }


        void HandleLeaderboardLoaded(List<LeaderboardEntry> entries)
        {
            int count = entries == null ? 0 : entries.Count;
           

            // an empty list is also what the service sends when the request failed,
            // so the player sees a message instead of a screen that hangs forever
            if (count == 0)
            {
                SetStatus("No scores yet, or the leaderboard is offline.");
                if (entriesText != null) entriesText.text = string.Empty;
                return;
            }

            SetStatus(string.Empty);

            var sb = new StringBuilder();
            int rows = Mathf.Min(count, maxRowsToShow);

            for (int i = 0; i < rows; i++)
            {
                string colour = i < MedalColours.Length ? MedalColours[i] : RowColour;

                sb.Append("<color=").Append(colour).Append('>')
                  .Append("<pos=").Append(RankStop).Append('>').Append(i + 1)
                  .Append("<pos=").Append(NameStop).Append('>').Append(Sanitise(entries[i].name))
                  .Append("<pos=").Append(ScoreStop).Append('>').Append(entries[i].score)
                  .Append("</color>");

                if (i < rows - 1)
                {
                    sb.AppendLine();
                }
            }

            if (entriesText != null)
                entriesText.text = sb.ToString();
        }

        void HandleScoreSubmitted(bool success)
        {
            SetStatus(success ? "Score submitted." : "Couldn't reach the leaderboard - progress saved locally.");
        }

        // Names come from player input, so they must not be able to inject TMP
        // rich-text tags into the rows we build here.
        static string Sanitise(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "-";
            }

            return name.Replace('<', ' ').Replace('>', ' ').Trim();
        }

        void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}