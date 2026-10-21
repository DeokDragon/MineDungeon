using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MonsterProfileRuntime
{
    public static MineMonsterKind Kind(EnemyHealth hp)
    {
        if (!hp || hp.GetComponent<StoneGolemBossAI>()) throw new InvalidOperationException("일반 몬스터만 지원합니다.");
        if (hp.GetComponent<StoneMonsterAI>()) return MineMonsterKind.Stone;
        if (hp.GetComponent<IronMonsterAI>()) return MineMonsterKind.Iron;
        if (hp.GetComponent<CrystalMonsterAI>()) return MineMonsterKind.Crystal;
        if (hp.GetComponent<CoalMonsterAI>()) return MineMonsterKind.Coal;
        if (hp.GetComponent<MagnetMonsterAI>()) return MineMonsterKind.Magnet;
        throw new InvalidOperationException("알 수 없는 일반 몬스터");
    }
    public static int Attack(EnemyHealth hp) => hp.GetComponent<StoneMonsterAI>() ? hp.GetComponent<StoneMonsterAI>().AttackDamage : hp.GetComponent<NormalMonsterAI>().AttackDamage;
    public static bool ValidPose(MonsterMapProfile.Entry e) =>
        new[] { e.localPosition.x, e.localPosition.y, e.localPosition.z, e.localEuler.x, e.localEuler.y, e.localEuler.z, e.localScale.x, e.localScale.y, e.localScale.z }
            .All(v => !float.IsNaN(v) && !float.IsInfinity(v)) && e.localScale.x > 0 && e.localScale.y > 0 && e.localScale.z > 0;
    public static EnemyHealth[] Compose(GameObject room, MonsterMapProfile profile, bool runtime)
    {
        var parent = room.transform.Find("Monsters");
        if (!parent) throw new InvalidOperationException("기존 Monsters 부모 누락");
        var originals = room.GetComponentsInChildren<EnemyHealth>(true);
        var used = new HashSet<EnemyHealth>(); var result = new List<EnemyHealth>();
        foreach (var e in profile.entries)
        {
            EnemyHealth hp = null;
            if (!string.IsNullOrEmpty(e.embeddedPath)) hp = room.transform.Find(e.embeddedPath)?.GetComponent<EnemyHealth>();
            if (hp && (used.Contains(hp) || Kind(hp) != Kind(e.template))) hp = null;
            if (!hp) hp = UnityEngine.Object.Instantiate(e.template, parent);
            used.Add(hp); result.Add(hp);
            hp.transform.localPosition = e.localPosition;
            hp.transform.localEulerAngles = e.localEuler;
            hp.transform.localScale = e.localScale;
        }
        foreach (var hp in originals)
            if (!used.Contains(hp))
            {
                hp.gameObject.SetActive(false);
                if (runtime) UnityEngine.Object.Destroy(hp.gameObject); else UnityEngine.Object.DestroyImmediate(hp.gameObject);
            }
        room.GetComponent<DungeonRoom>().Controller.ConfigureSpawnEnemies(result.ToArray());
        return result.ToArray();
    }
    public static void Apply(DungeonRoom room, MonsterMapProfile map, MonsterFloorProfile balance, int difficulty)
    {
        balance.ValidateMap(difficulty, map);
        var enemies = Compose(room.gameObject, map, true);
        for (int i = 0; i < enemies.Length; i++)
        {
            balance.Calculate(difficulty, map.entries[i].template, out int hp, out int attack);
            enemies[i].ConfigureSpawnHealth(hp);
            var stone = enemies[i].GetComponent<StoneMonsterAI>();
            if (stone) stone.ConfigureSpawnAttack(attack); else enemies[i].GetComponent<NormalMonsterAI>().ConfigureSpawnAttack(attack);
        }
    }
}
