using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using FMODUnity;

public class Inventory : MonoBehaviour
{
    #region Singleton
    public static Inventory instance;
    #endregion
    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;
    public int space = 18;  // Amount of item spaces
    // Our current list of items in the inventory
    public List<Item> items;
    public List<Item> Ranged_hotbar;
    public List<Item> Consumable_hot_bar;
    public int cash;
    public GameObject cash_ui;
    public Item quick_access_consume;
    public Sprite default_health_icon;
    public Sprite default_ranged_icon;

    public StudioEventEmitter pick_up_item;

    public StudioEventEmitter consume_health_item;

    // Add a new item if enough room

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        if (instance != null)
        {
            //Destroy(gameObject);
            return;
        }
        
    }

    public void reload_inventory()
    {
        List<Item> items_to_collect = new List<Item>();
        items.Clear();
        items_to_collect.Clear();
        items_to_collect = SaveData.load_items();
        foreach (Item item_ref in items_to_collect)
        {
            if (items.Contains(item_ref) != true)
            {
                items.Add(item_ref);
            }
        }
        cash = SaveData.load_money();
        if (quick_access_consume != null)
            GUI_control.instance.dpad_health_item.sprite = quick_access_consume.icon;
        //gameObject.GetComponent<lily_load_save>().fetch();
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void destroy_gun_col_list()
    {
        on_guns_app_open.instance.destroy_list();
        on_guns_app_open.instance.pop_up.SetActive(true);
    }

    public void load_ranged_hotbar()
    {
        Ranged_hotbar.Clear();
        Ranged_hotbar.Add(lily_charge_attack.instance.default_ranged_weapon);
        foreach (Item itm in items)
        {
            if (itm.my_type == Item.ItemType.Ranged_weapon || itm.my_type == Item.ItemType.Throwable)
            {
                Ranged_hotbar.Add(itm);
            }
        }
    }

    public bool use_useable_item(Item item_to_use)
    {
        if (item_to_use.my_type == Item.ItemType.Usable || item_to_use.my_type == Item.ItemType.Quest_Item)
        {
            if (items.Contains(item_to_use) == true)
            {
                if (item_to_use.amount > 1)
                {
                    item_to_use.amount = item_to_use.amount - 1;
                }
                else
                {
                    items.Remove(item_to_use);
                    item_to_use.amount = 0;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public void Add_cash(int amount)
    {
        cash = cash + amount;
        GameObject ui_ele = Instantiate(cash_ui, GUI_control.instance.chat_box);
        ui_ele.GetComponent<TextMeshProUGUI>().text = "+ $" + amount;
        ui_ele.GetComponent<TextMeshProUGUI>().color = Color.green;
    }

    public void Add_field(string field_text)
    {
        GameObject ui_ele = Instantiate(cash_ui, GUI_control.instance.chat_box);
        ui_ele.GetComponent<TextMeshProUGUI>().text = field_text;
        ui_ele.GetComponent<TextMeshProUGUI>().color = Color.white;
    }

    public bool Add(Item iteme)
    {
        if (items.Contains(iteme))
        {
            int itemstack = items.IndexOf(iteme);
            items[itemstack].amount += iteme.pick_up_amount;
            pick_up_item_sound();
            return true;
        }
        else
        {
            if (items.Count >= space)
            {
                Debug.Log("not enough room");
                GameObject ui_ele = Instantiate(cash_ui, GUI_control.instance.chat_box);
                ui_ele.GetComponent<TextMeshProUGUI>().text = "not enough room for " + iteme.name_of_item;
                return false;
            }
            else
            {
                iteme.amount = iteme.pick_up_amount;
                items.Add(iteme);
                pick_up_item_sound();
                return true;
            }
        }
    }
    public void Remove(Item item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
        }
        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();
    }

    public void drop(Item itm, int number)
    {
        if (items.Contains(itm))
        {
            if (itm.amount > number)
            {
                itm.amount = itm.amount - number;
            }
            else
            {
                items.Remove(itm);
            }
        }
        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();
    }

    public void consume_item(Item item, int amount)
    {
        if (items.Contains(item))
        {
            if (item.amount > amount)
            {
                item.amount = item.amount - amount;
            }
            else
            {
                items.Remove(item);
            }
        }
        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();
    }

    public void run_out(Item item)
    {
        Remove(item);
        if (item == quick_access_consume)
        {
            GUI_control.instance.dpad_health_item.sprite = default_health_icon;
        }
    }

    public void use(Item item)
    {
        int index = items.IndexOf(item);
        if (items[index].my_type == Item.ItemType.Ranged_weapon || items[index].my_type == Item.ItemType.Throwable)
        {
            lily_charge_attack.instance.update_ranged(items[index]);
        }
        if (items[index].my_type == Item.ItemType.Health || items[index].my_type == Item.ItemType.Buff)
        {
            eat_health_item_sound();
            lily_charge_attack.instance.gameObject.GetComponent<lily_item_buff_engine>().use_item(items[index]);
            if (items[index].amount != 1)
            {
                items[index].amount = items[index].amount - 1;
            }
            else
            {
                run_out(item);
            }
        }
    }

    public void assign_quick_use(Item thing)
    {
        quick_access_consume = thing;
        GUI_control.instance.dpad_health_item.sprite = thing.icon;
    }

    public void up_dpad()
    {
        use(quick_access_consume);
    }

    public void eat_health_item_sound()
    {
        consume_health_item.Play();
    }

    public void pick_up_item_sound()
    {
        pick_up_item.Play();
    }

}