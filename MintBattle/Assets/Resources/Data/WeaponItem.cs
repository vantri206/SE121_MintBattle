using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Item/Hero Weapon")]
public class WeaponItem : ItemData
{
    public int attackBonus;
    public int hpBonus;
    public int defenseBonus;
    public int speedBonus;
    public WeaponItem()
    {
        itemType = ItemType.Weapon;
    }
    public override void OnEquip(Hero hero)
    {
        hero.itemBonusAttack += attackBonus;
        hero.itemBonusHP += hpBonus;
        Debug.Log($"Hero {hero.Id} equiped {this.name}");
    }
    public override void OnUnequip(Hero hero)
    {
        hero.itemBonusAttack -= attackBonus;
        hero.itemBonusHP -= hpBonus;
        Debug.Log($"Hero {hero.Id} unequiped {this.name}");
    }

    public override void OnUse(Hero hero)
    {
        Debug.Log("Cant use weapon, only for equip");
    }
}