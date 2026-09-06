using System;
using System.Collections.Generic;
using UnityEngine;
using Ashfall.Core;
using Ashfall.Enemies;
using Ashfall.Player;

namespace Ashfall.Systems
{
    // Manages per-level objectives and broadcasts updates to UI.
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

        readonly HashSet<Enemy> registeredEnemies = new HashSet<Enemy>();

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

        // Called by Enemy in its OnEnable, so we never have to scan the whole
        // scene to find out how many enemies exist.
        public void RegisterEnemy(Enemy enemy)
        {
            registeredEnemies.Add(enemy);
        }

        public void UnregisterEnemy(Enemy enemy)
        {
            registeredEnemies.Remove(enemy);
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
            return registeredEnemies.Count;
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
