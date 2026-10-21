#if UNITY_EDITOR
using UnityEngine;

// Preview scene metadata only; never attached to runtime monsters or saved into map prefabs.
[AddComponentMenu("")]
public sealed class MonsterProfileEntryMarker : MonoBehaviour
{
    public EnemyHealth template;
    public string entryId, embeddedPath;
}
#endif
