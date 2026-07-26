using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.GameplayAnimals + "/Form Definition", fileName = "FormDefinition")]
public class AnimalFormDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public int formId = 0;
    public string displayName = "New Form";

    [Header("Visuals")]
    public GameObject rigPrefab;

    [Header("Movement")]
    public MovementStrategySO movementStrategy;

    [Header("Base Stats")]
    public float moveSpeed = 5f;
    public float jumpImpulse = 6f;

    [Header("UI")]
    public float nameTagHeightOffset = 2.5f;

    [Header("Audio")]
    public AudioClip switchSound;
}
