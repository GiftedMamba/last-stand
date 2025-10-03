using Game.Gameplay.Abilities;

namespace Game.Gameplay.Abilities
{
    public interface IGlobalAbilityService
    {
        void Trigger(GlobalAbility ability);
    }
}