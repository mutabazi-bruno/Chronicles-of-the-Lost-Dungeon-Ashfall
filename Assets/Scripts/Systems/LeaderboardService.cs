using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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

    // Firebase REST API backend for cross-platform leaderboard support.
    //
    // singleton, same shape as our other managers (GameManager/SaveManager/AudioManager).
    public class LeaderboardService : MonoBehaviour
    {
        public static LeaderboardService Instance { get; private set; }

        [Header("Firebase Realtime Database")]
        [Tooltip("Your database URL, e.g. https://yourproject-default-rtdb.firebaseio.com/ " +
                 "(keep the trailing slash)")]
        public string databaseUrl = "https://YOUR-PROJECT-default-rtdb.firebaseio.com/";

        [Tooltip("Node the scores live under")]
        public string scoresNode = "scores";

        [Header("Player identity")]
        public string playerName = "Player";

        [Tooltip("Automatically submit coin total to the leaderboard whenever a level is completed")]
        public bool autoSubmitOnLevelComplete = true;

        [Tooltip("Seconds before a request is considered dead")]
        public int requestTimeout = 8;

        // The leaderboard screen listens to these; it never calls the network itself.
        public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;
        public event Action<bool> OnScoreSubmitted; // true = success, false = failed/offline

       
[ContextMenu("Submit Test Score")]
void SubmitTestScoreFromInspector()
{
    if (!Application.isPlaying)
    {
        Debug.LogWarning("[LeaderboardService] enter Play Mode first");
        return;
    }

    SubmitScore("TestPlayer", 100);
}

[ContextMenu("Fetch Leaderboard")]
void FetchFromInspector()
{
    if (!Application.isPlaying)
    {
        Debug.LogWarning("[LeaderboardService] enter Play Mode first");
        return;
    }

    FetchLeaderboard();
}

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

            // prefer the name the player actually typed on the name-entry screen;
            // fall back to the inspector default if they never got one (e.g. no
            // name-entry screen wired up in this scene)
            string name = PlayerProfile.Instance != null && PlayerProfile.Instance.HasPlayerName()
                ? PlayerProfile.Instance.PlayerName
                : playerName;

            SubmitScore(name, score);
        }

        public void SubmitScore(string name, int score)
        {
            StartCoroutine(SubmitScoreRoutine(name, score));
        }

        public void FetchLeaderboard()
        {
            StartCoroutine(FetchLeaderboardRoutine());
        }

        // Sanitize names for Firebase keys and store one entry per player.
        string SanitiseKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Player";

            var sb = new StringBuilder();
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                    sb.Append(c);
            }

            string result = sb.ToString();
            return string.IsNullOrEmpty(result) ? "Player" : result;
        }

        string BuildUrl(string suffix)
        {
            string root = databaseUrl.EndsWith("/") ? databaseUrl : databaseUrl + "/";
            return $"{root}{scoresNode}{suffix}";
        }

        // Handle network errors gracefully.
        bool TrySend(UnityWebRequest request, out UnityWebRequestAsyncOperation operation)
        {
            operation = null;
            try
            {
                operation = request.SendWebRequest();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardService] request could not be sent: {e.Message}");
                return false;
            }
        }

        IEnumerator SubmitScoreRoutine(string name, int score)
        {
            string key = SanitiseKey(name);
            string url = BuildUrl($"/{key}.json");

            var entry = new LeaderboardEntry { name = name, score = score };
            string body = JsonUtility.ToJson(entry);

            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = requestTimeout;

                if (!TrySend(request, out var operation))
                {
                    OnScoreSubmitted?.Invoke(false);
                    yield break;
                }

                yield return operation;

                bool success = request.result == UnityWebRequest.Result.Success;

                if (!success)
                {
                    // Ignore API errors to prevent gameplay interruption.
                    Debug.LogWarning($"[LeaderboardService] score submit failed: {request.error}");
                }
                else
                {
                    Debug.Log($"[LeaderboardService] submitted {name} = {score}");
                }

                OnScoreSubmitted?.Invoke(success);
            }
        }

        IEnumerator FetchLeaderboardRoutine()
        {
            string url = BuildUrl(".json");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = requestTimeout;

                if (!TrySend(request, out var operation))
                {
                    // fire with an empty list rather than not firing at all, so the UI
                    // can show "leaderboard unavailable" instead of hanging forever
                    OnLeaderboardLoaded?.Invoke(new List<LeaderboardEntry>());
                    yield break;
                }

                yield return operation;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[LeaderboardService] fetch failed: {request.error}");
                    OnLeaderboardLoaded?.Invoke(new List<LeaderboardEntry>());
                    yield break;
                }

                List<LeaderboardEntry> entries = ParseFirebaseJson(request.downloadHandler.text);
                OnLeaderboardLoaded?.Invoke(entries);
            }
        }

        // Parse JSON dictionary by extracting individual object blocks.
        List<LeaderboardEntry> ParseFirebaseJson(string json)
        {
            var results = new List<LeaderboardEntry>();

            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
                return results;

            try
            {
                int depth = 0;
                int start = -1;

                for (int i = 0; i < json.Length; i++)
                {
                    char c = json[i];

                    if (c == '{')
                    {
                        depth++;
                        // depth 1 is the outer wrapper, depth 2 is an actual entry
                        if (depth == 2) start = i;
                    }
                    else if (c == '}')
                    {
                        if (depth == 2 && start >= 0)
                        {
                            string block = json.Substring(start, i - start + 1);
                            var entry = JsonUtility.FromJson<LeaderboardEntry>(block);
                            if (entry != null && !string.IsNullOrEmpty(entry.name))
                                results.Add(entry);
                            start = -1;
                        }
                        depth--;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardService] couldn't parse leaderboard response: {e.Message}");
            }

            // sorting algorithm - highest score first, same comparison-delegate approach
            // used by InventoryLogic.SortByValue
            results.Sort((a, b) => b.score.CompareTo(a.score));
            return results;
        }
    }
}