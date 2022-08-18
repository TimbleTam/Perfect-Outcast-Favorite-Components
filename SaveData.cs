using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

public class SaveData : MonoBehaviour
{

    #region Singleton
    public static SaveData save;

    void Awake()
    {
        if (save == null)
        {
            save = this;
            DontDestroyOnLoad(this);
        }
        if (save != null)
        {
            //Destroy(gameObject);
            return;
        }
        GameObject player = FindPlayer.find_player();

        Cursor.lockState = CursorLockMode.Confined;
        //player.GetComponent<lily_movement_new>().load_saved_bindings();
    }

    #endregion
    [Header("this script handles save data, saving and loading")]

    [Header("vars intended to be accessable by other objects in game")]
    public List<SceneIndexs> scenes;
    public int total_kills;

    [Header("required processing vars for translating file format from binary information to the intended variable format")]
    public List<Note> note_order_array;
    public List<Sword> sword_order_array;
    public List<missionlist> mission_order_array;
    public List<level_hint> level_hint_order_array;
    public List<Item> inventory_order_array;
    public Sword donor_sword;
    public Note donor_note;
    public missionlist donor_mission;
    public level_hint donor_hint;
    public AI_Controller[] enemy_list;
    public List<string> ene_save_list;
    public List<string> ene_comp_list;
    public vending_machine[] vender_list;

    //public Difficulty_Settings difficulty;

    public GameObject load_screen_spawner;
    public save_point[] save_Points;

    [SerializeField]
    public static bool show_quest;

    public List<int> scene_ints;

    private StreamWriter level_writer;

    void Start()
    {
        get_diff_settings();
        load_volume_settings();
    }

    #region Grab File

