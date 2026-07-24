using UnityEngine;
using Ashfall.Interfaces;
using Ashfall.Enemies;

namespace Ashfall.Enemies.Behaviours
{
    // defensive guy, doesnt move, telegraphs before hitting hard
    // this is our State pattern - idle/windup/attack/cooldown
    public class GuardianBehaviour : IEnemyBehaviour
    {
        enum State { Idle, Windup, Attack, Cooldown }

        State currentState = State.Idle;
        float stateTimer;

        const float windupDuration = 0.8f;
        const float cooldownDuration = 1.5f;

        public void Tick(GameObject enemyObj, Transform player)
        {
            if (player == null) return;

            var enemy = enemyObj.GetComponent<Enemy>();
            float distance = Vector2.Distance(enemyObj.transform.position, player.position);

            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case State.Idle:
                    if (distance <= enemy.attackRange)
                        ChangeState(State.Windup);
                    break;

                case State.Windup:
                    // telegraphing here, could flash a sprite or play an anim later
                    if (stateTimer <= 0f)
                        ChangeState(State.Attack);
                    break;

                case State.Attack:
                    // only actually hits if player stuck around in range
                    if (distance <= enemy.attackRange)
                    {
                        var damageable = player.GetComponent<IDamageable>();
                        damageable?.TakeDamage(enemy.attackDamage);
                    }
                    ChangeState(State.Cooldown);
                    break;

                case State.Cooldown:
                    if (stateTimer <= 0f)
                        ChangeState(State.Idle);
                    break;
            }
        }

        void ChangeState(State newState)
        {
            currentState = newState;

            switch (newState)
            {
                case State.Windup:
                    stateTimer = windupDuration;
                    break;
                case State.Cooldown:
                    stateTimer = cooldownDuration;
                    break;
            }
        }
    }
}