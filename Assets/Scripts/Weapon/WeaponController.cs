using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Socket")]
    public Transform weaponSocket;

    [Header("Weapon Pool")]
    public Transform weaponPoolRoot;

    private WeaponData currentWeaponData;

    private GameObject currentWeapon;

    private Dictionary<WeaponData, GameObject> weaponPool
        = new Dictionary<WeaponData, GameObject>();

    private static WeaponController _instance;
    public static WeaponController Instance
    {
        get {
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    private void Awake()
    {
        Instance = this;
        weaponSocket = transform;
        weaponPoolRoot = GameObject.Find("WeaponPool").transform;
    }

    public void EquipWeapon(WeaponData weaponData)
    {
        // 已经装备的是同一把武器
        if (currentWeaponData == weaponData)
        {
            return;
        }

        // 先卸下当前武器
        UnequipCurrentWeapon();

        // 从对象池获取武器
        GameObject weapon = GetWeaponFromPool(weaponData);

        // 挂到手上的 WeaponSocket
        weapon.transform.SetParent(weaponSocket);

        // 设置本地位置
        weapon.transform.localPosition = Vector3.zero;

        // 设置本地旋转
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.localScale = Vector3.one;

        // 启用
        weapon.SetActive(true);

        // 更新当前武器
        currentWeapon = weapon;
        currentWeaponData = weaponData;
    }


    private GameObject GetWeaponFromPool(WeaponData weaponData)
    {
        // 已经创建过
        if (weaponPool.TryGetValue(weaponData, out GameObject weapon))
        {
            return weapon;
        }

        // 第一次使用，创建
        weapon = Instantiate(
            weaponData.weaponPrefab,
            weaponPoolRoot
        );

        weapon.SetActive(false);

        // 加入对象池
        weaponPool.Add(
            weaponData,
            weapon
        );

        return weapon;
    }


    private void UnequipCurrentWeapon()
    {
        if (currentWeapon == null)
        {
            return;
        }

        // 关闭武器
        currentWeapon.SetActive(false);

        // 放回对象池
        currentWeapon.transform.SetParent(
            weaponPoolRoot
        );

        currentWeaponData = null;

        currentWeapon = null;
    }
}