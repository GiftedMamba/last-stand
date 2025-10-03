using UnityEngine;
using Game.Gameplay.Player;
using Game.Gameplay.Enemies;
using Game.Configs;

/// <summary>
/// Test script to verify that PlayerAttack now continues attacking continuously
/// by cooldown even when enemies die, instead of pausing.
/// </summary>
public class TestContinuousAttack : MonoBehaviour
{
    [Header("Test Components")]
    public PlayerAttack playerAttack;
    public EnemyRegistry enemyRegistry;
    public PlayerConfig playerConfig;
    
    [Header("Test Results")]
    public bool testPassed = false;
    public float testDuration = 10f;
    public int attackCount = 0;
    public int enemyDeathCount = 0;
    
    private float testTimer;
    private float lastAttackTime;
    private float expectedAttackInterval;
    
    void Start()
    {
        Debug.Log("[DEBUG_LOG] Starting continuous attack test...");
        
        if (playerAttack == null || enemyRegistry == null || playerConfig == null)
        {
            Debug.LogError("[DEBUG_LOG] Missing required components for test!");
            return;
        }
        
        // Calculate expected attack interval from config
        expectedAttackInterval = 1f / Mathf.Max(0.01f, playerConfig.BaseAttackSpeed);
        Debug.Log($"[DEBUG_LOG] Expected attack interval: {expectedAttackInterval:F2}s (Attack Speed: {playerConfig.BaseAttackSpeed})");
        
        // Initialize player attack system
        playerAttack.Init(playerConfig, enemyRegistry);
        
        // Subscribe to enemy deaths if possible
        var enemies = enemyRegistry.Enemies;
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.OnDied += OnEnemyDied;
                }
            }
        }
        
        testTimer = testDuration;
    }
    
    void Update()
    {
        testTimer -= Time.deltaTime;
        
        // Monitor for attacks (simplified - checking for new projectiles)
        CheckForNewAttacks();
        
        // Verify continuous attacking behavior
        VerifyContinuousAttacking();
        
        if (testTimer <= 0f)
        {
            CompleteTest();
        }
    }
    
    private void CheckForNewAttacks()
    {
        // Simple heuristic: if time since last attack is close to 0, we likely just attacked
        float timeSinceLastCheck = Time.time - lastAttackTime;
        
        // Check if we should have attacked based on cooldown
        if (timeSinceLastCheck >= expectedAttackInterval - 0.1f)
        {
            var projectiles = FindObjectsOfType<Game.Gameplay.Combat.Projectile>();
            if (projectiles.Length > 0)
            {
                // New projectile found, likely indicates attack
                attackCount++;
                lastAttackTime = Time.time;
                Debug.Log($"[DEBUG_LOG] Attack detected! Total attacks: {attackCount}");
            }
        }
    }
    
    private void VerifyContinuousAttacking()
    {
        // Check if we have valid targets
        var enemies = enemyRegistry.Enemies;
        bool hasLiveEnemies = false;
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    hasLiveEnemies = true;
                    break;
                }
            }
        }
        
        // If we have enemies and haven't attacked in too long, there might be an issue
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (hasLiveEnemies && timeSinceLastAttack > expectedAttackInterval * 2f && attackCount > 0)
        {
            Debug.LogWarning($"[DEBUG_LOG] Potential attack pause detected! {timeSinceLastAttack:F2}s since last attack with live enemies present.");
        }
    }
    
    private void OnEnemyDied()
    {
        enemyDeathCount++;
        Debug.Log($"[DEBUG_LOG] Enemy died! Total deaths: {enemyDeathCount}. Verifying attack continues...");
        
        // Check shortly after enemy death if attacks resume
        Invoke(nameof(CheckAttackResumption), expectedAttackInterval + 0.5f);
    }
    
    private void CheckAttackResumption()
    {
        var enemies = enemyRegistry.Enemies;
        bool hasLiveEnemies = false;
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    hasLiveEnemies = true;
                    break;
                }
            }
        }
        
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (hasLiveEnemies && timeSinceLastAttack < expectedAttackInterval * 3f)
        {
            Debug.Log("[DEBUG_LOG] SUCCESS: Attack resumed after enemy death!");
        }
        else if (hasLiveEnemies)
        {
            Debug.LogError($"[DEBUG_LOG] FAILURE: Attack did not resume after enemy death! {timeSinceLastAttack:F2}s since last attack.");
        }
    }
    
    private void CompleteTest()
    {
        // Evaluate test results
        float expectedAttacks = testDuration / expectedAttackInterval;
        float attackEfficiency = (float)attackCount / expectedAttacks;
        
        testPassed = attackEfficiency > 0.5f && enemyDeathCount > 0; // At least 50% efficiency and some enemy deaths occurred
        
        Debug.Log($"[DEBUG_LOG] Test completed!");
        Debug.Log($"[DEBUG_LOG] Expected attacks: {expectedAttacks:F1}, Actual attacks: {attackCount}");
        Debug.Log($"[DEBUG_LOG] Attack efficiency: {attackEfficiency:P1}");
        Debug.Log($"[DEBUG_LOG] Enemy deaths: {enemyDeathCount}");
        Debug.Log($"[DEBUG_LOG] Test result: {(testPassed ? "PASSED" : "FAILED")}");
        
        if (testPassed)
        {
            Debug.Log("[DEBUG_LOG] Fix verified: Player attacks continuously by cooldown even when enemies die!");
        }
        else
        {
            Debug.LogError("[DEBUG_LOG] Fix may not be working properly or test conditions not met.");
        }
        
        enabled = false;
    }
    
    void OnDestroy()
    {
        // Cleanup event subscriptions
        if (enemyRegistry?.Enemies != null)
        {
            foreach (var enemy in enemyRegistry.Enemies)
            {
                if (enemy != null)
                {
                    enemy.OnDied -= OnEnemyDied;
                }
            }
        }
    }
}