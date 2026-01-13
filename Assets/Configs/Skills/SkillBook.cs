// using System.Collections.Generic;
// using AutoChess.Configs;

// namespace AutoChess.Core
// {
//     /// <summary>
//     /// Central place to build reusable SkillDefSO / BuffDefSO templates for quick iteration.
//     /// These are plain runtime definitions (NOT assets). Later you can replace them with SkillDefSOSO/BuffDefSODefSO assets.
//     /// </summary>
//     public static class SkillBook
//     {
//         // ---------------- BuffDefSO templates ----------------


//         // ---------------- Skill templates ----------------

//         /// <summary>
//         /// Basic attack:
//         /// - 通常由战斗逻辑控制频率（例如 Unit.AtkInterval），所以这里 Cooldown 设 0
//         /// - 伤害按命中时 source.Atk 计算（吃 BuffDefSO）
//         /// </summary>
//         public static SkillDefSO BasicAttack(float range)
//         {
//             return new SkillDefSO
//             {
//                 id = "basic_attack",
//                 cooldown = 0f,
//                 Range = range,
//                 Effects = new List<EffectDefSO> { new DamageFromAtkEffect { Mult = 1f } }
//             };
//         }

//         /// <summary>
//         /// Power strike: a heavier basic attack variant (e.g., 150% atk).
//         /// </summary>
//         public static SkillDefSO PowerStrike(float cooldown = 4f, float range = 2f, float mult = 1.5f)
//         {
//             return new SkillDefSO
//             {
//                 id = "power_strike",
//                 cooldown = cooldown,
//                 Range = range,
//                 Effects = new List<EffectDefSO> { new DamageFromAtkEffect { Mult = mult } }
//             };
//         }

//         /// <summary>
//         /// Fireball: flat damage + apply Burn.
//         /// </summary>
//         public static SkillDefSO Fireball(float cooldown = 5f, float range = 6f, float damage = 25f)
//         {
//             var burn = Burn(duration: 3f, tickInterval: 1f, tickDamage: 5f);
//             return new SkillDefSO
//             {
//                 id = "fireball",
//                 Cooldown = cooldown,
//                 Range = range,
//                 Effects = new List<EffectDefSO>
//                 {
//                     new DamageEffect { Amount = damage },
//                     new ApplyBuffDefSOEffect { BuffDefSO = burn }
//                 }
//             };
//         }

//         /// <summary>
//         /// Frost bolt: flat damage + slow.
//         /// </summary>
//         public static SkillDefSO FrostBolt(
//             float cooldown = 5f,
//             float range = 6f,
//             float damage = 18f,
//             float slowduration = 2.5f,
//             float deltaMoveSpeed = -1.0f)
//         {
//             var slow = Slow(duration: slowduration, deltaMoveSpeed: deltaMoveSpeed);
//             return new SkillDefSO
//             {
//                 id = "frost_bolt",
//                 Cooldown = cooldown,
//                 Range = range,
//                 Effects = new List<EffectDefSO>
//                 {
//                     new DamageEffect { Amount = damage },
//                     new ApplyBuffDefSOEffect { BuffDefSO = slow }
//                 }
//             };
//         }

//         /// <summary>
//         /// Rally: self-cast ATK BuffDefSO.
//         /// 目前 SystemSkill 只会找敌人目标，所以这个技能需要你后续在 SystemSkill 里支持“自选目标/友军/自身”。
//         /// </summary>
//         public static SkillDefSO Rally(float cooldown = 8f, float range = 0f, float duration = 3f, float deltaAtk = 10f)
//         {
//             var atkUp = AtkUp(duration: duration, deltaAtk: deltaAtk);
//             return new SkillDefSO
//             {
//                 id = "rally",
//                 Cooldown = cooldown,
//                 Range = range,
//                 Effects = new List<EffectDefSO>
//                 {
//                     new ApplyBuffDefSOEffect { BuffDefSO = atkUp }
//                 }
//             };
//         }

//         /// <summary>
//         /// Runtime-only effect: deal source.Atk * Mult damage at cast time.
//         /// (因为你当前 SystemEffect.cs 里没有 DamageFromAtkEffect，这里补一个，避免 SkillBook 依赖缺失)
//         /// </summary>
//         private sealed class DamageFromAtkEffect : EffectDefSO
//         {
//             public float Mult = 1f;

//             public void Apply(BattleWorld world, Unit source, Unit target)
//             {
//                 if (target == null || target.IsDead) return;
//                 float dmg = source.Atk * Mult;
//                 target.Hp -= dmg;

//                 // 如果你希望这里也能触发事件，把下面这行打开（前提：BattleWorld 有 EmitAttack）
//                 // world.EmitAttack(source, target, dmg);
//             }
//         }
//     }
// }
