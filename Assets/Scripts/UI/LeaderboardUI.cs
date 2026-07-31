using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Ashfall.Systems;

namespace Ashfall.UI
{
    // The screen half of the REST API feature. It never calls the network itself -
    // it just subscribes to LeaderboardService's events, so the UI and the API layer
    // stay completely decoupled (swap dreamlo for any other backend and this file
    // doesn't change by a single line).
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

        bool subscribed;

        void OnDestroy()
        {
            Unsubscribe();
        }

        // subscribing lazily instead of in OnEnable, because OnEnable can run before
        // LeaderboardService.Awake() has set Instance - which silently skipped the
        // subscription and left the UI listening to nothing forever.
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
            Debug.Log("[LeaderboardUI] ShowLeaderboard() called");

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

        // TEMPORARY helper - hook to any button to push a known score onto the board so
        // you can prove the round trip works. Remove before submitting the project.
        public void SubmitTestScore()
        {
            if (LeaderboardService.Instance == null)
            {
                Debug.LogWarning("[LeaderboardUI] no LeaderboardService to submit through");
                return;
            }

            Subscribe();
            Debug.Log("[LeaderboardUI] submitting test score");
            LeaderboardService.Instance.SubmitScore("TestPlayer", 100);
        }

        void HandleLeaderboardLoaded(List<LeaderboardEntry> entries)
        {
            int count = entries == null ? 0 : entries.Count;
            Debug.Log($"[LeaderboardUI] leaderboard loaded with {count} entries");

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
                sb.AppendLine($"{i + 1}.  {entries[i].name}  -  {entries[i].score}");
            }

            if (entriesText != null)
                entriesText.text = sb.ToString();
        }

        void HandleScoreSubmitted(bool success)
        {
            Debug.Log($"[LeaderboardUI] score submitted, success = {success}");
            SetStatus(success ? "Score submitted." : "Couldn't reach the leaderboard - progress saved locally.");
        }

        void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}