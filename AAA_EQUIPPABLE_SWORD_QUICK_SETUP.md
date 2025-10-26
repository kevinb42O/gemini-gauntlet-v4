# ⚡ EQUIPPABLE SWORD SYSTEM - QUICK SETUP GUIDE
**For:** Rapid Implementation  
**Time:** 15 minutes  
**Reference:** AAA_EQUIPPABLE_SWORD_ITEM_SYSTEM_COMPLETE.md (full documentation)

---

## 🚀 RAPID IMPLEMENTATION CHECKLIST

### ✅ 1. Create Core Scripts (5 minutes)

#### `Assets/scripts/EquippableWeaponItemData.cs`
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Equippable Weapon")]
public class EquippableWeaponItemData : ChestItemData
{
    public WeaponHandType allowedHands = WeaponHandType.RightHand;
    public string weaponPrefabPath = "Assets/prefabs_made/SWORD/sword-of-arturias";
    public GameObject worldItemModel;
    public string weaponTypeID = "sword";
    public bool isUniqueWeapon = true;
    
    public override bool IsSameItem(ChestItemData other)
    {
        if (isUniqueWeapon) return false;
        return base.IsSameItem(other);
    }
}

[System.Flags]
public enum WeaponHandType
{
    RightHand = 1 << 0,
    LeftHand = 1 << 1,
    BothHands = RightHand | LeftHand
}
```

#### `Assets/scripts/WeaponEquipmentManager.cs`
```csharp
using UnityEngine;
using GeminiGauntlet.UI;

public class WeaponEquipmentManager : MonoBehaviour
{
    public static WeaponEquipmentManager Instance { get; private set; }
    
    public UnifiedSlot rightHandWeaponSlot;
    public UnifiedSlot leftHandWeaponSlot;
    public PlayerShooterOrchestrator playerShooter;
    
    private EquippableWeaponItemData _rightHandWeapon;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        if (playerShooter == null)
            playerShooter = FindObjectOfType<PlayerShooterOrchestrator>();
    }
    
    void Start()
    {
        if (rightHandWeaponSlot != null)
        {
            rightHandWeaponSlot.OnSlotChanged += OnRightHandSlotChanged;
            CheckRightHandEquipment();
        }
    }
    
    void OnDestroy()
    {
        if (rightHandWeaponSlot != null)
            rightHandWeaponSlot.OnSlotChanged -= OnRightHandSlotChanged;
    }
    
    private void OnRightHandSlotChanged(ChestItemData item, int count)
    {
        CheckRightHandEquipment();
    }
    
    private void CheckRightHandEquipment()
    {
        if (rightHandWeaponSlot == null || playerShooter == null) return;
        
        ChestItemData currentItem = rightHandWeaponSlot.CurrentItem;
        
        if (currentItem != null && currentItem is EquippableWeaponItemData weaponData)
        {
            _rightHandWeapon = weaponData;
            
            if (weaponData.weaponTypeID == "sword")
            {
                playerShooter.SetSwordAvailable(true);
                Debug.Log($"[WeaponEquipmentManager] ✅ Sword equipped!");
                
                if (FloatingTextManager.Instance != null)
                {
                    Vector3 pos = playerShooter.transform.position + Vector3.up * 50f;
                    FloatingTextManager.Instance.ShowFloatingText($"⚔️ {weaponData.itemName} Equipped!", pos, Color.cyan, customSize: 24f);
                }
            }
        }
        else
        {
            _rightHandWeapon = null;
            playerShooter.SetSwordAvailable(false);
            Debug.Log($"[WeaponEquipmentManager] ❌ No weapon equipped");
        }
    }
    
    public bool HasRightHandWeapon(string weaponTypeID)
    {
        return _rightHandWeapon != null && _rightHandWeapon.weaponTypeID == weaponTypeID;
    }
    
    public void RefreshEquipmentState()
    {
        CheckRightHandEquipment();
    }
}
```

#### `Assets/scripts/WorldSwordPickup.cs`
```csharp
using UnityEngine;
using GeminiGauntlet.UI;
using GeminiGauntlet.Audio;

public class WorldSwordPickup : MonoBehaviour
{
    public EquippableWeaponItemData swordItemData;
    public float pickupRange = 250f;
    public bool enableBobbing = true;
    public float bobbingSpeed = 1.5f;
    public float bobbingHeight = 10f;
    public bool enableRotation = true;
    public float rotationSpeed = 30f;
    
