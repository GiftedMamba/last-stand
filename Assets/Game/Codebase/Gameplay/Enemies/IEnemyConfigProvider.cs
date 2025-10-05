using System.Collections.Generic;
using Game.Configs;

namespace Game.Gameplay.Enemies
{
    public interface IEnemyConfigProvider
    {
        /// <summary>
        /// Returns any random enemy config available, or null if none.
        /// </summary>
        EnemyConfig GetRandomAny();

        /// <summary>
        /// Returns a random config for the given enemy type, or null if none.
        /// </summary>
        EnemyConfig GetRandomForType(EnemyType type);

        /// <summary>
        /// Returns a random config for any of the provided types, or null if none found.
        /// </summary>
        EnemyConfig GetRandomForTypes(IReadOnlyList<EnemyType> types);
    }
}
