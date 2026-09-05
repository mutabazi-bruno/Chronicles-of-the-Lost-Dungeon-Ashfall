using System;
using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Enemies.Behaviours;
using Ashfall.Systems;

namespace Ashfall.Enemies
{
    public enum EnemyType
    {
        Warrior,
        Archer,
        Guardian,
        Bomber
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        public int maxHealth = 50;
        public int currentHealth;
        public float moveSpeed = 2f;
        public float detectionRange = 5f;
        public float attackRange = 1f;
        public int attackDamage = 10;

        [Header("Behaviour")]
        [Tooltip("Which strategy this enemy uses. Adding a new type means adding one enum " +
                 "value and one line in CreateBehaviour - nothing else in the codebase changes.")]
        public EnemyType enemyType = EnemyType.Warrior;

        [Tooltip("Leave empty - the player is found automatically by tag on Awake. Only set " +
                 "this by hand if you need an enemy to target something other than the player.")]
        public Transform player;

        [Header("Ranged (only used by ranged enemies)")]
        public Transform firePoint;
        public GameObject projectilePrefab;

        [Header("Death")]
        public float destroyDelayAfterDeath = 1.5f; // gives death anim time to play
        public GameObject dropOnDeath; // optional, e.g. a key pickup prefab

        Rigidbody2D rb;
        Animator animator;
        Collider2D col;
        IEnemyBehaviour behaviour;
        bool isDead;

        // observer pattern - GameManager/loot can listen via this instance event
        public event Action<Enemy> OnEnemyDefeated;
        // static so AudioManager can listen for any enemy dying without a direct reference
        public static event Action OnAnyEnemyDeath;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            col = GetComponent<Collider2D>();
            currentHealth = maxHealth;

            SetBehaviour(CreateBehaviour(enemyType));
        }

        void OnEnable()
        {
            ObjectiveManager.Instance?.RegisterEnemy(this);
        }

        void OnDisable()
        {
            ObjectiveManager.Instance?.UnregisterEnemy(this);
        }

        void Start()
        {
            // Resolve player reference at runtime to avoid prefab linking issues.
            if (player == null)
                FindPlayer();
        }

        void FindPlayer()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning($"[Enemy] {name} couldn't find an object tagged 'Player'");
        }

        // Factory pattern for assigning enemy behaviours.
        static IEnemyBehaviour CreateBehaviour(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Archer: return new ArcherBehaviour();
                case EnemyType.Guardian: return new GuardianBehaviour();
                case EnemyType.Bomber: return new BomberBehaviour();
                default: return new WarriorBehaviour();
            }
        }

        void Update()
        {
            if (isDead) return;

            // player can be destroyed/reloaded mid-level, so re-acquire rather than going inert
            if (player == null)
            {
                FindPlayer();
                if (player == null) return;
            }

            behaviour?.Tick(gameObject, player);
        }

        // called by whatever spawns this enemy if it wants to override the default strategy
        public void SetBehaviour(IEnemyBehaviour newBehaviour)
        {
            behaviour = newBehaviour;
        }

        public Rigidbody2D Rigidbody => rb;
        public Animator Animator => animator; // behaviours can grab this to trigger anims

        public bool IsDead => isDead;

        public void TakeDamage(int amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;

            if (currentHealth <= 0)
                Die();
            else
                animator?.SetTrigger("Hurt");
        }

        void Die()
        {
            isDead = true;
            rb.linearVelocity = Vector2.zero;
            if (col != null) col.enabled = false; // cant be hit or block player anymore

            animator?.SetTrigger("Death");
            OnAnyEnemyDeath?.Invoke();

            if (dropOnDeath != null)
                Instantiate(dropOnDeath, transform.position, Quaternion.identity);

            OnEnemyDefeated?.Invoke(this);
            Destroy(gameObject, destroyDelayAfterDeath);
        }
    }
}
