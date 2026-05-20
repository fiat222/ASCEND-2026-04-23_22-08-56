using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    [SerializeField] private float maxHp  = 300f;
    [SerializeField] private float def    = 20f;

    [Header("Interrupt Bar (small)")]
    [SerializeField] private float interruptThreshold     = 40f;
    [SerializeField] private float interruptDifficultyInc = 15f;
    [SerializeField] private int   interruptMaxStacks     = 10;
    [SerializeField] private float interruptDuration      = 0.8f;

    [Header("Stun Bar (large)")]
    [SerializeField] private float stunThreshold     = 100f;
    [SerializeField] private float stunDifficultyInc = 25f;
    [SerializeField] private int   stunMaxStacks     = 5;
    [SerializeField] private float stunDuration      = 3f;
    [SerializeField] private float stunImmunity      = 5f;
    [SerializeField] private float stunDamageBonus   = 1.3f;

    [Header("HP Bar UI")]
    [SerializeField] private Slider hpSlider;

    [Header("Runtime (read-only)")]
    [SerializeField] private float currentHp;
    [SerializeField] private float interruptAccum;
    [SerializeField] private float stunAccum;

    public bool  IsAlive   => currentHp > 0f;
    public float CurrentHp => currentHp;
    public float MaxHp     => maxHp;

    public event Action OnDied;
    public event Action OnHit;
    public event Action OnInterrupted;
    public event Action OnStunned;

    // ── Stagger state ──────────────────────────────────────────────────────────

    private int   _interruptStacks;
    private int   _stunStacks;
    private bool  _isInterrupted;
    private bool  _isStunned;
    private bool  _stunImmune;
    private float _currentInterruptThreshold;
    private float _currentStunThreshold;
    private float _activeDamageBonus = 1f;

    // ── Init ───────────────────────────────────────────────────────────────────

    private void Awake()
    {
        currentHp = maxHp;
        _currentInterruptThreshold = interruptThreshold;
        _currentStunThreshold      = stunThreshold;
        RefreshSlider();
    }

    private void Update()
    {
        if (hpSlider != null && Camera.main != null)
            hpSlider.transform.parent.LookAt(
                hpSlider.transform.parent.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
    }

    // ── IDamageable ────────────────────────────────────────────────────────────

    public void TakeDamage(float rawDamage)
    {
        if (!IsAlive) return;

        float finalDmg = rawDamage * (100f / (100f + def)) * _activeDamageBonus;
        currentHp = Mathf.Max(0f, currentHp - finalDmg);

        RefreshSlider();
        OnHit?.Invoke();

        if (currentHp <= 0f)
            OnDied?.Invoke();
    }

    public void TakeStagger(float staggerDmg)
    {
        if (!IsAlive) return;

        Debug.Log($"[EnemyStats] TakeStagger: {staggerDmg:F1} (Interrupt: {interruptAccum:F1}/{_currentInterruptThreshold:F1}, Stun: {stunAccum:F1}/{_currentStunThreshold:F1})");

        AccumulateStagger(staggerDmg);
    }

    // ── Stagger logic ──────────────────────────────────────────────────────────

    private void AccumulateStagger(float staggerDmg)
    {
        // Interrupt bar
        if (!_isInterrupted && _interruptStacks < interruptMaxStacks)
        {
            interruptAccum += staggerDmg;
            if (interruptAccum >= _currentInterruptThreshold)
            {
                interruptAccum = 0f;
                _interruptStacks = Mathf.Min(_interruptStacks + 1, interruptMaxStacks);
                _currentInterruptThreshold += interruptDifficultyInc;
                StartCoroutine(InterruptRoutine());
            }
        }

        // Stun bar
        if (!_isStunned && !_stunImmune && _stunStacks < stunMaxStacks)
        {
            stunAccum += staggerDmg;
            if (stunAccum >= _currentStunThreshold)
            {
                stunAccum = 0f;
                _stunStacks = Mathf.Min(_stunStacks + 1, stunMaxStacks);
                _currentStunThreshold += stunDifficultyInc;
                StartCoroutine(StunRoutine());
            }
        }
    }

    private IEnumerator InterruptRoutine()
    {
        _isInterrupted = true;
        OnInterrupted?.Invoke();
        yield return new WaitForSeconds(interruptDuration);
        _isInterrupted = false;
    }

    private IEnumerator StunRoutine()
    {
        _isStunned         = true;
        _activeDamageBonus = stunDamageBonus;
        OnStunned?.Invoke();

        yield return new WaitForSeconds(stunDuration);

        _isStunned         = false;
        _activeDamageBonus = 1f;
        _stunImmune        = true;

        yield return new WaitForSeconds(stunImmunity);
        _stunImmune = false;
    }

    // ── UI ─────────────────────────────────────────────────────────────────────

    private void RefreshSlider()
    {
        if (hpSlider == null) return;
        hpSlider.maxValue = maxHp;
        hpSlider.value    = currentHp;
    }

    public bool IsInterrupted => _isInterrupted;
    public bool IsStunned     => _isStunned;

    // Called on non-server clients by NetworkEnemyStats to sync host's result
    public void SyncHp(float hp)
    {
        bool wasAlive = IsAlive;
        currentHp = hp;
        RefreshSlider();
        OnHit?.Invoke();
        if (currentHp <= 0f && wasAlive) OnDied?.Invoke();
    }
}