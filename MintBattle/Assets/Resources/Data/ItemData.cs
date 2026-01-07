using UnityEngine;
public enum ItemType { Weapon, Consumable }

public abstract class ItemData : ScriptableObject
{
    public string Id;
    public ItemType itemType;
    public Sprite icon;
    public abstract void OnEquip(Hero hero);
    public abstract void OnUnequip(Hero hero);
    public abstract void OnUse(Hero hero);
}