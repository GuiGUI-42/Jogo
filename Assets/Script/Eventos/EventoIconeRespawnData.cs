using UnityEngine;

// Armazena informações para um futuro respawn do ícone no mesmo local
public class EventoIconeRespawnData : MonoBehaviour
{
    [Tooltip("Prefab do ícone para respawn posterior")] public GameObject respawnPrefab;
    [Tooltip("Parent a ser usado no respawn")] public Transform parent;
    [Tooltip("Posição em mundo para respawn")] public Vector3 worldPosition;
    [Tooltip("Rotação em mundo para respawn")] public Quaternion worldRotation = Quaternion.identity;
}
