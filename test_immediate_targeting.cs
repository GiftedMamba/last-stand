using UnityEngine;
using Game.Gameplay.Player;
using Game.Gameplay.Enemies;

/// <summary>
/// Simple test to verify immediate target switching when enemies die
/// </summary>
public class TestImmediateTargeting : MonoBehaviour
{
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private Enemy[] testEnemies;
    
    private void Start()
    {
        Debug.Log("[DEBUG_LOG] Starting immediate targeting test");
        
        // Test scenario: Kill the current target and verify immediate retargeting
        if (playerAttack != null && testEnemies != null && testEnemies.Length > 1)
        {
            Debug.Log($"[DEBUG_LOG] Test setup: {testEnemies.Length} enemies available");
            
            // Let player target first enemy, then kill it to test immediate retargeting
            StartCoroutine(TestTargetSwitching());
        }
        else
        {
            Debug.LogError("[DEBUG_LOG] Test setup incomplete - missing PlayerAttack or test enemies");
        }
    }
    
    private System.Collections.IEnumerator TestTargetSwitching()
    {
        // Wait a bit for initial targeting
        yield return new WaitForSeconds(1f);
        
        // Kill the first enemy to trigger immediate retargeting
        if (testEnemies[0] != null && !testEnemies[0].IsDead)
        {
            Debug.Log("[DEBUG_LOG] Killing first enemy to test immediate retargeting");
            testEnemies[0].TakeDamage(1000f); // Overkill to ensure death
        }
        
        // The player should immediately switch to the next target without waiting for cooldown
        Debug.Log("[DEBUG_LOG] Enemy killed - player should immediately pick new target");
        
        yield return new WaitForSeconds(2f);
        Debug.Log("[DEBUG_LOG] Test completed");
    }
}