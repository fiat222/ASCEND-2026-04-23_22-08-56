using ASCEND.Core;

public interface IStatable
{
    int GetStat(CoreStatType stat);
    float GetProgress(CoreStatType stat);
    float GetThreshold(CoreStatType stat);
    void GainProgress(CoreStatType stat, float amount);
}