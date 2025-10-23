using System.Collections.Generic;
using UnityEngine;
using CompanionAI;

/// <summary>
/// Manages health UI entries for all active companions in the scene.
/// </summary>
public class CompanionHealthUIManager : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Transform entriesParent;
    [SerializeField] private CompanionHealthUIEntry entryPrefab;
    [SerializeField] private bool clearExistingChildrenOnStart = true;

    private readonly Dictionary<CompanionCore, CompanionHealthUIEntry> _activeEntries = new Dictionary<CompanionCore, CompanionHealthUIEntry>();

    private Transform ParentTransform => entriesParent != null ? entriesParent : transform;

    private void Awake()
    {
        if (entryPrefab == null)
        {
            Debug.LogError("[CompanionHealthUIManager] Entry prefab reference missing. Assign the `CompanionHealthUIEntry` prefab in the inspector.");
        }
    }

    private void OnEnable()
    {
        CompanionCore.OnCompanionSpawned += HandleCompanionSpawned;
        CompanionCore.OnCompanionRemoved += HandleCompanionRemoved;

        if (clearExistingChildrenOnStart)
        {
            ClearExistingChildren();
        }

        InitializeExistingCompanions();
    }

    private void OnDisable()
    {
        CompanionCore.OnCompanionSpawned -= HandleCompanionSpawned;
        CompanionCore.OnCompanionRemoved -= HandleCompanionRemoved;
    }

    private void ClearExistingChildren()
    {
        if (ParentTransform == null)
        {
            return;
        }

        for (int i = ParentTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(ParentTransform.GetChild(i).gameObject);
        }

        _activeEntries.Clear();
    }

    private void InitializeExistingCompanions()
    {
        if (entryPrefab == null)
        {
            return;
        }

        foreach (var companion in CompanionCore.GetActiveCompanions())
        {
            if (companion == null || _activeEntries.ContainsKey(companion))
            {
                continue;
            }

            CreateEntryForCompanion(companion);
        }
    }

    private void HandleCompanionSpawned(CompanionCore companion)
    {
        if (entryPrefab == null || companion == null)
        {
            return;
        }

        if (_activeEntries.ContainsKey(companion))
        {
            return;
        }

        CreateEntryForCompanion(companion);
    }

    private void HandleCompanionRemoved(CompanionCore companion)
    {
        if (companion == null)
        {
            return;
        }

        if (_activeEntries.TryGetValue(companion, out CompanionHealthUIEntry entry) && entry != null)
        {
            Destroy(entry.gameObject);
        }

        _activeEntries.Remove(companion);
    }

    private void CreateEntryForCompanion(CompanionCore companion)
    {
        if (entryPrefab == null || ParentTransform == null)
        {
            Debug.LogWarning("[CompanionHealthUIManager] Cannot create entry - missing prefab or parent transform.");
            return;
        }

        CompanionHealthUIEntry entryInstance = Instantiate(entryPrefab, ParentTransform);
        entryInstance.Initialize(companion);
        _activeEntries[companion] = entryInstance;
    }
}
