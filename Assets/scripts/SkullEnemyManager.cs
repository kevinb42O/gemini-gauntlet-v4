using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class SkullEnemyManager : MonoBehaviour
{
    public enum SkullLOD { Near, Mid, Far }

    private static SkullEnemyManager _instance;
    private static readonly List<SkullEnemy> _skulls = new List<SkullEnemy>();
    private static readonly Dictionary<SkullEnemy, SkullLOD> _lastLods = new Dictionary<SkullEnemy, SkullLOD>();
    private static int _cursor = 0;

    [Header("LOD Distances (meters)")]
    [Tooltip("<= Near distance => Near LOD")]
    public float nearDistance = 25f;
    [Tooltip("> Near and <= Mid distance => Mid LOD; > Mid => Far LOD")] 
    public float midDistance = 60f;
    [Tooltip("Hysteresis (meters) to avoid rapid LOD flipping")]
    [Range(0f, 20f)] public float hysteresis = 5f;

    [Header("Frame Budgeting")]
    [Tooltip("How many skulls to LOD-check per frame")] 
    [Range(1, 512)] public int checksPerFrame = 64;

    [Header("Player Lookup")]
    [Tooltip("How often to refresh the player Transform if missing (seconds)")]
    public float playerRefreshInterval = 1.5f;

    private Transform _player;
    private float _playerRefreshTimer;

    // Ensure the manager exists when first used
    private static void EnsureInstance()
    {
        if (_instance != null) return;
        _instance = FindObjectOfType<SkullEnemyManager>();
        if (_instance == null)
        {
            var go = new GameObject("SkullEnemyManager");
            _instance = go.AddComponent<SkullEnemyManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        _skulls.Clear();
        _lastLods.Clear();
        _cursor = 0;
    }

    public static void Register(SkullEnemy skull)
    {
        EnsureInstance();
        if (skull == null) return;
        if (!_skulls.Contains(skull))
        {
            _skulls.Add(skull);
            _lastLods[skull] = SkullLOD.Far; // default until first classification
            _instance.ClassifyImmediate(skull);
        }
    }

    public static void Unregister(SkullEnemy skull)
    {
        if (_instance == null || skull == null) return;
        int idx = _skulls.IndexOf(skull);
        if (idx >= 0)
        {
            _skulls.RemoveAt(idx);
            if (_cursor > idx) _cursor--;
            if (_cursor >= _skulls.Count) _cursor = 0;
        }
        _lastLods.Remove(skull);
    }

    private void Update()
    {
        RefreshPlayerIfNeeded();
        if (_player == null || _skulls.Count == 0) return;

        int budget = Mathf.Min(checksPerFrame, _skulls.Count);
        float nearEnter = Mathf.Max(0f, nearDistance - hysteresis);
        float nearExit = nearDistance + hysteresis;
        float midEnter = Mathf.Max(nearExit, midDistance - hysteresis);
        float midExit = midDistance + hysteresis;

        float nearEnterSqr = nearEnter * nearEnter;
        float nearExitSqr = nearExit * nearExit;
        float midEnterSqr = midEnter * midEnter;
        float midExitSqr = midExit * midExit;

        for (int i = 0; i < budget; i++)
        {
            if (_skulls.Count == 0) break; // safety

            if (_cursor >= _skulls.Count) _cursor = 0;
            var skull = _skulls[_cursor];
            _cursor++;

            if (skull == null)
            {
                _skulls.RemoveAt(_cursor - 1);
                _cursor--;
                continue;
            }

            Vector3 d = skull.transform.position - _player.position;
            float sqrDist = d.sqrMagnitude;
            SkullLOD prev = _lastLods.TryGetValue(skull, out var p) ? p : SkullLOD.Far;
            SkullLOD next = DecideLodWithHysteresis(sqrDist, prev, nearEnterSqr, nearExitSqr, midEnterSqr, midExitSqr);
            if (next != prev)
            {
                _lastLods[skull] = next;
                skull.ApplyLOD(next);
            }
        }
    }

    private void RefreshPlayerIfNeeded()
    {
        _playerRefreshTimer -= Time.unscaledDeltaTime;
        if (_player != null && _player.gameObject != null) return;

        if (_playerRefreshTimer > 0f) return;
        _playerRefreshTimer = Mathf.Max(0.25f, playerRefreshInterval);

        // Try by tag first
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players != null && players.Length > 0)
        {
            _player = players[0].transform;
            return;
        }

        // Fallback: any PlayerHealth in scene
        var ph = FindObjectOfType<PlayerHealth>();
        if (ph != null) _player = ph.transform;
    }

    private void ClassifyImmediate(SkullEnemy skull)
    {
        if (skull == null) return;
        RefreshPlayerIfNeeded();
        SkullLOD lod = SkullLOD.Far;
        if (_player != null)
        {
            float dist = Vector3.Distance(skull.transform.position, _player.position);
            if (dist <= nearDistance) lod = SkullLOD.Near;
            else if (dist <= midDistance) lod = SkullLOD.Mid;
            else lod = SkullLOD.Far;
        }
        _lastLods[skull] = lod;
        skull.ApplyLOD(lod);
    }

    private static SkullLOD DecideLodWithHysteresis(
        float sqrDist,
        SkullLOD prev,
        float nearEnterSqr,
        float nearExitSqr,
        float midEnterSqr,
        float midExitSqr)
    {
        switch (prev)
        {
            case SkullLOD.Near:
                if (sqrDist > midExitSqr) return SkullLOD.Far;
                if (sqrDist > nearExitSqr) return SkullLOD.Mid;
                return SkullLOD.Near;

            case SkullLOD.Mid:
                if (sqrDist < nearEnterSqr) return SkullLOD.Near;
                if (sqrDist > midExitSqr) return SkullLOD.Far;
                return SkullLOD.Mid;

            case SkullLOD.Far:
                if (sqrDist < nearEnterSqr) return SkullLOD.Near;
                if (sqrDist < midEnterSqr) return SkullLOD.Mid;
                return SkullLOD.Far;
        }
        return SkullLOD.Mid;
    }
}
