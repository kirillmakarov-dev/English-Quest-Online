using UnityEngine;

public class NameTagPositioner : MonoBehaviour
{
    [SerializeField] private PlayerFormSwitcher _formSwitcher;
    [SerializeField] private Transform _nameTagRoot;

    private void OnEnable()
    {
        _formSwitcher.OnFormChanged += OnFormChanged;
    }

    private void OnDisable()
    {
        _formSwitcher.OnFormChanged -= OnFormChanged;
    }

    private void OnFormChanged(AnimalFormDefinitionSO form)
    {
        Vector3 pos = _nameTagRoot.localPosition;
        pos.y = form.nameTagHeightOffset;
        _nameTagRoot.localPosition = pos;
    }
}
