using System;
using UnityEngine;
public class Item
{
    public string tokenId;     
    public ItemData data;

    public string equippedByHeroId = null;
    public Item(string tokenId, ItemData data)
    {
        this.tokenId = tokenId;
        this.data = data;
        this.equippedByHeroId = null;
    }
    public bool CanEquip()
    {
        return string.IsNullOrEmpty(equippedByHeroId);
    }
}