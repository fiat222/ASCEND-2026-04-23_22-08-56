using System;
using UnityEngine;

[Serializable]
public class StatConfig
{
    [Header("Curve Settings")]
    public float curveK = 0.05f;

    [Header("Output Range")]
    public float baseValue;
    public float bonusAtMax;
}

public static class StatCurveCalculator
{
    public static float Curve(int stat, float k = 0.05f)
        => 1f - Mathf.Exp(-k * stat);

    public static float Calculate(StatConfig config, int statValue)
    {
        float curve = Curve(statValue, config.curveK);
        return config.baseValue + curve * config.bonusAtMax;
    }
}
