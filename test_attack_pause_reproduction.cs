using UnityEngine;
using Game.Gameplay.Player;
using Game.Gameplay.Enemies;

/// <summary>
/// Reproduction script to test the attack pause issue when enemies die.
/// This script simulates the scenario described in the issue where
/// player attack pauses when an enemy dies instead of continuing to attack other enemies.
/// </summary>
public class TestAttackPauseReproduction : MonoBehaviour
{
    [Header("Test Setup")]
    public PlayerAttack playerAttack;
    public Enemy[] testEnemies;
    
    [Header("Test Results")]
    public bool isTestRunning;
    public float timeSinceLastAttack;
    public int enemiesKilled;
    public bool attackPausedAfterKill;
    
    private float lastAttackTime;
    private int previousEnemyCount;
    
    void Start()
    {
        if (playerAttack == null)
        {
            Debug.LogError("[DEBUG_LOG] PlayerAttack reference is missing!");
            return;
        }
        
        if (testEnemies == null || testEnemies.Length == 0)
        {
            Debug.LogError("[DEBUG_LOG] Test enemies array is empty!");
            return;
        }
        
        Debug.Log("[DEBUG_LOG] Starting attack pause reproduction test...");
        isTestRunning = true;
        previousEnemyCount = testEnemies.Length;
        
        // Subscribe to enemy death events
        foreach (var enemy in testEnemies)
        {
            if (enemy != null)
            {
                enemy.OnDied += OnEnemyDied;
            }
        }
    }
    
    void Update()
    {
        if (!isTestRunning) return;
        
        timeSinceLastAttack += Time.deltaTime;
        
        // Check if player is attacking (this would need to be monitored differently in actual implementation)
        // For now, we'll simulate by checking if any projectiles were spawned recently
        CheckForRecentAttack();
        
        // Monitor if attack paused after enemy death
        MonitorAttackPause();
    }
    
    private void OnEnemyDied()
    {
        enemiesKilled++;
        Debug.Log($"[DEBUG_LOG] Enemy died! Total killed: {enemiesKilled}");
        
        // Start monitoring for attack pause
        Invoke(nameof(CheckAttackPauseAfterDeath), 0.1f);
    }
    
    private void CheckAttackPauseAfterDeath()
    {
        // Check if there are still valid targets
        bool hasValidTargets = false;
        foreach (var enemy in testEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                hasValidTargets = true;
                break;
            }
        }
        
        if (hasValidTargets && timeSinceLastAttack > 2f)
        {
            attackPausedAfterKill = true;
            Debug.LogError($"[DEBUG_LOG] BUG REPRODUCED: Attack paused for {timeSinceLastAttack:F1}s after enemy death, but valid targets still exist!");
        }
    }
    
    private void CheckForRecentAttack()
    {
        // This is a simplified check - in real scenario we'd monitor projectile spawning
        // or use events from PlayerAttack
        var projectiles = FindObjectsOfType<Game.Gameplay.Combat.Projectile>();
        if (projectiles.Length > 0)
        {
            timeSinceLastAttack = 0f;
            lastAttackTime = Time.time;
        }
    }
    
    private void MonitorAttackPause()
    {
        // Log periodic status
        if (Time.time % 2f < Time.deltaTime)
        {
            int aliveEnemies = 0;
            foreach (var enemy in testEnemies)
            {
                if (enemy != null && !enemy.IsDead)
                    aliveEnemies++;
            }
            
            Debug.Log($"[DEBUG_LOG] Status - Alive enemies: {aliveEnemies}, Time since last attack: {timeSinceLastAttack:F1}s, Attack paused: {attackPausedAfterKill}");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (testEnemies != null)
        {
            foreach (var enemy in testEnemies)
            {
                if (enemy != null)
                {
                    enemy.OnDied -= OnEnemyDied;
                }
            }
        }
    }
}