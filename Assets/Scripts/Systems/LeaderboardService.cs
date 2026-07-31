using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Ashfall.Systems
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;
    }

    // dreamlo wraps everything in this shape:
    // { "dreamlo": { "leaderboard": { "entry": [ {name, score, seconds, text, date} ] } } }
    // when there's only one entry, dreamlo sometimes sends "entry" as a single object
    // instead of an array - HandleRawJson below works around that.
    [Serializable]
    class DreamloRoot { public DreamloWrapper dreamlo; }
    [Serializable]
    class DreamloWrapper { public DreamloBoard leaderboard; }
    [Serializable]
    class DreamloBoard { public string entry; } // kept as raw text, re-parsed manually

    // REST API integration - online leaderboard.
    // Uses dreamlo.com (free, no server of our own required, plain HTTP GET calls).
    // singleton, same shape as our other managers (GameManager/SaveManager/AudioManager).
    public class LeaderboardService : MonoBehaviour
    {
        public static LeaderboardService Instance { get; private set; }

        [Header("Dreamlo codes - get these free at dreamlo.com")]
        [Tooltip("Private code - keep secret, only used to submit scores")]
        public string privateCode = "YOUR_PRIVATE_CODE";
        [Tooltip("Public code - safe to share, used to read scores")]
        public string publicCode = "YOUR_PUBLIC_CODE";

        [Header("Player identity")]
        public string playerName = "Player";

        [Tooltip("Automatically submit coin total to the leaderboard whenever a level is completed")]
        public bool autoSubmitOnLevelComplete = true;

        // observer pattern - UI (a leaderboard screen) reacts to these instead of polling
        public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;
        public event Action<bool> OnScoreSubmitted; // true = success, false = failed/offline

        // NOTE: dreamlo's free tier is HTTP only (HTTPS needs a small paid upgrade on their
        // end). That's fine for PC/Editor/Android, but a WebGL build hosted on an HTTPS page
        // (e.g. Unity Play) may block the request as mixed content. If that happens for your
        // WebGL build, either upgrade the dreamlo board or swap baseUrl below for any other
        // HTTPS JSON host - nothing else in this file needs to change.
        const string baseUrl = "http://dreamlo.com/lb/";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // wired up here (not inside LevelManager) so LevelManager doesn't need to know
            // the leaderboard exists at all - keeps the two systems decoupled.
            if (autoSubmitOnLevelComplete && LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelCompleted += HandleLevelCompleted;
            }
        }

        void OnDestroy()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
        }

        void HandleLevelCompleted(string levelId)
        {
            int score = SaveManager.Instance != null && SaveManager.Instance.CurrentSave != null
                ? SaveManager.Instance.CurrentSave.coins
                : 0;

            SubmitScore(playerName, score);
        }

        public void SubmitScore(string name, int score)
        {
            StartCoroutine(SubmitScoreRoutine(name, score));
        }

        public void FetchLeaderboard()
        {
            StartCoroutine(FetchLeaderboardRoutine());
        }

        IEnumerator SubmitScoreRoutine(string name, int score)
        {
            string safeName = UnityWebRequest.EscapeURL(name);
            string url = $"{baseUrl}{privateCode}/add/{safeName}/{score}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 8; // don't let a dead connection hang the game
                yield return request.SendWebRequest();

                bool success = request.result == UnityWebRequest.Result.Success;

                if (!success)
                {
                    // graceful failure - API being down should never crash or block gameplay,
                    // the level completion / save flow already happened locally either way.
                    Debug.LogWarning($"[LeaderboardService] score submit failed: {request.error}");
                }

                OnScoreSubmitted?.Invoke(success);
            }
        }

        IEnumerator FetchLeaderboardRoutine()
        {
            string url = $"{baseUrl}{publicCode}/json";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 8;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[LeaderboardService] fetch failed: {request.error}");
                    // fire with an empty list rather than not firing at all, so the UI
                    // can show "leaderboard unavailable" instead of hanging forever
                    OnLeaderboardLoaded?.Invoke(new List<LeaderboardEntry>());
                    yield break;
                }

                List<LeaderboardEntry> entries = ParseDreamloJson(request.downloadHandler.text);
                OnLeaderboardLoaded?.Invoke(entries);
            }
        }

        // small hand-rolled parse: dreamlo's json isn't a clean fit for JsonUtility because
        // "entry" is an array when there are 2+ scores but a single object when there's 1,
        // and an empty string when there are none.
        List<LeaderboardEntry> ParseDreamloJson(string json)
        {
            var results = new List<LeaderboardEntry>();

            try
            {
                int entryIndex = json.IndexOf("\"entry\"");
                if (entryIndex < 0) return results; // no scores yet, empty board

                bool isArray = json.IndexOf('[', entryIndex) is int bracketPos
                    && bracketPos >= 0
                    && bracketPos < json.IndexOf('{', entryIndex);

                string wrapped = isArray
                    ? "{\"list\":" + json.Substring(json.IndexOf('[', entryIndex)) : null;

                if (isArray)
                {
                    // trim to just the array + close it off cleanly
                    int end = wrapped.LastIndexOf(']');
                    wrapped = wrapped.Substring(0, end + 1) + "}";
                    var listWrapper = JsonUtility.FromJson<EntryListWrapper>(wrapped);
                    if (listWrapper?.list != null) results.AddRange(listWrapper.list);
                }
                else
                {
                    int start = json.IndexOf('{', entryIndex);
                    int end = json.IndexOf('}', start);
                    string single = json.Substring(start, end - start + 1);
                    var entry = JsonUtility.FromJson<LeaderboardEntry>(single);
                    if (entry != null) results.Add(entry);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardService] couldn't parse leaderboard response: {e.Message}");
            }

            results.Sort((a, b) => b.score.CompareTo(a.score));
            return results;
        }

        [Serializable]
        class EntryListWrapper { public List<LeaderboardEntry> list; }
    }
}