    public static ProgressData Load_Progress()
    {
        string path = Application.persistentDataPath + "/playerInfo" + ".lily";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            ProgressData data = formatter.Deserialize(stream) as ProgressData;
            stream.Close();

            Debug.Log("save file was accessed inside " + path);
            return data;
        }
        else
        {
            Debug.LogError("save file was not found inside " + path);
            return null;
        }
    }

    #endregion

    #region Functions

    public void save_diff_settings(float health_multi, float enemy_dam, float enemy_cool_down, float detect_time)
    {
        BinaryFormatter bf = new BinaryFormatter();
        string diff_path = Application.persistentDataPath + "/Difficulty settings" + ".lilgamedif";
        FileStream file = File.Create(diff_path);
        difficulty_settings_save data = new difficulty_settings_save(health_multi, enemy_dam, enemy_cool_down, detect_time);
        bf.Serialize(file, data);
        file.Close();
        Debug.Log("difficulty/settings has been saved");
    }

    public float calc_enemy_damage(float health, float damage)
    {
        health = health - damage;
        return health;
    }

    public float calc_player_damage(float health, float damage)
    {
        if(third_person_camera_control.instance.lock_on == true && lily_charge_attack.instance.gameObject.GetComponent<Animator>().GetFloat("Speed y") < -0.5f)
        {
            damage = damage / 3;
        }

        if (lily_item_buff_engine.infini_health != true)
        {
            health = health - (damage);
        }
        else
        {
            health = health - ((damage) / 10);
        }
        return health;
    }

    public void Save(bool pos_saved, string save_point_index)
    {
        if (Directory.Exists(Application.persistentDataPath + "/save data") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/save data");
        }

        //save the information to the binary file
        save_level();
        save_items();
        save_swords();
        save_money();
        save_notes();
        //save_hints();
        save_missions();
        save_vendor_stocks();
        save_world_time();
        save_lily_file(lily_play_stats.instance.Health, 1, 1, 1);

        State_Object_Flag[] state_flags = Resources.FindObjectsOfTypeAll<State_Object_Flag>();

        foreach (State_Object_Flag flag in state_flags)
        {
            flag.Save_state();
        }

        enemy_list = GameObject.FindObjectsOfType<AI_Controller>(true); ;
        ene_save_list.Clear();

        foreach (AI_Controller obj in enemy_list)
        {
            ene_save_list.Add(obj.GetComponent<Enemy_ID>().ID);
        }

        save_enemies(ene_save_list, get_main_scene_index());

        if (pos_saved == true)
            save_point_id(save_point_index);
        else
            delete_save_point_id();

        Inventory.instance.Add_field("Save Successful");

        Resources.UnloadUnusedAssets();

    }

    public void Save(List<SceneIndexs> seens)
    {
        if (Directory.Exists(Application.persistentDataPath + "/save data") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/save data");
        }

        if (Directory.Exists(Application.persistentDataPath + "/save data/scenes") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/save data/scenes");
        }
        else
        {
            string path = Application.persistentDataPath + "/save data/scenes";
            var hi = Directory.GetFiles(path);

            for (int i = 0; i < hi.Length; i++)
            {
                File.Delete(hi[i]);
            }

            Directory.Delete(path);
            Directory.CreateDirectory(path);
        }

        foreach (SceneIndexs scenenum in seens)
        {
            int num = (int)scenenum;
            string scene_path1 = Application.persistentDataPath + "/save data/scenes/scene layer " + num.ToString() + ".ini";

            if (File.Exists(scene_path1) == true)
            {
                File.Delete(scene_path1);
            }

            StreamWriter writer = new StreamWriter(scene_path1, true);
            writer.WriteLine(num.ToString());
            writer.Close();
        }

        BinaryFormatter formatter_notes = new BinaryFormatter();
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/save information/");
        string scene_save_path = Application.persistentDataPath + "/save data/save information/" + "level save" + ".sav";
        FileStream stream_item = new FileStream(scene_save_path, FileMode.Create);
        level_save_save_data data = new level_save_save_data(get_main_scene_index(), true, 1, 1);
        formatter_notes.Serialize(stream_item, data);
        stream_item.Close();

        //save the information to the binary file
        save_items();
        save_swords();
        save_money();
        save_notes();
        save_hints();
        save_missions();
        save_vendor_stocks();
        save_world_time();
        save_lily_file(lily_play_stats.instance.Health, 1, 1, 1);
        State_Object_Flag[] state_flags = Resources.FindObjectsOfTypeAll<State_Object_Flag>();

        foreach (State_Object_Flag flag in state_flags)
        {
            flag.Save_state();
        }

        enemy_list = GameObject.FindObjectsOfType<AI_Controller>(true); ;
        ene_save_list.Clear();

        foreach (AI_Controller obj in enemy_list)
        {
            ene_save_list.Add(obj.GetComponent<Enemy_ID>().ID);
        }

        save_enemies(ene_save_list, get_main_scene_index());
        Inventory.instance.Add_field("Save successful");
    }

    private List<SceneIndexs> compile_current_scenes()
    {
        Scene[] scene_list = SceneManager.GetAllScenes();
        List<SceneIndexs> compilation = new List<SceneIndexs>();

        foreach (Scene seen in scene_list)
        {
            compilation.Add((SceneIndexs)seen.buildIndex);
        }

        return compilation;
    }

    public static int get_main_scene_index()
    {
        Scene[] scene_list = SceneManager.GetAllScenes();
        int scene_reset_ind = int.MaxValue;

        foreach (Scene seens in scene_list)
        {
            if (seens.buildIndex < scene_reset_ind)
            {
                scene_reset_ind = seens.buildIndex;
            }
        }

        if (scene_reset_ind > SceneManager.sceneCountInBuildSettings)
        {
            scene_reset_ind = 0;
        }

        return scene_reset_ind;
    }

    public static float set_world_time()
    {
        string save_path = Application.persistentDataPath + "/save data/" + "save current time" + ".ini";

        if (File.Exists(save_path) == true)
        {
            StreamReader reader = new StreamReader(save_path);
            string content = reader.ReadLine();
            reader.Close();
            float value = float.Parse(content);
            return value;
        }
        else
        {
            return 12f;
        }
    }

    public void save_world_time()
    {
        float saved_time = DayCycleController.instance.time_of_day;
        string save_path = Application.persistentDataPath + "/save data/" + "save current time" + ".ini";

        if (File.Exists(save_path) == true)
        {
            File.Delete(save_path);
        }

        StreamWriter writer = new StreamWriter(save_path, true);
        writer.WriteLine(saved_time);
        writer.Close();
    }

    public void New_Game(List<SceneIndexs> opening_scenes)
    {
        string path = Application.persistentDataPath + "/playerInfo" + ".lily";

        if (File.Exists(path) == true)
        {
            File.Delete(path);
        }

        if (Directory.Exists(Application.persistentDataPath + "/save data/") == true)
        {
            Directory.Delete(Application.persistentDataPath + "/save data/", true);
        }
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/");

        if (File.Exists(Application.persistentDataPath + "/enemy list" + ".lilfile") == true)
        {
            File.Delete(Application.persistentDataPath + "/enemy list" + ".lilfile");
        }

        save_lily_file(100, 1, 0, 0);

        //save the information to the binary file
        GameObject loader = Instantiate(load_screen_spawner);
        loader.GetComponent<level_loader_tool>().next_level = opening_scenes;
        loader.SetActive(true);
        Debug.Log("A NEW save has successfully been made inside the file directory " + path + "!!!");
    }

    #endregion

    #region private save functions

    private void save_items()
    {
        if (Directory.Exists(Application.persistentDataPath + "/save data/items/") == true)
        {
            Directory.Delete(Application.persistentDataPath + "/save data/items/", true);
        }
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/items/");

        foreach (Item g in Inventory.instance.items)
        {
            string path_item = Application.persistentDataPath + "/save data/" + "items/" + g.name_of_item + ".ini";

            using (StreamWriter sw = new StreamWriter(path_item, true))
            {
                sw.WriteLine("" + g.amount.ToString());
                sw.WriteLine("" + g.name_of_item);
                sw.WriteLine("");
                sw.WriteLine("Saved at:" + System.DateTime.Now);
                Debug.Log("saved succesfuly");
                sw.Close();
            }
        }
    }

    public void save_money()
    {
        int amount = Inventory.instance.cash;

        string save_path = Application.persistentDataPath + "/save data/" + "save currency amount" + ".ini";

        if (File.Exists(save_path) == true)
        {
            File.Delete(save_path);
        }

        StreamWriter writer = new StreamWriter(save_path, true);
        writer.WriteLine(amount);
        writer.Close();
    }

    public static int load_money()
    {
        string save_path = Application.persistentDataPath + "/save data/" + "save currency amount" + ".ini";

        if (File.Exists(save_path) == true)
        {
            StreamReader reader = new StreamReader(save_path);
            string content = reader.ReadLine();
            reader.Close();
            int value = int.Parse(content);
            return value;
        }
        else
        {
            return 0;
        }
    }

    private void save_missions()
    {
        mission_order_array.Clear();

        foreach (missionlist d in Resources.FindObjectsOfTypeAll(typeof(missionlist)) as missionlist[])
        {
            mission_order_array.Add(d);
        }

        BinaryFormatter formatter_missions = new BinaryFormatter();
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "missions/");

        foreach (missionlist g in mission_collection.instance.missions)
        {
            //create the folder for the mission
            Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "missions/" + g.mission_name + "/");

            //inside of the mission folder create a basic info file for keeping track of the missions state
            string path_director_item = Application.persistentDataPath + "/save data/" + "missions/" + g.mission_name + "/" + g.mission_name + ".questcontrol";
            FileStream stream_item = new FileStream(path_director_item, FileMode.Create);
            Save_Data_Mission data = new Save_Data_Mission(g.mission_name, mission_order_array.IndexOf(g), g.complete);
            formatter_missions.Serialize(stream_item, data);
            stream_item.Close();

            //create another folder for the steps being taken and their progression
            Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "missions/" + g.mission_name + "/Progression/");

            //create a file for each step in order that contains thier completed state and descriptions to prevent fucking around
            foreach (missionlist.Objective step in g.steps)
            {
                string step_item_path = Application.persistentDataPath + "/save data/" + "missions/" + g.mission_name + "/Progression/" + step.objective_description + ".step";
                FileStream stream_step = new FileStream(step_item_path, FileMode.Create);
                mission_steps_save step_data = new mission_steps_save(step.objective_description, step.complete);
                formatter_missions.Serialize(stream_step, step_data);
                stream_step.Close();
            }
        }
    }

    private void save_hints()
    {
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "hints/");

        level_hint_order_array.Clear();

        foreach (level_hint d in Resources.FindObjectsOfTypeAll(typeof(level_hint)) as level_hint[])
        {
            level_hint_order_array.Add(d);
        }

        BinaryFormatter formatter_missions = new BinaryFormatter();

        foreach (level_hint g in level_hints_manager.instance.hints)
        {
            //create the folder for the level
            Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "hints/" + get_main_scene_index().ToString() + "/");

            //inside of the level folder create all the files for the saved hints
            string path_director_item = Application.persistentDataPath + "/save data/" + "hints/" + get_main_scene_index().ToString() + "/" + g.hint_name + ".hint";
            FileStream stream_item = new FileStream(path_director_item, FileMode.Create);
            level_hint_save_data data = new level_hint_save_data(g.hint_name, g.hint_description);
            formatter_missions.Serialize(stream_item, data);
            stream_item.Close();
        }
    }

    private void save_swords()
    {
        if (Directory.Exists(Application.persistentDataPath + "/save data/weapons/") == true)
        {
            Directory.Delete(Application.persistentDataPath + "/save data/weapons/", true);
        }
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/weapons/");

        sword_order_array.Clear();

        foreach (Sword d in Resources.FindObjectsOfTypeAll(typeof(Sword)) as Sword[])
        {
            sword_order_array.Add(d);
        }

        BinaryFormatter formatter_sword = new BinaryFormatter();

        foreach (Sword g in sword_collection.instance.swords)
        {
            if (sword_order_array.Contains(g) == true)
            {
                string path_item = Application.persistentDataPath + "/save data/" + "weapons/" + g.name + ".sword";
                FileStream stream_item = new FileStream(path_item, FileMode.Create);
                Sword_save_data data = new Sword_save_data(g.name);

                Debug.Log("this item returns in the id list as:  " + sword_order_array.IndexOf(g));
                Debug.Log("an item has been saved in the save inventory");

                formatter_sword.Serialize(stream_item, data);
                stream_item.Close();
            }
            else
            {
                Debug.Log("OOPSIEWOOPISE!!!! it woows wike dis itwem is not avawable in the ordow awway uwu");
            }
        }
    }

    //used on the instance of manually saving at a save point or beating a level to save the players current inventory of notes
    private void save_notes()
    {
        if (Directory.Exists(Application.persistentDataPath + "/save data/" + "notes/") == true)
        {
            Directory.Delete(Application.persistentDataPath + "/save data/" + "notes/", true);
        }

        Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "notes/");

        note_order_array.Clear();
        foreach (Note d in Resources.FindObjectsOfTypeAll(typeof(Note)) as Note[])
        {
            note_order_array.Add(d);
        }

        BinaryFormatter formatter_notes = new BinaryFormatter();
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "notes/");
        foreach (Note g in notes_collection.instance.notes)
        {
            if (note_order_array.Contains(g) == true)
            {
                string path_item = Application.persistentDataPath + "/save data/" + "notes/" + g.name_of_note + ".note";
                FileStream stream_item = new FileStream(path_item, FileMode.Create);
                note_save_data data = new note_save_data(g.name_of_note);
                Debug.Log("this item returns in the id list as:  " + note_order_array.IndexOf(g));
                Debug.Log("an item has been saved in the save inventory that item is " + g.name_of_note);

                formatter_notes.Serialize(stream_item, data);
                stream_item.Close();
            }
        }
    }

    //used on instance of saving at a save point or on level start when level files do not match to either save the position of the player at the used save point or at a default starting position in the level
    private void save_level()
    {
        if (Directory.Exists(Application.persistentDataPath + "/save data/scenes/") == true)
        {
            Directory.Delete(Application.persistentDataPath + "/save data/scenes/", true);
        }
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/scenes/");

        scenes.Clear();

        scenes = compile_current_scenes();

        scene_ints.Clear();

        foreach (SceneIndexs scenenum in scenes)
        {
            scene_ints.Add((int)scenenum);
        }

        //BinaryFormatter formatter_scenes = new BinaryFormatter();  

        //foreach (SceneIndexs g in scenes)
        //{
        //        string scene_path = Application.persistentDataPath + "/save data/scenes/scene layer " + g.ToString() + ".ini";
        //        FileStream scene_stream_item = new FileStream(scene_path, FileMode.Create);
        //        level_save_data scene_data = new level_save_data((int)g);

        //        formatter_scenes.Serialize(scene_stream_item, scene_data);
        //        scene_stream_item.Close();
        //    
        //}


        foreach (SceneIndexs scenenum in scenes)
        {
            int my_int = (int)scenenum;

            string scene_path = Application.persistentDataPath + "/save data/scenes/scene layer " + my_int.ToString() + ".ini";

            using (StreamWriter sw = new StreamWriter(scene_path, true))
            {
                sw.WriteLine("" + my_int.ToString());
                sw.WriteLine("");
                sw.WriteLine("Saved at:" + System.DateTime.Now);
                Debug.Log("saved succesfully");
                sw.Close();
            }
        }

        BinaryFormatter formatter_notes = new BinaryFormatter();

        Directory.CreateDirectory(Application.persistentDataPath + "/save data/save information/");
        string scene_save_path = Application.persistentDataPath + "/save data/save information/" + "level save" + ".sav";
        FileStream stream_item = new FileStream(scene_save_path, FileMode.Create);
        level_save_save_data data = new level_save_save_data(get_main_scene_index(), true, 1, 1);
        formatter_notes.Serialize(stream_item, data);
        stream_item.Close();
    }

    #endregion

    #region private load functions

    //used on startup of every level to give player object the previously saved items and their item counts
    public static List<Item> load_items()
    {
        //inventory_order_array.Clear();
        List<Item> item_array = new List<Item>();
        List<Item> item_output = new List<Item>();

        foreach (Item d in Resources.FindObjectsOfTypeAll(typeof(Item)) as Item[])
        {
            item_array.Add(d);
        }

        foreach (Item item_ref in item_array)
        {
            string path_item = Application.persistentDataPath + "/save data/" + "items/" + item_ref.name_of_item + ".ini";

            if (File.Exists(path_item) == true)
            {
                int amount_to_give = 1;
                StreamReader reader = new StreamReader(path_item);
                string content = reader.ReadLine();
                reader.Close();
                amount_to_give = int.Parse(content);

                if (item_output.Contains(item_ref) != true)
                {
                    item_output.Add(item_ref);
                }
                //if (Inventory.instance.items.Contains(item_ref) != true)
                //{
                //Inventory.instance.items.Add(item_ref);
                //}
                item_ref.amount = amount_to_give;
            }
        }
        return item_output;
    }

    public static List<level_hint> load_hints()
    {
        List<level_hint> hint_array = new List<level_hint>();
        List<level_hint> hint_output = new List<level_hint>();

        foreach (level_hint d in Resources.FindObjectsOfTypeAll(typeof(level_hint)) as level_hint[])
        {
            hint_array.Add(d);
        }

        string hint_folder = Application.persistentDataPath + "/save data/" + "hints/" + get_main_scene_index().ToString() + "/";

        foreach (level_hint hint_ref in hint_array)
        {
            if (Directory.Exists(hint_folder))
            {
                string hint_direction = Application.persistentDataPath + "/save data/" + "hints/" + get_main_scene_index().ToString() + "/" + hint_ref.hint_name + ".hint";
                Debug.Log("i can see a file for the hint of:  " + hint_ref.hint_name);
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(hint_direction, FileMode.Open);
                level_hint_save_data mission_loaded = (level_hint_save_data)bf.Deserialize(file);
                file.Close();

                if (mission_loaded.hint_name == hint_ref.hint_name)
                {
                    if (mission_loaded.hint_description == hint_ref.hint_description)
                    {
                        hint_output.Add(hint_ref);
                    }
                }
                //when the mission is given to the donor change directory to the progression folder and find each step and mark the completetion
            }
        }
        return hint_output;
    }

    //Used on startup of every level to give player object the previously owned/saved swords
    public static List<Sword> load_swords()
    {
        List<Sword> sword_array = new List<Sword>();
        List<Sword> sword_output = new List<Sword>();

        foreach (Sword d in Resources.FindObjectsOfTypeAll(typeof(Sword)) as Sword[])
        {
            sword_array.Add(d);
        }

        foreach (Sword item_ref in sword_array)
        {
            string path_item = Application.persistentDataPath + "/save data/" + "weapons/" + item_ref.name + ".sword";

            if (File.Exists(path_item))
            {
                Debug.Log("i can see a file for the item of:  " + item_ref.name);
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(path_item, FileMode.Open);
                Sword_save_data item_loaded = (Sword_save_data)bf.Deserialize(file);
                file.Close();

                foreach (Sword swd in sword_array)
                {
                    if (item_loaded.swords_name == swd.name)
                    {
                        sword_output.Add(swd);
                    }
                }
            }
        }
        return sword_output;
    }

    //used on started up every level to give player object the previously owned missions and update their basic values to the updated values
    public static List<missionlist> load_missions()
    {
        List<missionlist> mission_array = new List<missionlist>();
        List<missionlist> mission_output = new List<missionlist>();
        //mission_collection mission_stock = FindPlayer.find_player().GetComponent<mission_collection>();

        foreach (missionlist d in Resources.FindObjectsOfTypeAll(typeof(missionlist)) as missionlist[])
        {
            mission_array.Add(d);
        }

        foreach (missionlist mission_ref in mission_array)
        {
            string quest_folder = Application.persistentDataPath + "/save data/" + "missions/" + mission_ref.mission_name + "/";

            if (Directory.Exists(quest_folder))
            {
                string quest_control_direction = Application.persistentDataPath + "/save data/" + "missions/" + mission_ref.mission_name + "/" + mission_ref.mission_name + ".questcontrol";
                Debug.Log("i can see a file for the item of:  " + mission_ref.mission_name);
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(quest_control_direction, FileMode.Open);
                Save_Data_Mission mission_loaded = (Save_Data_Mission)bf.Deserialize(file);
                file.Close();
                missionlist new_mission = mission_array[mission_loaded.ID_in_array];
                new_mission.complete = mission_loaded.complete;
                //when the mission is given to the donor change directory to the progression folder and find each step and mark the completetion
                foreach (missionlist.Objective mission_step in new_mission.steps)
                {
                    string quest_step_progression = Application.persistentDataPath + "/save data/" + "missions/" + new_mission.mission_name + "/Progression/" + mission_step.objective_description + ".step";

                    if (File.Exists(quest_step_progression))
                    {
                        BinaryFormatter step_bf = new BinaryFormatter();
                        FileStream step_file = File.Open(quest_step_progression, FileMode.Open);
                        mission_steps_save step_loaded = (mission_steps_save)step_bf.Deserialize(step_file);
                        step_file.Close();
                        mission_step.complete = step_loaded.complete;
                    }
                }
                mission_output.Add(new_mission);
            }
        }
        return mission_output;
    }

    //Used on startup of level to give player object the previously owned notes
    public static List<Note> load_notes()
    {
        List<Note> note_array = new List<Note>();
        List<Note> note_output = new List<Note>();

        foreach (Note d in Resources.FindObjectsOfTypeAll(typeof(Note)) as Note[])
        {
            note_array.Add(d);
        }

        foreach (Note item_ref in note_array)
        {
            string path_item = Application.persistentDataPath + "/save data" + "/notes/" + item_ref.name_of_note + ".note";

            if (File.Exists(path_item))
            {
                Debug.Log("i can see a file for the item of:  " + item_ref.name_of_note);
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(path_item, FileMode.Open);
                note_save_data item_loaded = (note_save_data)bf.Deserialize(file);
                file.Close();

                foreach (Note noet in note_array)
                {
                    if (item_loaded.notes_name == noet.name_of_note)
                    {
                        note_output.Add(noet);
                    }
                }
            }
        }

        return note_output;
    }

    #endregion

    //used by the intro cutscene of scenes to gather if cutscene has been seen by the player before. if not or the cutscene is not recurring return false, if they have seen the cutscene. return true
    public bool load_scene_cutscene_config(string cutscene_name)
    {
        string save_location = Application.persistentDataPath + "/save data/" + "Viewed Cutscenes/" + cutscene_name + ".ini";

        if (File.Exists(save_location) != true)
        {
            return false;
        }
        else
        {
            StreamReader reader = new StreamReader(save_location);
            string content = reader.ReadLine();
            reader.Close();

            if (content == "Recurring Cutscene")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public void make_cutscene_save(string cutscene_name, bool recurring)
    {
        if (Directory.Exists(Application.persistentDataPath + "/save data/" + "Viewed Cutscenes/") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "Viewed Cutscenes/");
        }

        string save_location = Application.persistentDataPath + "/save data/" + "Viewed Cutscenes/" + cutscene_name + ".ini";

        if (File.Exists(save_location) == true)
        {
            File.Delete(save_location);
        }

        using (StreamWriter sw = new StreamWriter(save_location, true))
        {
            if (recurring == true)
            {
                sw.WriteLine("" + "Recurring Cutscene");
            }
            else
            {
                sw.WriteLine("" + "Single View Cutscene");
            }
            sw.WriteLine("");
            sw.WriteLine("Saved at:" + System.DateTime.Now);
            Debug.Log("saved succesfuly");
            sw.Close();
        }
        //for each vendor in the scene, create another list of the stock levels of each item they possess, and save this list in a file in the correct save directory   
    }

    public void get_diff_settings()
    {
        string diff_path = Application.persistentDataPath + "/Difficulty settings" + ".lilgamedif";

        if (File.Exists(diff_path))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(diff_path, FileMode.Open);
            difficulty_settings_save data = (difficulty_settings_save)bf.Deserialize(file);
            file.Close();
        }
    }

    public void save_volume_settings(float master, float music, float effects, float dialogue, float ui_vol)
    {
        if (Directory.Exists(Application.persistentDataPath + "/settings") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/settings");
        }

        string setting_path = Application.persistentDataPath + "/settings/" + "audio levels" + ".ini";

        if (File.Exists(setting_path) == true)
        {
            File.Delete(setting_path);
        }

        using (StreamWriter sw = new StreamWriter(setting_path, true))
        {
            sw.WriteLine("" + master.ToString());
            sw.WriteLine("" + music.ToString());
            sw.WriteLine("" + effects.ToString());
            sw.WriteLine("" + dialogue.ToString());
            sw.WriteLine("" + ui_vol.ToString());
            sw.WriteLine("");
            sw.WriteLine("Saved at:" + System.DateTime.Now);
            Debug.Log("saved succesfuly");
            sw.Close();
        }

        gameObject.GetComponent<fmod_audio_level_control>().set_audio_levels(master, music, effects, dialogue, ui_vol);
    }

    public void reset_saved_bindings(bool Keyboard, bool Gamepad)
    {
        if (Keyboard == true)
        {
            save_input_binding("Keyboard", "<Mouse>/rightButton", "Aim", true);
            save_input_binding("Keyboard", "<Mouse>/leftButton", "Attack Light", true);
            save_input_binding("Keyboard", "<Keyboard>/leftCtrl", "Crouch", true);
            save_input_binding("Keyboard", "<Keyboard>/leftShift", "Dodge", true);
            save_input_binding("Keyboard", "<Keyboard>/space", "Heavy Attack", true);
            save_input_binding("Keyboard", "<Keyboard>/e", "Interact", true);
            save_input_binding("Keyboard", "<Mouse>/middleButton", "Lock on", true);
            save_input_binding("Keyboard", "<Keyboard>/leftAlt", "Parry", true);
            save_input_binding("Keyboard", "<Mouse>/leftButton", "Shoot", true);
            save_input_binding("Keyboard", "<Keyboard>/b", "Sprint", true);
        }
        if (Gamepad == true)
        {
            save_input_binding("Gamepad", "<Gamepad>/leftTrigger", "Aim", true);
            save_input_binding("Gamepad", "<Gamepad>/buttonWest", "Attack Light", true);
            save_input_binding("Gamepad", "<Gamepad>/leftStickPress", "Crouch", true);
            save_input_binding("Gamepad", "<Gamepad>/rightTrigger", "Dodge", true);
            save_input_binding("Gamepad", "<Gamepad>/buttonNorth", "Heavy Attack", true);
            save_input_binding("Gamepad", "<Gamepad>/buttonEast", "Interact", true);
            save_input_binding("Gamepad", "<Gamepad>/rightShoulder", "Lock on", true);
            save_input_binding("Gamepad", "<Gamepad>/buttonSouth", "Parry", true);
            save_input_binding("Gamepad", "<Gamepad>/rightTrigger", "Shoot", true);
            save_input_binding("Gamepad", "<Gamepad>/leftShoulder", "Sprint", true);
        }
    }

    public void save_input_binding(string control_or_keyboard, string override_paths, string action, bool force_player_input_change)
    {
        //if (Directory.Exists(Application.persistentDataPath + "/settings" + "/bindings") != true)
        //{
        //   Directory.CreateDirectory(Application.persistentDataPath + "/settings" + "/bindings/");
        //}

        //string setting_path = Application.persistentDataPath + "/settings" + "/bindings/" + control_or_keyboard + " " + action + ".ini";

        //if (File.Exists(setting_path) == true)
        //{
        //   File.Delete(setting_path);
        //}

        //using (StreamWriter sw = new StreamWriter(setting_path, true))
        //{
        //  sw.WriteLine("" + override_paths);
        //sw.WriteLine("");
        //sw.WriteLine("Saved for: " + control_or_keyboard + " " + action);
        //sw.WriteLine("Saved at:" + System.DateTime.Now);
        //Debug.Log("saved succesfuly");
        //sw.Close();
        //}

        // if(force_player_input_change == true)
        //{

        //  load_input_binding(control_or_keyboard, action);

        //}
    }

    public void load_input_binding(string control_or_keyboard, string action)
    {


        //string setting_path = Application.persistentDataPath + "/settings" + "/bindings/" + control_or_keyboard + " " + action + ".ini";



        //if (File.Exists(setting_path) == true)
        //{

        //  StreamReader reader = new StreamReader(setting_path);
        // string content = reader.ReadLine();
        // reader.Close();

        // override_pth = content;

        // GameObject player = FindPlayer.find_player();

        // player.GetComponent<lily_movement_new>().reassign_new_keybindings(control_or_keyboard, override_pth, action);
        //}
        //else
        //{
        //   GameObject player = FindPlayer.find_player();

        //  player.GetComponent<lily_movement_new>().reset_key_binding(control_or_keyboard, action);
        //}

    }

    public void load_volume_settings()
    {
        string setting_path = Application.persistentDataPath + "/settings/" + "audio levels" + ".ini";
        float master = 1.8f;
        float music = 1;
        float effects = 1;
        float dialogue = 1;
        float ui_vol = 1;

        if (File.Exists(setting_path) == true)
        {
            using (TextReader rdr = File.OpenText(setting_path))
            {
                int lineIndex = 0;
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (lineIndex == 0)
                        master = float.Parse(line);
                    else if (lineIndex == 1)
                        music = float.Parse(line);
                    else if (lineIndex == 2)
                        effects = float.Parse(line);
                    else if (lineIndex == 3)
                        dialogue = float.Parse(line);
                    else if (lineIndex == 4)
                        ui_vol = float.Parse(line);

                    lineIndex++;
                }
                rdr.Close();
            }

            gameObject.GetComponent<fmod_audio_level_control>().set_audio_levels(master, music, effects, dialogue, ui_vol);
        }
        else
        {
            gameObject.GetComponent<fmod_audio_level_control>().set_audio_levels(master, music, effects, dialogue, ui_vol);
        }
    }

    public void save_mouse_sensitivity(float x_sens, float y_sens)
    {
        //saves the current look sensitivity
        if (Directory.Exists(Application.persistentDataPath + "/settings") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/settings");
        }

        string setting_path = Application.persistentDataPath + "/settings/" + "mouse look senitivity" + ".ini";

        if (File.Exists(setting_path) == true)
        {
            File.Delete(setting_path);
        }

        using (StreamWriter sw = new StreamWriter(setting_path, true))
        {
            sw.WriteLine("" + x_sens.ToString());
            sw.WriteLine("" + y_sens.ToString());
            sw.WriteLine("");
            sw.WriteLine("Saved at:" + System.DateTime.Now);
            Debug.Log("saved succesfuly");
            sw.Close();
        }
    }

    public void save_mouse_aim_sensitivity(float x_sens, float y_sens)
    {
        //saves the current look sensitivity
        if (Directory.Exists(Application.persistentDataPath + "/settings") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/settings");
        }

        string setting_path = Application.persistentDataPath + "/settings/" + "mouse aim senitivity" + ".ini";

        if (File.Exists(setting_path) == true)
        {
            File.Delete(setting_path);
        }

        using (StreamWriter sw = new StreamWriter(setting_path, true))
        {
            sw.WriteLine("" + x_sens.ToString());
            sw.WriteLine("" + y_sens.ToString());
            sw.WriteLine("");
            sw.WriteLine("Saved at:" + System.DateTime.Now);
            Debug.Log("saved succesfuly");
            sw.Close();
        }
    }

    public void save_stick_sensitivity(float x_sens, float y_sens)
    {
        //saves the current look sensitivity
        if (Directory.Exists(Application.persistentDataPath + "/settings") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/settings");
        }

        string setting_path = Application.persistentDataPath + "/settings/" + "stick look senitivity" + ".ini";

        if (File.Exists(setting_path) == true)
        {
            File.Delete(setting_path);
        }

        using (StreamWriter sw = new StreamWriter(setting_path, true))
        {
            sw.WriteLine("" + x_sens.ToString());
            sw.WriteLine("" + y_sens.ToString());
            sw.WriteLine("");
            sw.WriteLine("Saved at:" + System.DateTime.Now);
            Debug.Log("saved succesfuly");
            sw.Close();
        }
    }

    public void save_stick_aim_sensitivity(float x_sens, float y_sens)
    {
        //saves the current look sensitivity
        if (Directory.Exists(Application.persistentDataPath + "/settings") != true)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/settings");
        }

        string setting_path = Application.persistentDataPath + "/settings/" + "stick aim senitivity" + ".ini";

        if (File.Exists(setting_path) == true)
        {
            File.Delete(setting_path);
        }

        using (StreamWriter sw = new StreamWriter(setting_path, true))
        {
            sw.WriteLine("" + x_sens.ToString());
            sw.WriteLine("" + y_sens.ToString());
            sw.WriteLine("");
            sw.WriteLine("Saved at:" + System.DateTime.Now);
            Debug.Log("saved succesfuly");
            sw.Close();
        }
    }

    public void get_mouse_look_sensitivity()
    {
        string setting_path = Application.persistentDataPath + "/settings/" + "mouse look senitivity" + ".ini";

        float hoz = 25;
        float vert = 25;

        if (File.Exists(setting_path) == true)
        {
            using (TextReader rdr = File.OpenText(setting_path))
            {
                int lineIndex = 0;
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (lineIndex == 0)
                        hoz = float.Parse(line);
                    else if (lineIndex == 1)
                        vert = float.Parse(line);
                    lineIndex++;
                }
                rdr.Close();
            }

            Vector2 val = new Vector2(hoz, vert);
            third_person_camera_control.instance.mouse_look_sensitivity = val;
        }
        else
        {
            third_person_camera_control.instance.mouse_look_sensitivity = new Vector2(25, -25);
        }
    }

    public void get_mouse_aim_sensitivity()
    {
        string setting_path = Application.persistentDataPath + "/settings/" + "mouse aim senitivity" + ".ini";
        float hoz = 1;
        float vert = 1;

        if (File.Exists(setting_path) == true)
        {
            using (TextReader rdr = File.OpenText(setting_path))
            {
                int lineIndex = 0;
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (lineIndex == 0)
                        hoz = float.Parse(line);
                    else if (lineIndex == 1)
                        vert = float.Parse(line);
                    lineIndex++;
                }
                rdr.Close();
            }
            Vector2 val = new Vector2(hoz, vert);
            third_person_camera_control.instance.mouse_aim_sensitivity = val;
        }
        else
        {
            third_person_camera_control.instance.mouse_aim_sensitivity = new Vector2(1, -1);
        }
    }

    public void get_stick_look_sensitivity()
    {
        string setting_path = Application.persistentDataPath + "/settings/" + "stick look senitivity" + ".ini";
        float hoz = 100;
        float vert = 100;

        if (File.Exists(setting_path) == true)
        {
            using (TextReader rdr = File.OpenText(setting_path))
            {
                int lineIndex = 0;
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (lineIndex == 0)
                        hoz = float.Parse(line);
                    else if (lineIndex == 1)
                        vert = float.Parse(line);
                    lineIndex++;
                }
                rdr.Close();
            }

            Vector2 val = new Vector2(hoz, vert);
            third_person_camera_control.instance.thumb_stick_look_sensitivity = val;
            //third_person_camera_control.instance.thumb_stick_look_sensitivity  =  new Vector2(100, -100);
        }
        else
        {
            third_person_camera_control.instance.thumb_stick_look_sensitivity = new Vector2(100, -100);
        }

        third_person_camera_control.instance.enabled = true;
    }

    public void get_stick_aim_sensitivity()
    {
        string setting_path = Application.persistentDataPath + "/settings/" + "stick aim senitivity" + ".ini";
        float hoz = 1;
        float vert = 1;

        if (File.Exists(setting_path) == true)
        {
            using (TextReader rdr = File.OpenText(setting_path))
            {
                int lineIndex = 0;
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (lineIndex == 0)
                        hoz = float.Parse(line);
                    else if (lineIndex == 1)
                        vert = float.Parse(line);
                    lineIndex++;
                }
                rdr.Close();
            }

            Vector2 val = new Vector2(hoz, vert);
            third_person_camera_control.instance.thumb_stick_aim_sensitivity = val;
        }
        else
        {
            third_person_camera_control.instance.thumb_stick_aim_sensitivity = new Vector2(1, -1);
        }
    }

    public float get_sens()
    {
        float the_ret_sens = 0f;
        Directory.CreateDirectory(Application.persistentDataPath + "/settings");
        string setting_path = Application.persistentDataPath + "/settings/" + "player senitivity" + ".txt";

        if (File.Exists(setting_path) == true)
        {
            StreamReader reader = new StreamReader(setting_path);
            string content = reader.ReadLine();
            reader.Close();
            the_ret_sens = float.Parse(content);
            return the_ret_sens;
        }
        else
        {
            the_ret_sens = 150f;
            return the_ret_sens;
        }
    }

    public static void load_enemy_deaths()
    {
        enemy_list listy = fetch_ene_file();

        if (listy.level_name == get_main_scene_index())
        {
            Inventory.instance.Add_field("Save has loaded with " + listy.level_name);
            List<string> enemy_comp_list = new List<string>();
            enemy_comp_list = fetch_ene_file().enemies;
            AI_Controller[] ene_list = GameObject.FindObjectsOfType<AI_Controller>(true);
            foreach (AI_Controller obj in ene_list)
            {
                if (enemy_comp_list.Contains(obj.GetComponent<Enemy_ID>().ID) != true)
                {
                    obj.effective_area.GetComponent<when_player_leaves_volume>().ai.Remove(obj.gameObject);
                    obj.effective_area.GetComponent<when_player_leaves_volume>().GetComponentInChildren<enable_when_player_enters>().npc.Remove(obj.gameObject);
                    Destroy(obj.gameObject);
                }
            }
        }
    }

    public static void load_enemy_deaths_in_effective_area(when_player_leaves_volume area)
    {
        enemy_list listy = fetch_ene_file();

        if (listy.level_name == get_main_scene_index())
        {
            Inventory.instance.Add_field("Save has loaded with " + listy.level_name);
            List<string> enemy_comp_list = new List<string>();
            enemy_comp_list = fetch_ene_file().enemies;
            List<AI_Controller> ene_list = new List<AI_Controller>();
            foreach (GameObject obj in area.ai)
            {
                ene_list.Add(obj.GetComponent<AI_Controller>());
            }


            //AI_Controller[] ene_list = GameObject.FindObjectsOfType<AI_Controller>(true);
            foreach (AI_Controller obj in ene_list)
            {
                if (enemy_comp_list.Contains(obj.GetComponent<Enemy_ID>().ID) != true)
                {
                    obj.effective_area.GetComponent<when_player_leaves_volume>().ai.Remove(obj.gameObject);
                    obj.effective_area.GetComponent<when_player_leaves_volume>().GetComponentInChildren<enable_when_player_enters>().npc.Remove(obj.gameObject);
                    Destroy(obj.gameObject);
                }
            }
        }
    }

    //used by the player movement script to correct the number of enemies, give the player the correct amount of health
    public void load_save_level_states()
    {
        enemy_list listy = fetch_ene_file();

        if (listy.level_name == get_main_scene_index())
        {
            Inventory.instance.Add_field("Save has loaded with " + listy.level_name);
            ene_comp_list.Clear();
            ene_comp_list = fetch_ene_file().enemies;
            enemy_list = GameObject.FindObjectsOfType<AI_Controller>(true); ;

            foreach (AI_Controller obj in enemy_list)
            {
                if (ene_comp_list.Contains(obj.GetComponent<Enemy_ID>().ID) != true)
                {
                    obj.effective_area.GetComponent<when_player_leaves_volume>().ai.Remove(gameObject);
                    obj.effective_area.GetComponent<when_player_leaves_volume>().spawner.GetComponent<enable_when_player_enters>().npc.Remove(gameObject);
                    Destroy(obj.gameObject);
                }
            }
        }
    }

    public static void load_player_position(GameObject playr)
    {
        save_point[] save_locations = Resources.FindObjectsOfTypeAll<save_point>();

        foreach (save_point obj in save_locations)
        {
            if (obj.save_point_id == fetch_save_id())
            {
                obj.summon_player(playr);
            }
        }
    }

    public void Load_on_menu()
    {
        ProgressData this_save = fetch_lily_file();
        get_saved_scenes();
        GameObject loader = new GameObject();
        loader.SetActive(false);
        loader.AddComponent<level_loader_tool>();
        loader.GetComponent<level_loader_tool>().next_level_spawn_position = 0;
        loader.GetComponent<level_loader_tool>().next_level = scenes;
        loader.GetComponent<level_loader_tool>().reset_level = false;
        loader.GetComponent<level_loader_tool>().transition_on_collide = false;
        loader.SetActive(true);
    }

    public void level_beat(int totl_kills)
    {
        save_items();
        save_level();
        save_notes();
        save_swords();
        save_lily_file(lily_play_stats.instance.Health, get_main_scene_index(), total_kills, 0);
        Debug.Log("level beat void has successfully saved progress!");
    }

    private void save_lily_file(float health, int level, int kills, int save_local)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/playerInfo" + ".lily");
        ProgressData data = new ProgressData(health, level, kills, save_local);
        bf.Serialize(file, data);
        file.Close();
        Debug.Log("progress has been saved");
    }

    public static ProgressData fetch_lily_file()
    {
        if (File.Exists(Application.persistentDataPath + "/playerInfo" + ".lily"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/playerInfo" + ".lily", FileMode.Open);
            ProgressData data = (ProgressData)bf.Deserialize(file);
            file.Close();
            return data;
        }
        else
        {
            return null;
        }
    }

    public void save_enemies(List<string> eneies, int thename)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/enemy list" + ".lilfile");
        enemy_list data = new enemy_list(thename, eneies);
        bf.Serialize(file, data);
        file.Close();
        Debug.Log("enemy list has been saved");
    }

    private void save_point_id(string the_id)
    {
        Directory.CreateDirectory(Application.persistentDataPath + "/save data");
        string save_path = Application.persistentDataPath + "/save data/" + "level save point" + ".txt";
        if (File.Exists(save_path) == true)
        {
            File.Delete(save_path);
        }
        StreamWriter writer = new StreamWriter(save_path, true);
        writer.WriteLine(the_id);
        writer.Close();
    }

    //script used on saving to create a list and directory of all the available venders in the level, make individual lists of their stock and save each in a file based under their name in the save data directory
    public void save_vendor_stocks()
    {
        //create the save directory folder for the current level and build a list of each vendor in the scene
        Directory.CreateDirectory(Application.persistentDataPath + "/save data/" + "vendor saves/" + get_main_scene_index().ToString());
        vender_list = Resources.FindObjectsOfTypeAll<vending_machine>();
        //for each vendor in the scene, create another list of the stock levels of each item they possess, and save this list in a file in the correct save directory
        foreach (vending_machine ven in vender_list)
        {
            if (ven.vender_name != null)
            {
                List<int> stock_levels = new List<int>();
                foreach (vending_machine.wares itm in ven.items_on_sale)
                {
                    stock_levels.Add(itm.stock);
                }
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Create(Application.persistentDataPath + "/save data/" + "vendor saves/" + get_main_scene_index().ToString() + "/" + ven.vender_name + ".stkfile");
                VenderStockSave data = new VenderStockSave(ven.vender_name, stock_levels);
                bf.Serialize(file, data);
                file.Close();
            }
        }
    }

    //script used on the load up of a level to find all the vendors and reassign their stock levels back to the correct amount from the previous save to prevent scumming
    public static void load_vendor_stocks()
    {
        if (Directory.Exists(Application.persistentDataPath + "/save data/" + "vendor saves/" + get_main_scene_index()) == true)
        {
            vending_machine[] vend_list = Resources.FindObjectsOfTypeAll<vending_machine>();
            foreach (vending_machine ven in vend_list)
            {
                string stock_dir = Application.persistentDataPath + "/save data/" + "vendor saves/" + get_main_scene_index() + "/" + ven.vender_name + ".stkfile";
                if (File.Exists(stock_dir) == true)
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(stock_dir, FileMode.Open);
                    VenderStockSave stock_loaded = (VenderStockSave)bf.Deserialize(file);
                    file.Close();
                    for (int i = 0; i < ven.items_on_sale.Count; i++)
                    {
                        ven.items_on_sale[i].stock = stock_loaded.Stock_levels[i];
                    }
                }
            }
        }
    }

    public void delete_save_point_id()
    {
        Directory.CreateDirectory(Application.persistentDataPath + "/save data");
        string save_path = Application.persistentDataPath + "/save data/" + "level save point" + ".txt";
        if (File.Exists(save_path) == true)
        {
            File.Delete(save_path);
        }
    }

    public static string fetch_save_id()
    {
        string save_path = Application.persistentDataPath + "/save data/" + "level save point" + ".txt";

        if (File.Exists(save_path) == true)
        {
            StreamReader reader = new StreamReader(save_path);
            string content = reader.ReadLine();
            reader.Close();
            return content;
        }
        else
        {
            return null;
        }
    }

    private void get_saved_scenes()
    {
        string scene_path = Application.persistentDataPath + "/save data/scenes/";

        List<string> individual_scenepath = new List<string>(Directory.GetFiles(scene_path));

        scenes.Clear();

        foreach (string scene in individual_scenepath)
        {
            int scene_ind = 0;

            using (TextReader rdr = File.OpenText(scene))
            {
                int lineIndex = 0;
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (lineIndex == 0)
                        scene_ind = int.Parse(line);
                    lineIndex++;
                }
                rdr.Close();
            }

            scenes.Add((SceneIndexs)scene_ind);
        }
    }

    private int fetch_level1()
    {
        string scene_path = Application.persistentDataPath + "/save data/" + "current level scene 1" + ".ini";

        if (File.Exists(scene_path) == true)
        {
            StreamReader reader = new StreamReader(scene_path);
            string content = reader.ReadLine();
            reader.Close();
            int ind = int.Parse(content);
            return ind;
        }
        else
        {
            return 0;
        }
    }

    private int fetch_level2()
    {
        string scene_path = Application.persistentDataPath + "/save data/" + "current level scene 2" + ".ini";

        if (File.Exists(scene_path) == true)
        {
            StreamReader reader = new StreamReader(scene_path);
            string content = reader.ReadLine();
            reader.Close();
            int ind = int.Parse(content);
            return ind;
        }
        else
        {
            return 0;
        }
    }

    private int fetch_level3()
    {
        string scene_path = Application.persistentDataPath + "/save data/" + "current level scene 3" + ".ini";

        if (File.Exists(scene_path) == true)
        {
            StreamReader reader = new StreamReader(scene_path);
            string content = reader.ReadLine();
            reader.Close();
            int ind = int.Parse(content);
            return ind;
        }
        else
        {
            return 0;
        }
    }

    public static enemy_list fetch_ene_file()
    {
        if (File.Exists(Application.persistentDataPath + "/enemy list" + ".lilfile"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/enemy list" + ".lilfile", FileMode.Open);
            enemy_list data = (enemy_list)bf.Deserialize(file);
            file.Close();
            return data;
        }
        else
        {
            return null;
        }
    }

    public static void assign_new_ID()
    {
        AI_Controller[] ai_list = GameObject.FindObjectsOfType<AI_Controller>(true);

        foreach (AI_Controller ai in ai_list)
        {
            if (ai.gameObject.GetComponent<Enemy_ID>() == true)
            {
                ai.gameObject.GetComponent<Enemy_ID>().ID = "ID" + ai.gameObject.name + (ai.gameObject.transform.position.x * 3000).ToString() + (ai.gameObject.transform.position.y * 2000).ToString() + (ai.gameObject.transform.position.z * 1000).ToString();
            }
            else
            {
                ai.gameObject.AddComponent<Enemy_ID>();
                ai.gameObject.GetComponent<Enemy_ID>().ID = "ID" + ai.gameObject.name + (ai.gameObject.transform.position.x * 3000).ToString() + (ai.gameObject.transform.position.y * 2000).ToString() + (ai.gameObject.transform.position.z * 1000).ToString();
            }
        }
    }
}