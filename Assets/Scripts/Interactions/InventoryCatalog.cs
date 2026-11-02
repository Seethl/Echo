using System;
using UnityEngine;

namespace Echo.Interactions
{
    [CreateAssetMenu(menuName="Echo/Inventory labels")]
    public sealed class InventoryCatalog : ScriptableObject
    {
        [Serializable] public sealed class Item
        {
            public string id,displayName;
            [TextArea] public string hint;
            public Sprite icon;
        }
        public Item[] items=Array.Empty<Item>();
    }
}
