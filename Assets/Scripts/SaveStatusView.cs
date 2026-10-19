using TMPro;
using UnityEngine;

public sealed class SaveStatusView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    private void Update() { label.text = CharacterSaveService.Current != null ? CharacterSaveService.Current.Status : ""; }
}
