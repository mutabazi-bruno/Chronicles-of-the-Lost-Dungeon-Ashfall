using System;
using System.Collections.Generic;
using UnityEngine;
using Ashfall.Core;
using Ashfall.Enemies;
using Ashfall.Player;

namespace Ashfall.Systems
{
    // One per level scene (NOT on the Systems prefab - objectives are per-level).
    // Translates gameplay events into the pure objective objects in Ashfall.Core, then
    // announces changes so the HUD and the level exit can react without knowing each other.
    public class ObjectiveManager : MonoBehaviour
    {
        public static ObjectiveManager Instance { get; private set; }

        [Header("Defeat enemies")]
        public bool requireEnemiesDefeated = true;
        [Tooltip("Leave at 0 to auto-count every Enemy present in the scene at startup")]
        public int enemiesRequired = 0;

        [Header("Collect coins")]
        public bool requireCoins = false;
        public int coinsRequired = 10;

        // observer - HUD listens, LevelExit queries. Neither knows the other exists.
        public event Action OnObjectivesChanged;

        readonly List<IObjective> objectives = new List<IObjective>();
        DefeatEnemiesObjective defeatObjective;
        CollectCoinsObjective coinObjective;

        int coinsAtLevelStart;
        PlayerHealth playerHealth;

        public IReadOnlyList<IObjective> Objectives => objectives;

        // the exit is only usable once everything else is done
        public bool AllComplete
        {
            get
            {
                foreach (var objective in objectives)
                    if (!objective.IsComplete) return false;
                return true;
            }
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            BuildObjectives();

            Enemy.OnAnyEnemyDeath += HandleEnemyDefeated;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // coins carry across levels, so measure progress from the level's start
                    // rather than from zero
                    coinsAtLevelStart = playerHealth.stats.coins;
                    playerHealth.OnCoinsChanged += HandleCoinsChanged;
                }
            }

            OnObjectivesChanged?.Invoke();
        }

        void OnDestroy()
        {
            Enemy.OnAnyEnemyDeath -= HandleEnemyDefeated;

            if (playerHealth != null)
                playerHealth.OnCoinsChanged -= HandleCoinsChanged;

            if (Instance == this) Instance = null;
        }

        void BuildObjectives()
        {
            objectives.Clear();

            if (requireEnemiesDefeated)
            {
                int required = enemiesRequired > 0
                    ? enemiesRequired
                    : CountEnemiesInScene();

                defeatObjective = new DefeatEnemiesObjective(required);
                objectives.Add(defeatObjective);
            }

            if (requireCoins)
            {
                coinObjective = new CollectCoinsObjective(coinsRequired);
                objectives.Add(coinObjective);
            }
        }

        int CountEnemiesInScene()
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
#else
            return UnityEngine.Object.FindObjectsOfType<Enemy>().Length;
#endif
        }

        void HandleEnemyDefeated()
        {
            defeatObjective?.RegisterKill();
            OnObjectivesChanged?.Invoke();
        }

        void HandleCoinsChanged(int total)
        {
            coinObjective?.SetCollected(total - coinsAtLevelStart);
            OnObjectivesChanged?.Invoke();
        }
    }
}