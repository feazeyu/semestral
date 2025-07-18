using RPGFramework.Inventory;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RPGFramework.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField]
        public bool IsEnabled;
        protected virtual Color BaseColor => Color.white;
        public string EditorUILabel = "";
        [SerializeField]
        private GameObject _item;
        public GameObject Item 
        { 
            get {
                return _item;
            } private set 
            {
                _item = value;
            } 
        }

#if UNITY_EDITOR
        //These methods are here only for use in the editor, to be able to force items into slots they don't belong.
        //And slots like the locked ones, which cannot be edited by the player.
        public virtual bool EditorOnlyPutItem(GameObject item)
        {
            if (item == null) return false;
            EditorUILabel = "I";
            Item = item;
            return true;
        }
        public virtual bool EditorOnlyRemoveItem()
        {
            EditorUILabel = "";
            Item = null;
            return true;
        }
#endif
        public virtual bool PutItem(GameObject item) { 
            if (Item != null || !IsEnabled) return false;
            EditorUILabel = "I";
            Item = item;
            return true;
        }
        public virtual bool RemoveItem()
        {
            if (Item == null) return false;
            EditorUILabel = "";
            Item = null;
            return true;
        }

        public Color color
        {
            get
            {
                return IsEnabled ? BaseColor : BaseColor * 0.5f;
            }
        }
    }
    [Serializable]
    public class LockedInventorySlot : InventorySlot
    {
        protected override Color BaseColor => Color.red;
        public override bool PutItem(GameObject item)
        {
            return false;  
        }
        public override bool RemoveItem()
        {
            return false;
        }
    }
    
    [Serializable]
    public static class InventorySlotUtils
    {
        public static string[] GetSlotTypeNames()
        {
            return Assembly.GetAssembly(typeof(InventorySlot))
                .GetTypes()
                .Where(t => (t.IsSubclassOf(typeof(InventorySlot)) || t.IsAssignableFrom(typeof(InventorySlot))) && !t.IsAbstract)
                .Select(t => t.Name)
                .ToArray();
        }
    }
}
