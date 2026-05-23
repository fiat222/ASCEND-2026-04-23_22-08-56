using System;
using UnityEngine;
using ASCEND.Core;

namespace ASCEND.Systems
{
    public class StatusEffectHandler : MonoBehaviour
    {
        [Header("Status Bars")]
        public StatusBar bleeding = new StatusBar();
        public StatusBar poison = new StatusBar();
        public StatusBar freezing = new StatusBar();

        [Header("Default Thresholds")]
        [SerializeField] private float _bleedingThreshold = 100f;
        [SerializeField] private float _poisonThreshold = 80f;
        [SerializeField] private float _freezeThreshold = 90f;

        [Header("Default Durations")]
        [SerializeField] private float _bleedingDuration = 20f;
        [SerializeField] private float _poisonDuration = 60f;
        [SerializeField] private float _freezeDuration = 90f;

        [Header("Effect Data (optional — for defaults)")]
        [SerializeField] private StatusEffectSO _bleedingEffect;
        [SerializeField] private StatusEffectSO _poisonEffect;
        [SerializeField] private StatusEffectSO _freezeEffect;

        // Events
        public event Action<StatusEffectType> OnStatusTriggered;
        public event Action<StatusEffectType> OnStatusEnded;

        private void Start()
        {
            // Initialize thresholds from defaults or from SO if assigned
            if (_bleedingEffect != null)
            {
                bleeding.currentThreshold = _bleedingEffect.buildupThreshold;
            }
            else
            {
                bleeding.currentThreshold = _bleedingThreshold;
            }

            if (_poisonEffect != null)
            {
                poison.currentThreshold = _poisonEffect.buildupThreshold;
            }
            else
            {
                poison.currentThreshold = _poisonThreshold;
            }

            if (_freezeEffect != null)
            {
                freezing.currentThreshold = _freezeEffect.buildupThreshold;
            }
            else
            {
                freezing.currentThreshold = _freezeThreshold;
            }
        }

private void Update()
    {
        // Tick duration for active effects
        TickAndCheckStatusBar(bleeding, StatusEffectType.Bleeding);
        TickAndCheckStatusBar(poison, StatusEffectType.Poison);
        TickAndCheckStatusBar(freezing, StatusEffectType.Freezing);
    }

    private void TickAndCheckStatusBar(StatusBar bar, StatusEffectType effectType)
    {
        if (!bar.isActive) return;

        bar.timeRemaining -= Time.deltaTime;
        if (bar.timeRemaining <= 0f)
        {
            bar.timeRemaining = 0f;
            EndStatus(bar, effectType);
        }
    }

        /// <summary>
        /// Called by WeaponHitbox on hit. Fills bars if status is inactive.
        /// </summary>
        public void ApplyStatusHit(WeaponSO weapon, float statusResistancePercent)
        {
            if (weapon == null) return;

            // statusResistancePercent is 0-100 range from StatusResistance derived stat
            // Convert to fraction for reduction
            float resistanceFraction = statusResistancePercent / 100f;

            // Bleeding
            if (!bleeding.isActive && weapon.bleedingDmg > 0f)
            {
                float buildup = weapon.bleedingDmg * (1f - resistanceFraction);
                bleeding.currentAccumulation += buildup;

                Debug.Log($"[StatusEffectHandler] Bleeding buildup: +{buildup:F1} ({bleeding.currentAccumulation:F1}/{bleeding.currentThreshold:F1})");

                if (bleeding.currentAccumulation >= bleeding.currentThreshold)
                {
                    TriggerStatus(bleeding, _bleedingEffect, StatusEffectType.Bleeding);
                }
            }

            // Poison
            if (!poison.isActive && weapon.poisonDmg > 0f)
            {
                float buildup = weapon.poisonDmg * (1f - resistanceFraction);
                poison.currentAccumulation += buildup;

                Debug.Log($"[StatusEffectHandler] Poison buildup: +{buildup:F1} ({poison.currentAccumulation:F1}/{poison.currentThreshold:F1})");

                if (poison.currentAccumulation >= poison.currentThreshold)
                {
                    TriggerStatus(poison, _poisonEffect, StatusEffectType.Poison);
                }
            }

            // Freezing
            if (!freezing.isActive && weapon.freezeDmg > 0f)
            {
                float buildup = weapon.freezeDmg * (1f - resistanceFraction);
                freezing.currentAccumulation += buildup;

                Debug.Log($"[StatusEffectHandler] Freezing buildup: +{buildup:F1} ({freezing.currentAccumulation:F1}/{freezing.currentThreshold:F1})");

                if (freezing.currentAccumulation >= freezing.currentThreshold)
                {
                    TriggerStatus(freezing, _freezeEffect, StatusEffectType.Freezing);
                }
            }
        }

        /// <summary>
        /// Activates a status effect, locks bar at 0, starts duration.
        /// </summary>
        public void TriggerStatus(StatusBar bar, StatusEffectSO data, StatusEffectType effectType)
        {
            bar.isActive = true;
            bar.currentAccumulation = 0f;
            bar.timeRemaining = data != null ? data.duration : GetDefaultDuration(effectType);

            Debug.Log($"[StatusEffectHandler] Status TRIGGERED: {effectType}, duration={bar.timeRemaining}s");

            OnStatusTriggered?.Invoke(effectType);
        }

        /// <summary>
        /// Ends a status effect, resets bar accumulation.
        /// </summary>
        public void EndStatus(StatusBar bar, StatusEffectType effectType)
        {
            if (!bar.isActive) return;

            bar.isActive = false;
            bar.currentAccumulation = 0f;
            bar.timeRemaining = 0f;

            Debug.Log($"[StatusEffectHandler] Status ENDED: {effectType}");

            OnStatusEnded?.Invoke(effectType);
        }

        private float GetDefaultDuration(StatusEffectType effectType) => effectType switch
        {
            StatusEffectType.Bleeding => _bleedingDuration,
            StatusEffectType.Poison => _poisonDuration,
            StatusEffectType.Freezing => _freezeDuration,
            _ => 20f
        };

        // Public accessors for external systems
        public bool IsBleedingActive => bleeding.isActive;
        public bool IsPoisoned => poison.isActive;
        public bool IsFrozen => freezing.isActive;

        public float GetBleedingTimeRemaining => bleeding.timeRemaining;
        public float GetPoisonTimeRemaining => poison.timeRemaining;
        public float GetFreezingTimeRemaining => freezing.timeRemaining;
    }
}