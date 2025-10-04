using Game.Configs;
using Game.Core;
using Game.Core.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Enemies
{
  /// <summary>
  /// Awards player experience when the attached Enemy dies, using XP value from EnemyConfig.
  /// Resolves IPlayerLevelService from the nearest VContainer LifetimeScope at runtime.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class EnemyXpAwarder : MonoBehaviour
  {
    [SerializeField] private Enemy _enemy;

    private IPlayerLevelService _playerLevel;
    private bool _subscribed;

    [Inject]
    public void Construct(IPlayerLevelService playerLevel)
    {
      _playerLevel = playerLevel;
    }

    private void Awake()
    {
      if (_enemy == null)
        _enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
      if (_enemy == null)
        _enemy = GetComponent<Enemy>();

      TryResolvePlayerLevel();

      if (_enemy != null && !_subscribed)
      {
        _enemy.OnDied += HandleEnemyDied;
        _subscribed = true;
      }
    }

    private void OnDisable()
    {
      if (_enemy != null && _subscribed)
      {
        _enemy.OnDied -= HandleEnemyDied;
        _subscribed = false;
      }
    }

    private void HandleEnemyDied()
    {
      if (_enemy == null)
        return;

      // Ensure we have a player level service even if DI didn't run (e.g., spawned without resolver)
      TryResolvePlayerLevel();

      int xp = 0;
      EnemyConfig cfg = _enemy.Config;
      if (cfg != null)
      {
        xp = Mathf.Max(0, cfg.XpReward);
      }

      if (xp <= 0)
        return;

      if (_playerLevel == null)
      {
        GameLogger.LogWarning("EnemyXpAwarder: IPlayerLevelService not resolved. Cannot award XP.");
        return;
      }

      long beforeTotal = _playerLevel.TotalXp;
      int beforeLevel = _playerLevel.Level;

      int levelsGained = _playerLevel.AddExperience(xp);

      GameLogger.Log($"XP: +{xp} for killing '{_enemy.name}'. TotalXP: {beforeTotal} -> {_playerLevel.TotalXp}.");
      if (levelsGained > 0)
      {
        GameLogger.Log($"Level Up! {beforeLevel} -> {_playerLevel.Level} (+{levelsGained}).");
      }
    }

    private void TryResolvePlayerLevel()
    {
      if (_playerLevel != null) return;

      var scope = GetComponentInParent<LifetimeScope>();
      if (scope != null)
      {
        if (!scope.Container.TryResolve(out _playerLevel))
        {
          // Will log again on death if still null
        }
      }
    }
  }
}