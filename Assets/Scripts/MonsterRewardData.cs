using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mine Dungeon/Monster Reward")]
public sealed class MonsterRewardData : ScriptableObject
{
    [Min(0)] public int money;
    [Min(0)] public int experience;
    [Serializable] public sealed class Drop
    {
        public MaterialData material;
        [Tooltip("백분율: 0 = 지급 안 함, 100 = 항상 지급"), Range(0,100)] public float chancePercent = 100;
        [Min(1)] public int minimum = 1;
        [Tooltip("최소·최대 모두 포함"), Min(1)] public int maximum = 1;
        public bool IsValid => material != null && material.IsValid && minimum > 0 && maximum >= minimum
            && !float.IsNaN(chancePercent) && !float.IsInfinity(chancePercent) && chancePercent >= 0 && chancePercent <= 100;
        public int Roll()
        {
            if (!IsValid || chancePercent <= 0 || (chancePercent < 100 && UnityEngine.Random.value * 100 >= chancePercent)) return 0;
            // Avoid maximum + 1 overflow while including both endpoints.
            long span = (long)maximum - minimum + 1;
            long offset = Math.Min(span - 1, (long)(UnityEngine.Random.value * (double)span));
            return (int)(minimum + offset);
        }
    }
    public Drop[] materials = Array.Empty<Drop>();
    public string ValidationError
    {
        get
        {
            if (money < 0 || experience < 0) return "돈과 경험치는 음수일 수 없습니다.";
            if (materials != null) for (int i=0;i<materials.Length;i++)
                if (materials[i] == null || !materials[i].IsValid) return $"재료 항목 {i+1}: 참조·확률(0~100%)·수량(1 ≤ 최소 ≤ 최대)을 확인하세요.";
            return null;
        }
    }
    private void OnValidate() { var error=ValidationError; if(error!=null) Debug.LogWarning(error,this); }
}
