using System;
using System.Collections.Generic;
using ASCEND.Core;
using ASCEND.Data;
using UnityEngine;

namespace ASCEND.Systems
{
    public class CentralizedStatSystem : MonoBehaviour
    {
        public static CentralizedStatSystem Instance { get; private set; }

        private readonly List<IStatable> _registeredEntities = new List<IStatable>();

        // ── Events ──────────────────────────────────────────────────────────────

        public event Action<IStatable, CoreStatType, int> OnCoreStatLevelUp;
        public event Action<IStatable, CoreStatType, float, float> OnProgressChanged;

        // ── Singleton ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CentralizedStatSystem] Duplicate instance detected. Destroying self.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[CentralizedStatSystem] Singleton initialized.");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Registration ───────────────────────────────────────────────────────

        public void Register(IStatable entity)
        {
            if (entity == null) return;
            if (!_registeredEntities.Contains(entity))
            {
                _registeredEntities.Add(entity);
                Debug.Log($"[CentralizedStatSystem] Registered entity: {entity.GetType().Name}");
            }
        }

        public void Unregister(IStatable entity)
        {
            if (entity == null) return;
            if (_registeredEntities.Remove(entity))
            {
                Debug.Log($"[CentralizedStatSystem] Unregistered entity: {entity.GetType().Name}");
            }
        }

        // ── Notifications ───────────────────────────────────────────────────────

        public void NotifyStatLevelUp(IStatable entity, CoreStatType stat, int newLevel)
        {
            Debug.Log($"[CentralizedStatSystem] Stat Level Up! Entity={entity.GetType().Name} Stat={stat} NewLevel={newLevel}");
            OnCoreStatLevelUp?.Invoke(entity, stat, newLevel);
            RecalculateDerived(entity, stat);
        }

        public void NotifyProgressChanged(IStatable entity, CoreStatType stat, float current, float threshold)
        {
            OnProgressChanged?.Invoke(entity, stat, current, threshold);
        }

        public void RecalculateDerived(IStatable entity, CoreStatType stat)
        {
            Debug.Log($"[CentralizedStatSystem] Recalculating derived stats for {entity.GetType().Name} after {stat} level up.");

            // Broadcast derived stat changes based on which stat leveled up
            int statValue = entity.GetStat(stat);

            switch (stat)
            {
                case CoreStatType.VIT:
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.MaxHP, CalculateMaxHP(statValue));
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.StatusResistance, CalculateStatusResistance(entity.GetStat(CoreStatType.ARC), statValue));
                    break;

                case CoreStatType.END:
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.MaxStamina, CalculateMaxStamina(statValue));
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.EquipLoad, CalculateEquipLoad(statValue));
                    break;

                case CoreStatType.AGI:
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.StaminaRecovery, CalculateStaminaRecovery(statValue));
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.MovementSpeed, CalculateMovementSpeed(statValue));
                    break;

                case CoreStatType.STR:
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.CritDamage, CalculateCritDamage(statValue));
                    break;

                case CoreStatType.DEX:
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.CritRate, CalculateCritRate(statValue));
                    break;

                case CoreStatType.ARC:
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.MaxMana, CalculateMaxMana(statValue));
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.ManaRecovery, CalculateManaRecovery(statValue));
                    OnDerivedStatChanged?.Invoke(entity, DerivedStatType.StatusResistance, CalculateStatusResistance(statValue, entity.GetStat(CoreStatType.VIT)));
                    break;
            }
        }

        // ── Events ──────────────────────────────────────────────────────────────

        public event Action<IStatable, DerivedStatType, float> OnDerivedStatChanged;

        // ═══════════════════════════════════════════════════════════════════════════
        // DERIVED STAT FORMULAS (all static - the central source of truth)
        // ═══════════════════════════════════════════════════════════════════════════

        public static float CalculateMaxHP(int VIT)
            => 500f + StatCurveCalculator.Curve(VIT) * 1000f;

        public static float CalculateMaxStamina(int END)
            => 100f + StatCurveCalculator.Curve(END) * 300f;

        public static float CalculateStaminaRecovery(int AGI)
            => 10f + StatCurveCalculator.Curve(AGI) * 15f;

        public static float CalculateEquipLoad(int END)
            => 50f + StatCurveCalculator.Curve(END) * 200f;

        public static float CalculateCritRate(int DEX)
            => 5f + StatCurveCalculator.Curve(DEX) * 30f;

        public static float CalculateCritDamage(int STR)
            => 1.5f + StatCurveCalculator.Curve(STR) * 0.5f;

        public static float CalculateMaxMana(int ARC)
            => 50f + StatCurveCalculator.Curve(ARC) * 500f;

        public static float CalculateMovementSpeed(int AGI)
            => 100f + StatCurveCalculator.Curve(AGI) * 10f;

        public static float CalculateGuardStability(float guardBase, int STR)
            => guardBase + StatCurveCalculator.Curve(STR) * 50f;

        public static float CalculateManaRecovery(int ARC)
            => 5f + StatCurveCalculator.Curve(ARC) * 10f;

        // NEW - from STAT_SPEC
        public static float CalculateStatusResistance(int ARC, int VIT)
            => (StatCurveCalculator.Curve(ARC) * 70f) + (StatCurveCalculator.Curve(VIT) * 30f);

        // Crit rate as percentage (for crit roll)
        public static float CalculateCritRatePercent(int DEX)
            => Mathf.Min(5f + DEX * 0.3f, 100f);

        // Damage with crit roll applied
        public static float CalculateDamageWithCrit(float baseDamage, int STR, int DEX, float critRatePercent)
        {
            bool isCrit = UnityEngine.Random.value * 100f < critRatePercent;
            float critMultiplier = isCrit ? CalculateCritDamage(STR) : 1f;
            return baseDamage * critMultiplier;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PROGRESS THRESHOLD FORMULA
        // ═══════════════════════════════════════════════════════════════════════════

        public static float CalculateThreshold(int statValue, float progressBase, float progressBonus, float progressK)
        {
            float curve = StatCurveCalculator.Curve(statValue, progressK);
            return progressBase + curve * progressBonus;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // STAT CLAMPING
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Clamp a stat value within its min/max range.
        /// </summary>
        public static int ClampStat(int value, int min, int max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Clamp a stat value based on StatConfigSO range config.
        /// </summary>
        public static int ClampStat(int value, StatConfigSO config, CoreStatType stat)
        {
            if (config == null)
                return Mathf.Clamp(value, 1, 99);

            var (min, max) = config.GetStatRange(stat);
            return Mathf.Clamp(value, min, max);
        }
    }
}