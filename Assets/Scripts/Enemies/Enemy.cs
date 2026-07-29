using System;
using UnityEngine;
using Ashfall.Interfaces;

namespace Ashfall.Enemies
{
    public enum EnemyTestType
    {
        Warrior,
        Archer,
        Guardian
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
        public Transform player; // just drag the player in for now, we'll auto find it later

        [Header("Ranged (only used by ranged enemies)")]
        public Transform firePoint;
        public GameObject projectilePrefab;

        [Header("Death")]
        public float destroyDelayAfterDeath = 1.5f; // gives death anim time to play
        public GameObject dropOnDeath; // optional, e.g. a key pickup prefab

        [Header("Temp Testing (spawner will replace this later)")]
        public EnemyTestType testType = EnemyTestType.Warrior;

        Rigidbody2D rb;
        Animator animator;
        Collider2D col;
        IEnemyBehaviour behaviour;
        bool isDead;

        // observer pattern - GameManager/audio/loot can all listen without a direct reference
        public event Action<Enemy> OnEnemyDefeated;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            col = GetComponent<Collider2D>();
            currentHealth = maxHealth;

            // temp default so we can test behaviours before spawner exists
            switch (testType)
            {
                case EnemyTestType.Warrior:
                    SetBehaviour(new Ashfall.Enemies.Behaviours.WarriorBehaviour());
                    break;
                case EnemyTestType.Archer:
                    SetBehaviour(new Ashfall.Enemies.Behaviours.ArcherBehaviour());
                    break;
                case EnemyTestType.Guardian:
                    SetBehaviour(new Ashfall.Enemies.Behaviours.GuardianBehaviour());
                    break;
            }
        }

        void Update()
        {
            if (isDead) return;
            behaviour?.Tick(gameObject, player);
        }

        // called once by whatever spawns this enemy, sets which strategy it uses
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

            Debug.Log($"{name} took {amount} dmg, hp now {currentHealth}/{maxHealth}");

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

            Debug.Log($"{name} died, dropOnDeath is: {(dropOnDeath != null ? dropOnDeath.name : "NULL")}");

            if (dropOnDeath != null)
            {
                GameObject key = Instantiate(dropOnDeath, transform.position, Quaternion.identity);
                Debug.Log($"key spawned at {key.transform.position}, scale {key.transform.localScale}, active: {key.activeSelf}");
            }

            OnEnemyDefeated?.Invoke(this);
            Destroy(gameObject, destroyDelayAfterDeath);
        }
    }
}