# Archer Defense — Game Design Document (Final Prototype v1.0)

## 1) Core Concept
- Defend **three separate spots**; each has its own HP.
- Enemies **attack the closest spot**.
- **Archer auto-attacks** closest enemy. Player can **tap** an enemy to override target. Archer keeps focus until that unit dies or player retargets.
- **4 active spells** (cooldown-based).
- After **waves 1, 3, 5, 7, 9**, draft **1 of 2** passive abilities.
- Each passive has **up to 3 levels**. At level 3, it’s removed from the pool.
- **10 waves** total. **Boss** every **3rd** wave (3, 6, 9). **Wave 10** spawns **all bosses** together.
- Within each wave, **spawn rate ramps up** (starts slow → ends fast).

---

## 2) Player Systems

### 2.1 Archer
- **Base Damage:** 20
- **Attack Rate:** 1.0 shots/sec
- **Projectile Speed:** 24 u/s
- **Range:** 18 u
- **Targeting:** closest enemy unless player override; never switch mid-target unless enemy dies or player retargets.

### 2.2 Spots (3 total)
- **HP:** 1200 each
- **Regen:** none
- **Shielding:** via active spell only

### 2.3 Active Spells
1. **Stun** — AoE (radius 4u), 2.5s stun. Cooldown: 12s
2. **Spot Shield** — all spots invulnerable 3s. Cooldown: 25s
3. **Howl** — global -30% enemy damage and -10% move speed for 6s. Cooldown: 20s
4. **Explosive Arrow** — AoE (radius 2.5u) dealing (200 + 15 × CurrentWave) damage. Cooldown: 18s

### 2.4 Passive Abilities
Chosen after waves 1, 3, 5, 7, 9.  
Each can be taken up to level 3; then removed from pool.

1. **Split Shot** — fire 3 / 5 / 7 total arrows.
    - Per-arrow damage: 0.85 / 0.75 / 0.65 of base.
2. **Critical Strike** — +10% / +20% / +30% crit chance, crit ×2 damage.
3. **Frost Arrow** — slow 20% / 30% / 40% for 2.5s.
4. **Splash Damage** — 30% / 50% / 70% splash damage in radius 1.6 / 1.8 / 2.0u.
5. **Disintegrate** — 5% / 10% / 15% chance to insta-kill non-boss. Bosses take ×2 damage (no insta-kill).
6. **Desolator** — -2 / -4 / -6 armor for 3s on hit.
7. **Attack Speed** — +12% / +24% / +36% attack rate.
8. **Piercing Arrow** — arrows pierce 1 / 2 / 3 extra enemies, -15% damage per pierce.
9. **Poison Arrow** — DoT: 12 / 24 / 36 DPS for 3s (refresh on hit).
10. **Killer Instinct** — bonus damage scales with enemy missing HP:
- Bonus% = MaxBonus × (1 − CurrentHP / MaxHP)
- MaxBonus = 20% / 40% / 60%

---

## 3) Combat Rules

### 3.1 Armor
- Damage reduced by 6% per armor, capped at 60%.  
  EffectiveDamage = Base × (1 − min(Armor × 0.06, 0.60))

### 3.2 Enemy-to-Spot Damage
- Melee: DPS while in contact.
- Ranged/Caster: DPS on attack tick.
- Spot damage reductions (e.g. Howl) apply.

---

## 4) Enemies

| Enemy            | HP   | Armor | Speed | DPS to Spot | Notes |
|------------------|------|-------|-------|-------------|-------|
| Skeleton         | 100  | 0     | 2.2   | 8 /s        | Basic melee |
| Big Skeleton     | 240  | 2     | 2.0   | 10 /s       | Tanky melee |
| **Boss Skeleton**| 1200 | 6     | 1.8   | 20 /s       | Slow, durable |
| Ghoul            | 120  | 0     | 2.8   | 9 /s        | Fast melee |
| Big Ghoul        | 300  | 2     | 2.6   | 12 /s       | Fast, tankier |
| **Boss Ghoul**   | 1400 | 4     | 2.5   | 22 /s       | Fast boss |
| Weak Goblin      | 80   | 0     | 2.5   | 7 /s        | Swarm |
| Goblin Warrior   | 220  | 2     | 2.4   | 11 /s       | Core melee |
| Shaman Goblin    | 180  | 1     | 2.3   | 6 /s        | Buff aura: +20% dmg, +10% speed, heal allies 20 HP / 2s |
| **Boss Goblin**  | 1600 | 6     | 2.2   | 25 /s       | Final boss-tier |

---

## 5) Wave Design

### Spawn Behavior
- Waves ramp up: spawn interval decreases linearly from start to end.
- Early waves: 2.6s → 1.2s
- Mid waves: 2.2s → 1.0s
- Late waves: 2.0s → 0.8s

### Target Wave Durations
- Wave 1–2: ~60s
- Wave 3–6: ~70–80s
- Wave 7–9: ~80–90s
- Wave 10: ~110–130s

### Wave Breakdown

- **Wave 1** — 16× Skeleton, 4× Weak Goblin
- **Wave 2** — 18× Skeleton, 6× Big Skeleton, 2× Weak Goblin
- **Wave 3 (Boss Skeleton)** — 1× Boss Skeleton, 16× Skeleton, 6× Big Skeleton, 4× Ghoul
- **Wave 4** — 14× Ghoul, 8× Skeleton, 6× Big Skeleton, 4× Weak Goblin
- **Wave 5** — 14× Ghoul, 6× Big Ghoul, 8× Big Skeleton, 8× Goblin Warrior
- **Wave 6 (Boss Ghoul)** — 1× Boss Ghoul, 12× Ghoul, 6× Big Ghoul, 8× Goblin Warrior, 6× Big Skeleton, 5× Weak Goblin
- **Wave 7** — 18× Weak Goblin, 14× Goblin Warrior, 6× Ghoul, 4× Big Ghoul
- **Wave 8** — 16× Goblin Warrior, 8× Shaman Goblin, 10× Ghoul, 6× Big Ghoul, 6× Big Skeleton
- **Wave 9 (Boss Goblin)** — 1× Boss Goblin (+2 Shamans escort), 12× Goblin Warrior, 6× Shaman Goblin, 12× Ghoul, 10× Big Ghoul, 7× Big Skeleton
- **Wave 10 (Final, all bosses)** — 1× Boss Skeleton, 1× Boss Ghoul, 1× Boss Goblin, 16× Skeleton, 10× Big Skeleton, 12× Ghoul, 10× Big Ghoul, 12× Goblin Warrior, 7× Shaman Goblin

---

## 6) UI / UX
- **Top Bar:** Wave counter, timer, spot HP bars.
- **Center:** Archer, enemies, projectile effects.
- **Bottom Bar:** 4 spell buttons with cooldown indicators.
- **Draft Screen:** shows 2 passives after 1/3/5/7/9. Selecting shows level-up feedback.
- **Targeting:** tap enemy = red reticle; tap empty = revert to closest enemy.

---

## 7) Victory & Defeat
- **Victory:** survive Wave 10.
- **Defeat:** all 3 spots destroyed.

---

## 8) Stretch Goals (Post-Prototype)
- Endless/survival mode.
- Difficulty scaling.
- Meta-progression (unlock passives, spells, cosmetics).  
