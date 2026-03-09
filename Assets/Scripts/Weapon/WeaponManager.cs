using UnityEngine;

/// <summary>
/// 플레이어에 붙어 현재 무기를 관리합니다.
/// 세계 선택 후 RunManager가 EquipWeapon()을 호출해 무기를 교체합니다.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    public WeaponBase     CurrentWeapon        { get; private set; }
    public WeaponType     CurrentWeaponType    => CurrentWeapon?.WeaponType ?? WeaponType.Gun;
    public WeaponCategory CurrentWeaponCategory => CurrentWeapon?.Category  ?? WeaponCategory.Projectile;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>세계 선택 시 호출. 해당 무기를 활성화하고 나머지는 비활성화합니다.</summary>
    public void EquipWeapon(WeaponType type)
    {
        var weapons = GetComponents<WeaponBase>();
        WeaponBase target = null;

        foreach (var w in weapons)
        {
            bool match = w.WeaponType == type;
            w.enabled = match;
            if (match) target = w;
        }

        if (target == null)
        {
            target = type switch
            {
                WeaponType.Gun   => gameObject.AddComponent<GunWeapon>(),
                WeaponType.Sword => gameObject.AddComponent<MeleeWeapon>(),
                WeaponType.Staff => gameObject.AddComponent<StaffWeapon>(),
                WeaponType.Bow   => gameObject.AddComponent<GunWeapon>(),
                _                => gameObject.AddComponent<GunWeapon>(),
            };
            target.enabled = true;
        }

        CurrentWeapon = target;
        Debug.Log($"[WeaponManager] 무기 장착: {type} ({target.GetType().Name})");
    }

    /// <summary>현재 무기를 비활성화합니다 (UI 표시 중 등).</summary>
    public void DisableWeapon() { if (CurrentWeapon != null) CurrentWeapon.enabled = false; }

    /// <summary>현재 무기를 다시 활성화합니다.</summary>
    public void EnableWeapon()  { if (CurrentWeapon != null) CurrentWeapon.enabled = true;  }
}
