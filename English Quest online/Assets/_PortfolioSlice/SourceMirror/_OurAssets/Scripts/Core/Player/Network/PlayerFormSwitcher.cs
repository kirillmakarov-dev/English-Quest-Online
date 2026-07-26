using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class PlayerFormSwitcher : NetworkBehaviour
{
    [Header("Setup")]
    public Transform visualRoot;
    public List<AnimalFormDefinitionSO> forms = new List<AnimalFormDefinitionSO>();
    public int defaultFormId = 0;
    public bool enableInput = true;

    [Networked] public int NetworkedFormIndex { get; set; }

    private ChangeDetector _changes;
    private int _appliedFormIndex = -1;

    public AnimalFormDefinitionSO CurrentForm { get; private set; }
    public Animator CurrentAnimator { get; private set; }
    public GameObject CurrentRigInstance { get; private set; }
    public bool AreVisualsVisible { get; private set; } = true;

    /// <summary>
    /// Assign the name tag root GameObject here. It lives outside <see cref="visualRoot"/>
    /// so it must be toggled separately when visuals are hidden (e.g. while riding the bike).
    /// </summary>
    [SerializeField] private GameObject _nameTagRoot;
    [SerializeField] private AudioSource _switchAudioSource;

    public event Action<AnimalFormDefinitionSO> OnFormChanged;
    public GameObject formEffectPrefab;

    private bool _initialized;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasStateAuthority)
        {
            int initialIndex = 0;
            AnimalFormDefinitionSO defaultForm = forms.Find(f => f != null && f.formId == defaultFormId);
            if (defaultForm != null)
                initialIndex = GetFormIndex(defaultForm);

            NetworkedFormIndex = initialIndex;
        }

        ApplyFormVisuals(NetworkedFormIndex);
        _appliedFormIndex = NetworkedFormIndex;
        _initialized = true;
    }

    public override void Render()
    {
        foreach (string change in _changes.DetectChanges(this))
        {
            if (change == nameof(NetworkedFormIndex))
                ApplyFormVisuals(NetworkedFormIndex);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        DestroyCurrentRig();
    }

    private void Update()
    {
        if (!enableInput || forms.Count == 0 || !Object.HasStateAuthority)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1) && forms.Count >= 1)
            NetworkedFormIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2) && forms.Count >= 2)
            NetworkedFormIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3) && forms.Count >= 3)
            NetworkedFormIndex = 2;
        else if (Input.GetKeyDown(KeyCode.T) && forms.Count >= 2)
            NetworkedFormIndex = NetworkedFormIndex == 0 ? 1 : 0;
    }

    private void ApplyFormVisuals(int index)
    {
        if (index < 0 || index >= forms.Count) return;
        if (index == _appliedFormIndex && CurrentRigInstance != null) return;

        AnimalFormDefinitionSO form = forms[index];
        if (form == null) return;

        if (formEffectPrefab != null && AreVisualsVisible)
        {
            GameObject effect = Instantiate(formEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform);
            Destroy(effect, 2f);
        }

        SwitchToVisuals(form);

        if (_initialized && _switchAudioSource != null && form.switchSound != null)
            _switchAudioSource.PlayOneShot(form.switchSound);

        _appliedFormIndex = index;
    }

    private void SwitchToVisuals(AnimalFormDefinitionSO form)
    {
        if (form == null || form.rigPrefab == null)
            return;

        DestroyCurrentRig();

        Transform parent = visualRoot != null ? visualRoot : transform;
        CurrentRigInstance = Instantiate(form.rigPrefab, parent);
        CurrentRigInstance.transform.localPosition = Vector3.zero;
        CurrentRigInstance.transform.localRotation = Quaternion.identity;

        DisableOrphanedNetworkComponents(CurrentRigInstance);

        CurrentAnimator = CurrentRigInstance.GetComponentInChildren<Animator>(true);
        CurrentForm = form;
        ApplyRigVisibility();
        OnFormChanged?.Invoke(form);
    }

    /// <summary>
    /// Rig prefabs may carry Fusion components for standalone use. When instantiated
    /// as a local visual child, those components must be disabled so they do not conflict
    /// with the player NetworkObject or leave proxies without a replicated rig.
    /// </summary>
    private static void DisableOrphanedNetworkComponents(GameObject rigInstance)
    {
        foreach (NetworkObject networkObject in rigInstance.GetComponentsInChildren<NetworkObject>(true))
            networkObject.enabled = false;

        foreach (NetworkBehaviour behaviour in rigInstance.GetComponentsInChildren<NetworkBehaviour>(true))
            behaviour.enabled = false;
    }

    private void DestroyCurrentRig()
    {
        if (CurrentRigInstance == null) return;

        Destroy(CurrentRigInstance);
        CurrentRigInstance = null;
        CurrentAnimator = null;
        CurrentForm = null;
    }

    public bool SwitchTo(int formId)
    {
        if (!Object.HasStateAuthority) return false;

        AnimalFormDefinitionSO form = forms.Find(f => f != null && f.formId == formId);
        if (form == null) return false;

        int index = GetFormIndex(form);
        if (index == -1) return false;

        NetworkedFormIndex = index;
        return true;
    }

    public void SwitchTo(AnimalFormDefinitionSO form)
    {
        if (!Object.HasStateAuthority) return;

        int index = GetFormIndex(form);
        if (index != -1)
            NetworkedFormIndex = index;
    }

    private int GetFormIndex(AnimalFormDefinitionSO form)
    {
        if (form == null) return -1;

        for (int i = 0; i < forms.Count; i++)
        {
            if (forms[i] == form)
                return i;
        }

        return -1;
    }

    public void SetVisualsVisible(bool visible)
    {
        AreVisualsVisible = visible;
        ApplyRigVisibility();
    }

    public AnimalFormDefinitionSO GetCurrentFormDefinition()
    {
        if (CurrentForm != null)
            return CurrentForm;

        if (NetworkedFormIndex < 0 || NetworkedFormIndex >= forms.Count)
            return null;

        return forms[NetworkedFormIndex];
    }

    private void ApplyRigVisibility()
    {
        if (CurrentRigInstance != null)
            CurrentRigInstance.SetActive(AreVisualsVisible);

        if (_nameTagRoot != null)
            _nameTagRoot.SetActive(AreVisualsVisible);
    }
}
