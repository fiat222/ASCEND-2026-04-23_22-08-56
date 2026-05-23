using System;
using UnityEngine;
using ASCEND.Core;

namespace ASCEND.Data
{
    [CreateAssetMenu(fileName = "WarriorStatConfig", menuName = "ASCEND/Stats/Warrior")]
    public class StatConfigSO : ScriptableObject
    {
        [Header("Class")]
        public string className = "Warrior";

        [Header("Base Stats")]
        public int baseVIT = 10;
        public int baseEND = 10;
        public int baseAGI = 10;
        public int baseSTR = 10;
        public int baseDEX = 10;
        public int baseARC = 10;

        [Header("VIT Range")]
        public int vitMin = 1;
        public int vitMax = 99;

        [Header("END Range")]
        public int endMin = 1;
        public int endMax = 99;

        [Header("AGI Range")]
        public int agiMin = 1;
        public int agiMax = 99;

        [Header("STR Range")]
        public int strMin = 1;
        public int strMax = 99;

        [Header("DEX Range")]
        public int dexMin = 1;
        public int dexMax = 99;

        [Header("ARC Range")]
        public int arcMin = 1;
        public int arcMax = 99;

        [Header("VIT Progress Config")]
        public float vitProgressBase = 50f;
        public float vitProgressBonus = 150f;
        public float vitProgressK = 0.05f;

        [Header("END Progress Config")]
        public float endProgressBase = 50f;
        public float endProgressBonus = 150f;
        public float endProgressK = 0.05f;

        [Header("AGI Progress Config")]
        public float agiProgressBase = 50f;
        public float agiProgressBonus = 150f;
        public float agiProgressK = 0.05f;

        [Header("STR Progress Config")]
        public float strProgressBase = 50f;
        public float strProgressBonus = 150f;
        public float strProgressK = 0.05f;

        [Header("DEX Progress Config")]
        public float dexProgressBase = 50f;
        public float dexProgressBonus = 150f;
        public float dexProgressK = 0.05f;

        [Header("ARC Progress Config")]
        public float arcProgressBase = 50f;
        public float arcProgressBonus = 150f;
        public float arcProgressK = 0.05f;

        // ── Range helpers ──────────────────────────────────────────────────────────

        public (int min, int max) GetStatRange(CoreStatType stat)
        {
            return stat switch
            {
                CoreStatType.VIT => (vitMin, vitMax),
                CoreStatType.END => (endMin, endMax),
                CoreStatType.AGI => (agiMin, agiMax),
                CoreStatType.STR => (strMin, strMax),
                CoreStatType.DEX => (dexMin, dexMax),
                CoreStatType.ARC => (arcMin, arcMax),
                _ => (1, 99)
            };
        }

        public int GetMin(CoreStatType stat)
        {
            return stat switch
            {
                CoreStatType.VIT => vitMin,
                CoreStatType.END => endMin,
                CoreStatType.AGI => agiMin,
                CoreStatType.STR => strMin,
                CoreStatType.DEX => dexMin,
                CoreStatType.ARC => arcMin,
                _ => 1
            };
        }

        public int GetMax(CoreStatType stat)
        {
            return stat switch
            {
                CoreStatType.VIT => vitMax,
                CoreStatType.END => endMax,
                CoreStatType.AGI => agiMax,
                CoreStatType.STR => strMax,
                CoreStatType.DEX => dexMax,
                CoreStatType.ARC => arcMax,
                _ => 99
            };
        }

        // ── Progress config helpers ─────────────────────────────────────────────

        public (float baseVal, float bonus, float k) GetProgressConfig(CoreStatType stat)
        {
            return stat switch
            {
                CoreStatType.VIT => (vitProgressBase, vitProgressBonus, vitProgressK),
                CoreStatType.END => (endProgressBase, endProgressBonus, endProgressK),
                CoreStatType.AGI => (agiProgressBase, agiProgressBonus, agiProgressK),
                CoreStatType.STR => (strProgressBase, strProgressBonus, strProgressK),
                CoreStatType.DEX => (dexProgressBase, dexProgressBonus, dexProgressK),
                CoreStatType.ARC => (arcProgressBase, arcProgressBonus, arcProgressK),
                _ => (50f, 150f, 0.05f)
            };
        }

        // ── Base stat helpers ───────────────────────────────────────────────────

        public int GetBaseStat(CoreStatType stat)
        {
            return stat switch
            {
                CoreStatType.VIT => baseVIT,
                CoreStatType.END => baseEND,
                CoreStatType.AGI => baseAGI,
                CoreStatType.STR => baseSTR,
                CoreStatType.DEX => baseDEX,
                CoreStatType.ARC => baseARC,
                _ => 10
            };
        }
    }
}