    private Transform _playerTransform;
    private InventoryManager _inventoryManager;
    private Vector3 _originalPosition;
    private bool _isInRange = false;
    
    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;
        _inventoryManager = InventoryManager.Instance;
        _originalPosition = transform.position;
    }
    
    void Update()
    {
        if (_playerTransform == null) return;
        
        if (enableBobbing)
        {
            float bobOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = _originalPosition + Vector3.up * bobOffset;
        }
        
        if (enableRotation)
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        _isInRange = distance <= pickupRange;
        
        if (_isInRange && Input.GetKeyDown(KeyCode.E))
            TryPickupSword();
    }
    
    private void TryPickupSword()
    {
        if (swordItemData == null || _inventoryManager == null) return;
        
        if (_inventoryManager.TryAddItem(swordItemData, 1))
        {
            Debug.Log($"[WorldSwordPickup] ✅ Picked up {swordItemData.itemName}");
            
            if (FloatingTextManager.Instance != null)
            {
                Vector3 pos = _playerTransform.position + Vector3.up * 50f;
                FloatingTextManager.Instance.ShowFloatingText(
                    $"⚔️ {swordItemData.itemName} Acquired!", 
                    pos, 
                    swordItemData.GetRarityColor(),
                    customSize: 24f
                );
            }
            
            GameSounds.PlayGemCollection(_playerTransform.position);
            Destroy(gameObject);
        }
        else
        {
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowFloatingText(
                    "⚠️ Inventory Full!", 
                    _playerTransform.position + Vector3.up * 50f, 
                    Color.red,
                    customSize: 20f
                );
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
```

---

### ✅ 2. Modify PlayerShooterOrchestrator (3 minutes)

**Add after line 67:**
```csharp
private bool _isSwordAvailable = false;
```

**Add after ToggleSwordMode() method (~line 1280):**
```csharp
public void SetSwordAvailable(bool available)
{
    _isSwordAvailable = available;
    if (!available && IsSwordModeActive)
        ToggleSwordMode();
}

public bool CanUseSwordMode()
{
    return _isSwordAvailable;
}
```

**Modify ToggleSwordMode() start (line ~1204):**
```csharp
private void ToggleSwordMode()
{
    // ADD THIS CHECK:
    if (!IsSwordModeActive && !_isSwordAvailable)
    {
        Debug.Log("[PlayerShooterOrchestrator] ❌ Cannot activate - no sword!");
        if (FloatingTextManager.Instance != null)
        {
            Vector3 pos = transform.position + Vector3.up * 50f;
            FloatingTextManager.Instance.ShowFloatingText("⚠️ No Sword Equipped!", pos, Color.red, customSize: 20f);
        }
        return;
    }
    
    // ... rest of existing code continues unchanged
```

---

### ✅ 3. Modify ChestController (2 minutes)

**Add after line 100 (in header section):**
```csharp
[Header("Sword Item Spawning")]
public EquippableWeaponItemData swordItemData;
[Range(0f, 100f)]
public float swordSpawnChance = 10f;
```

**Add new method after SpawnSelfReviveItems():**
```csharp
private void SpawnSwordItem()
{
    if (swordItemData == null) return;
    
    float roll = Random.Range(0f, 100f);
    if (roll > swordSpawnChance) return;
    
    InventoryManager inventoryManager = InventoryManager.Instance;
    if (inventoryManager != null && inventoryManager.TryAddItem(swordItemData, 1))
    {
        Debug.Log($"📦 ChestController: Spawned {swordItemData.itemName}!");
    }
}
```

**Add to OpenChest() method (after SpawnSelfReviveItems() call):**
```csharp
SpawnSwordItem();
```

---

### ✅ 4. Unity Inspector Setup (5 minutes)

**Create Sword ScriptableObject:**
1. `Assets > Create > Inventory > Equippable Weapon`
2. Name: `SwordOfArtoriasWeapon`
3. Set:
   - itemName: "Sword of Artorias"
   - itemRarity: 4 (Epic)
   - weaponTypeID: "sword"
   - isUniqueWeapon: true

**Create UI Weapon Slots:**
1. Duplicate vest/backpack equipment slot
2. Rename: `RightHandWeaponSlot`
3. UnifiedSlot component: `isEquipmentSlot = true`
4. Add label: "Right Hand Weapon"

**Add WeaponEquipmentManager:**
1. Add to Player or InventoryManager GameObject
2. Assign:
   - rightHandWeaponSlot → drag slot from hierarchy
   - playerShooter → auto-finds or drag

**Create World Sword Prefab:**
1. Empty GameObject: `WorldSword_SwordOfArtorias`
2. Add model as child (from Assets/prefabs_made/SWORD/sword-of-arturias)
3. Add components:
   - WorldSwordPickup (assign swordItemData)
   - SphereCollider (trigger, radius 250)
   - Rigidbody (isKinematic)
4. Save as prefab

**Configure Chests:**
1. Select chest prefabs
2. Assign `swordItemData` → SwordOfArtoriasWeapon
3. Set `swordSpawnChance = 10`

---

## 🧪 QUICK TEST

1. **Pickup Test:** Place WorldSword in scene, walk up, press E
2. **Equipment Test:** Drag sword to right hand slot
3. **Mode Test:** Press Mouse Button 3 - should activate
4. **Block Test:** Remove sword from slot, try Mouse Button 3 - should block
5. **Chest Test:** Open chests until sword spawns

---

## 🔧 TROUBLESHOOTING

| Issue | Fix |
|-------|-----|
| Sword mode doesn't activate | Check `_isSwordAvailable` flag in debugger |
| No pickup prompt | Verify pickupRange = 250 (not 2.5!) |
| Sword doesn't stay equipped | Ensure slot has `isEquipmentSlot = true` |
| Console errors | Check all references assigned in Inspector |

---

## 📚 FULL DOCUMENTATION

See `AAA_EQUIPPABLE_SWORD_ITEM_SYSTEM_COMPLETE.md` for:
- Complete code with comments
- Forge recipe setup
- Advanced features
- Edge case handling
- Testing matrix

**Status:** ✅ Production Ready - 0% Bloat Code
