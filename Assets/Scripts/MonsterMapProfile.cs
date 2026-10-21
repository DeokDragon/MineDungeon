using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mine Dungeon/Monster Map Profile")]
public sealed class MonsterMapProfile : ScriptableObject
{
    public DungeonRoom map;
    public bool configured;
    [Serializable] public sealed class Entry
    {
        public string id;
        public EnemyHealth template;
        public string embeddedPath;
        public Vector3 localPosition, localEuler, localScale = Vector3.one;
        // Reserved, separate from species adjustments. Nonzero values are rejected until specified.
        [HideInInspector] public int reservedHpAdjustment, reservedAttackAdjustment;
    }
    public Entry[] entries = Array.Empty<Entry>();
}
