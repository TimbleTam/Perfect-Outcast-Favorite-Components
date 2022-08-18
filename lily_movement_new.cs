using System.Collections;
using UnityEngine;
using Cinemachine;
using FMODUnity;

public class lily_movement_new : MonoBehaviour {
    public enum Weapon_Mode { None, Sword, Ranged };
    public Weapon_Mode weapon_mode = Weapon_Mode.None;
    public int state;
    public Animator anim;
    public bool aim_toggle;
    public bool sprint;
    public bool crouch;
    public GameObject gun;
    public bool in_combat;
    private float sprint_weredown = 5.373f;
    public float movement_multiplyer;
    public float lock_on_distance = 50f;
    public bool dizzy;
    public Sword sword;
    public bool stop_player;
    [Range(0, 10)]
    public float rotation_speed;
    public float speed;
    private float allow_player_movement = 0.1f;
    public bool combat_flipper;

    [Range(-5, 5)]
    public float aim_movement_speed;
    private float dizzy_time;
    public Vector2 input_vector;
    public bool sprint_button_held;
    public static Vector2 aim_view_vector;
    public static Vector2 view_vector;
    public static bool is_game_pad;
    public static bool interact_button_held;
    private Inputmaster inputs;
    public pickup_item_ui pickup_control;
    public pickup_sword_ui sword_pick_up_control;
    public StudioEventEmitter footsteps_emitter;
    public CinemachineVirtualCamera new_third_cam;
    public CinemachineVirtualCamera phone_cam;
    public third_person_camera_control camera_Control;
    private Cinemachine3rdPersonFollow third_view;
    private float weapon_out_dist = 8;
    public float no_weapon_dist = 3;
    private float sprint_dist = 5;
    private float aim_dist = 1;
    private float aim_fov = 30;
    public float default_fov = 40;
    public bool camera_side = false;
    public GameObject weapon_wheel;
    public GameObject lily_vision_volume;
    public Vector3 phone_cam_standing_offset;
    public Vector3 phone_cam_crouching_offset;
    public Vector3 lock_on_offset = new Vector3(0, 1, 0);
    private Vector3 lock_on_damp = new Vector3(1f, 1, 1f);
    private Vector3 lock_on_no_damp = new Vector3(0.2f, 0.5f, 0.2f);
    private Vector3 non_lock_on_offset = new Vector3(0.5f, -0.4f, 0);
    public AnimationCurve sprint_wearoff;
    public float sprint_wearoff_value = 5;
    private Vector3 aim_dir;
    public Vector2 lock_move;

    private void Awake() {
        assign_inputs();
    }

    //Creates the input system controller used to control player movement and assign inputs to their respective functions and variables
    private void assign_inputs() {
        inputs = new Inputmaster();
        //Base Controls
        inputs.Playerinput.Movement.performed += x => input_vector = x.ReadValue<Vector2>();
        inputs.Playerinput.Movement.canceled += x => input_vector.Set(0, 0);
        inputs.Playerinput.AimLook.performed += x => aim_view_vector = x.ReadValue<Vector2>();
        inputs.Playerinput.Look.performed += x => view_vector = x.ReadValue<Vector2>();
        inputs.Playerinput.Sprint.performed += x => sprint_button_held = true;
        inputs.Playerinput.Interact.started += x => interact_button_held = true;
        inputs.Playerinput.Interact.canceled += x => interact_button_held = false;
        inputs.Playerinput.PickUp.performed += x => pickup_control.pick_up();
        inputs.Playerinput.PickUp.performed += x => sword_pick_up_control.pick_up();
        inputs.Playerinput.Crouch.performed += x => toggle_crouch();
        //Attacks
        inputs.Playerinput.Dodge.performed += x => gameObject.GetComponent<lily_charge_attack>().perform_standard_move("dodge", false);
        inputs.Playerinput.HoldDodge.performed += x => gameObject.GetComponent<lily_charge_attack>().perform_standard_move("hold dodge", true);
        inputs.Playerinput.Parry.performed += x => gameObject.GetComponent<lily_charge_attack>().perform_standard_move("parry", false);
        inputs.Playerinput.ParryHold.performed += x => gameObject.GetComponent<lily_charge_attack>().perform_standard_move("hold parry", true);
        inputs.Playerinput.Shoot.performed += x => gameObject.GetComponent<lily_charge_attack>().shoot();
        inputs.Playerinput.AttackLight.performed += x => gameObject.GetComponent<lily_charge_attack>().perform_standard_move("light", false);
        inputs.Playerinput.HoldAttackLight.performed += x => gameObject.GetComponent<lily_charge_attack>().perform_standard_move("hold light", true);
        inputs.Playerinput.HeavyAttack.performed += x => gameObject.GetComponent<lily_charge_attack>().perform_standard_move("heavy", false);
        inputs.Playerinput.HoldHeavyAttack.performed += x => gameObject.GetComponent<lily_charge_attack>().perform_standard_move("hold heavy", true);
        //Combat and Misc
        inputs.Playerinput.Aim.started += x => lily_charge_attack.aim_main = true;
        inputs.Playerinput.Aim.started += x => gameObject.GetComponent<lily_charge_attack>().aim_button_pressed();
        inputs.Playerinput.Aim.canceled += x => lily_charge_attack.aim_main = false;
        inputs.Playerinput.Aim.canceled += x => gameObject.GetComponent<lily_charge_attack>().aim_button_released();
        inputs.Playerinput.Lockon.performed += x => camera_Control.toggle_lock_on();
        inputs.Playerinput.SwitchLockOnLeft.performed += x => camera_Control.lock_on_left();
        inputs.Playerinput.SwitchLockOnRight.performed += x => camera_Control.lock_on_right();
        inputs.Playerinput.QuickAccessHealth.performed += x => Inventory.instance.up_dpad();
        inputs.Playerinput.ToggleSword.performed += x => gameObject.GetComponent<lily_charge_attack>().toggle_sword();
        inputs.Playerinput.SwitchCameraSide.performed += x => camera_side = !camera_side;
        inputs.Playerinput.WeaponWheelOpen.performed += x => weapon_wheel.SetActive(true);
        inputs.Playerinput.WeaponWheelOpen.canceled += x => weapon_wheel.SetActive(false);
        inputs.Playerinput.LilyVision.performed += x => lily_vision_volume.SetActive(true);
        inputs.Playerinput.LilyVision.performed += x => GUI_control.instance.special_equip_icon.SetBool("equipped", true);
        inputs.Playerinput.LilyVision.canceled += x => lily_vision_volume.SetActive(false);
        inputs.Playerinput.LilyVision.canceled += x => GUI_control.instance.special_equip_icon.SetBool("equipped", false);
        inputs.Playerinput.LockonMovement.performed += x => lock_move = x.ReadValue<Vector2>();
        inputs.Playerinput.LockonMovement.canceled += x => lock_move.Set(0, 0);
    }

    void Start() {
        anim = gameObject.GetComponent<Animator>();
        state = 1;
        third_view = new_third_cam.GetCinemachineComponent<Cinemachine.Cinemachine3rdPersonFollow>();
        Camera.main.gameObject.GetComponent<StudioListener>().attenuationObject = gameObject;
        SaveData.load_player_position(gameObject);
        SaveData.load_vendor_stocks();
    }

    //if player movement is enabled then allow the input system to track the inputs specified in this class
    private void OnEnable() {
        inputs.Enable();
    }

    //on disabling, the input system will be disabled aswell preventing useless checks every tick.
    //the animation speeds for aiming, running and lock on movement will also be set to 0 to prevent movement if the player model is still in view.
    private void OnDisable() {
        inputs.Disable();
        anim.SetFloat("Speed x", 0);
        anim.SetFloat("Speed y", 0);
        anim.SetBool("aiming", false);
        anim.SetFloat("sqrmag", 0);
    }

    //when the player model is destroyed or the scene it is inside of is destroyed. the input system will also be destroyed.
    //Bug found previously showed that old input systems were still in use so destroying them is neccessary to prevent a leak.
    private void OnDestroy() {
        inputs.Disable();
    }

    void Update() {
        if (lily_charge_attack.aim_main) {
            aim_rotation();
            weapon_mode = Weapon_Mode.Ranged;
            aim_toggle = false;
        }

        if (sprint_button_held && speed > allow_player_movement && !lily_play_stats.stamina_out && !crouch)
            sprint = true;
        else
            sprint = false;

        if (speed < allow_player_movement || lily_play_stats.stamina_out || crouch)
            sprint_button_held = false;

        //if ui is open or the player is caught in a stun or higher reasoning is telling the player not to move, no movement will activat
        if (lily_charge_attack.aim_main != true && !aim_toggle)
            aim_toggle = true;

        if (speed > allow_player_movement) {
            anim.SetFloat("hoz_strafe", input_vector.x);
            anim.SetFloat("vert_strafe", input_vector.y);
        }
        else {
            anim.SetFloat("hoz_strafe", 0);
            anim.SetFloat("vert_strafe", 0);
        }

        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("lock_on_movement")) {
            var lookPos = lily_charge_attack.instance.lock_on_control.lock_on_object.transform.position - transform.position;
            lookPos.y = 0;
            var rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = rotation;
        }

        movement_processing();
    }

    private void LateUpdate() {
        Shader.SetGlobalFloat("_Dither_Distance", third_view.CameraDistance);

        if (camera_side) {
            if (lily_charge_attack.aim_main)
                third_view.CameraSide = Mathf.Lerp(third_view.CameraSide, 0.86f, 0.1f);
            else
                third_view.CameraSide = Mathf.Lerp(third_view.CameraSide, 1f, 0.1f);
        }
        else {
            if (lily_charge_attack.aim_main)
                third_view.CameraSide = Mathf.Lerp(third_view.CameraSide, 0.14f, 0.1f);
            else
                third_view.CameraSide = Mathf.Lerp(third_view.CameraSide, 0f, 0.1f);
        }

        if (!camera_Control.enabled)
            camera_Control.enabled = true;

        if (anim.GetFloat("Crouch") > 0.5f) {
            camera_Control.gameObject.GetComponent<play_position_follow>().offset = Vector3.Lerp(camera_Control.gameObject.GetComponent<play_position_follow>().offset, new Vector3(0, 0.909f, 0), 0.1f);
            phone_cam.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = phone_cam_crouching_offset;
            
            if (lily_charge_attack.instance.sword_out)
                third_view.VerticalArmLength = Mathf.Lerp(third_view.VerticalArmLength, 0.6f, 0.1f);
            else
                third_view.VerticalArmLength = Mathf.Lerp(third_view.VerticalArmLength, 0f, 0.1f);
        }
        else {
            camera_Control.gameObject.GetComponent<play_position_follow>().offset = Vector3.Lerp(camera_Control.gameObject.GetComponent<play_position_follow>().offset, new Vector3(0, 1.598f, 0), 0.1f);
            phone_cam.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = phone_cam_standing_offset;
            if (lily_charge_attack.instance.sword_out) {
                if (lily_charge_attack.aim_main)
                    third_view.VerticalArmLength = Mathf.Lerp(third_view.VerticalArmLength, 0.4f, 0.1f);
                else
                    third_view.VerticalArmLength = Mathf.Lerp(third_view.VerticalArmLength, 1f, 0.1f);
            }
            else
                third_view.VerticalArmLength = Mathf.Lerp(third_view.VerticalArmLength, 0.4f, 0.1f);
        }

        if (sprint) {
            third_view.CameraDistance = Mathf.Lerp(third_view.CameraDistance, sprint_dist, 0.1f);
            new_third_cam.m_Lens.FieldOfView = Mathf.Lerp(new_third_cam.m_Lens.FieldOfView, Mathf.Lerp(default_fov, default_fov * 2f, sprint_wearoff.Evaluate(sprint_wearoff_value)), 0.5f);
        }
        else if (lily_charge_attack.aim_main) {
            third_view.CameraDistance = Mathf.Lerp(third_view.CameraDistance, aim_dist, 0.1f);
            new_third_cam.m_Lens.FieldOfView = Mathf.Lerp(new_third_cam.m_Lens.FieldOfView, aim_fov, 0.1f);
        }
        else {
            if (lily_charge_attack.instance.sword_out) {
                third_view.CameraDistance = Mathf.Lerp(third_view.CameraDistance, weapon_out_dist, 0.1f);
                new_third_cam.m_Lens.FieldOfView = Mathf.Lerp(new_third_cam.m_Lens.FieldOfView, default_fov, 0.1f);
            }
            else {
                third_view.CameraDistance = Mathf.Lerp(third_view.CameraDistance, no_weapon_dist, 0.1f);
                new_third_cam.m_Lens.FieldOfView = Mathf.Lerp(new_third_cam.m_Lens.FieldOfView, default_fov, 0.1f);
            }
        }

        if (third_person_camera_control.instance.lock_on) {
            third_view.ShoulderOffset = Vector3.Lerp(third_view.ShoulderOffset, lock_on_offset, 0.1f);
            third_view.Damping = Vector3.Lerp(third_view.Damping, lock_on_damp, 0.1f);
        }
        else {
            third_view.ShoulderOffset = Vector3.Lerp(third_view.ShoulderOffset, non_lock_on_offset, 0.1f);
            third_view.Damping = Vector3.Lerp(third_view.Damping, lock_on_no_damp, 0.1f);
        }
        Shader.SetGlobalVector("player_pos", gameObject.transform.position);
    }

    public IEnumerator Display_level_name() {
        yield return new WaitForSecondsRealtime(1);
        GUI_control.instance.level_Title.show_title();
    }

    public void toggle_crouch() {
        crouch = !crouch;

        if (crouch)
            gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0, 0.5f, 0);
        else
            gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0, 1, 0);

        gameObject.GetComponent<CapsuleCollider>().height = (float.Parse(crouch.ToString()) + 1);
    }

    //checked every update when this script is active
    public void movement_processing() {
        
        //if the player is aming. adjust the weapon mode to reflect the correct weapon and assign the aiming bool in the animator to allow the animing animation
        if (lily_charge_attack.aim_main)
            weapon_mode = Weapon_Mode.Ranged;
        else
            weapon_mode = Weapon_Mode.Sword;
        anim.SetBool("aiming", lily_charge_attack.aim_main);

        //if the player is currently locked on to an enemy use a simple locked on movement
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("lock_on_movement")) {
            anim.SetFloat("Speed x", lock_move.x);
            anim.SetFloat("Speed y", lock_move.y);

            //if the player is currently trying to move, rotate the player model towards the locked on enemy with a slerp
            if (lock_move.x != 0 || lock_move.y != 0) {
                Vector3 posA = gameObject.transform.position;
                Vector3 posB = third_person_camera_control.instance.lock_on_object.transform.position;
                posA.y = 0;
                posB.y = 0;
                Vector3 dir = (posB - posA).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotation_speed);
            }
        }
        else {
            //get the speed the player is trying to move by getting the square magnitude of the directon vector2
            //if the speed is greater than the deadzone then allow movement (handled by a seperate function)
            speed = input_vector.sqrMagnitude;
            anim.SetFloat("sqrmag", speed, 0.0f, Time.deltaTime);
            if (speed > allow_player_movement)
                Allow_movement();
        }

        //if the player is currently sprinting, adjust the animator to reflect that, same for combat mode and the combat flipper (combat flipper is used for debug in the editor)
        anim.SetBool("Sprint", sprint);
        anim.SetBool("in_combat", in_combat);
        combat_flipper = in_combat;

        //if the player is sprinting when in combat. adjust the rotation speed as well as their slowly reduce their stamina over time.
        //if they arent sprinting in combat keep the rotation speed at the desired amount
        if (sprint && in_combat) {
            rotation_speed = 0.2f;
            gameObject.GetComponent<lily_play_stats>().stamina_cooldown = 0;
            if (gameObject.GetComponent<lily_play_stats>().Stamina > 0) {
                sprint = true;
                gameObject.GetComponent<lily_play_stats>().Stamina -= (Time.deltaTime * sprint_weredown);
                gameObject.GetComponent<lily_play_stats>().Stamina_recovery -= (Time.deltaTime * (sprint_weredown / 2));
            }
            else
                sprint = false;
        }
        else
            rotation_speed = 0.35f;

        //if the player is crouched. lerp the crouch float in the animator to reflect this.
        anim.SetFloat("Crouch", Mathf.Lerp(anim.GetFloat("Crouch"), int.Parse(crouch.ToString()), 0.25f));

        //if the player is currently sprinting they are unable to crouch. (show me a person sprinting while crouched i dare you)
        if (sprint)
            crouch = false;
    }

    //called every frame when the player is aiming. Adjusts the direction of the player to reflect the direction the camera is pointing. slight slerp added to give a weight to the movement
    public void aim_rotation() {
        aim_dir = Camera.main.transform.forward + Camera.main.transform.right;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aim_dir), rotation_speed);
    }

    //called by enemies when they enter agro and by the player on leaving enemy areas and when all enemies in an area are killed.
    //changes the combat mode of the player to an appropriate state and toggles the facial expression of the player to match the current state
    public void toggle_combat_mode(bool val) {
            in_combat = val;
            anim.SetBool("scared", false);
    }

    //called whenever the weapon in the players hand needs to be changed.
    //changes the weapon thats in the players hand and if that weapon is the currently equipped sword. change the weapon_size int in the animator to reflect the weight of the weapon
    public void change_weapon_mode(string weapon_choice) {
        switch (weapon_choice) {
            case "Sword":
                gun.SetActive(false);
                gameObject.GetComponent<lily_charge_attack>().sword_object.SetActive(true);
                lily_charge_attack.instance.toggle_sword(true);
                weapon_mode = Weapon_Mode.Sword;

                if (gameObject.GetComponent<lily_charge_attack>().sword.size == Sword.SwordSize.Small)
                    anim.SetInteger("weapon_size", 0);
                else if (gameObject.GetComponent<lily_charge_attack>().sword.size == Sword.SwordSize.Medium)
                    anim.SetInteger("weapon_size", 2);
                else
                    anim.SetInteger("weapon_size", 1);
                break;
            case "Ranged":
                gun.SetActive(true);
                gameObject.GetComponent<lily_charge_attack>().sword_object.SetActive(false);
                weapon_mode = Weapon_Mode.Ranged;
                break;
            default:
                gun.SetActive(false);
                gameObject.GetComponent<lily_charge_attack>().sword_object.SetActive(false);
                weapon_mode = Weapon_Mode.None;
                break;
        }
    }

    //called from inside movement_processing. This function is the control for the player movement when not locked on.
    private void Allow_movement() {
        //take the direction the player is trying to move from the stick or directional button
        //multiply that by the direction of the camera to find the direction the player is trying to move in 3d space
        float z = input_vector.y * movement_multiplyer;
        float y = input_vector.x * movement_multiplyer;
        var forward = Camera.main.transform.forward;
        var right = Camera.main.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        //assign the speed of the player based on how far the stick is being pushed. if the player is aming, half the speed. non applicable snippet for keyboard play.
        if (!lily_charge_attack.aim_main)
            anim.SetFloat("sqrmag", speed, 0.0f, Time.deltaTime);
        else
            anim.SetFloat("sqrmag", speed / 2, 0.0f, Time.deltaTime);

        //find the y axis for the player model to take from the desired direction and rotate towards it based on the rotation speed
        Vector3 dir = forward * z + right * y;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotation_speed);
    }

    //function called whenever the player presses the button to dodge. Will check if the player is currently able to and perform the dodge animation
    public void Dodge() {
        //check that the player is not out of stamina, not holing down aim and not currently dodging.
        //stacking dodges would allow the player to stack I frames making the combat too easy and dodging while aiming proved to be disorienting
        if (!lily_play_stats.stamina_out && !anim.GetCurrentAnimatorStateInfo(0).IsTag("dodge") && !lily_charge_attack.aim_main) {
            //reset the attack triggers to prevent them from acting unwillingly after the animation is concluded
            anim.ResetTrigger("light_attack");
            anim.ResetTrigger("heavy_attack");
            //use dodge_stam to remove the calculated dodge stamina, calculate the stamina recovery cooldown time and set the cooldown timer to zero
            gameObject.GetComponent<lily_play_stats>().dodge_stam();
            gameObject.GetComponent<lily_play_stats>().stamina_cooldown = 0;
            //find the direction the player is wanting to dodge in 3d space using the camera and the direction they are trying to move.
            //if the player isnt moving the stick or directional keys, the direction of the dodge will automatically choose forward
            float z = input_vector.y * movement_multiplyer;
            float y = input_vector.x * movement_multiplyer;
            var forward = Camera.main.transform.forward;
            var right = Camera.main.transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            forward.Normalize();
            Vector3 dir = forward * z + right * y;
            //align the player model to the direction chosen for dodging and perform the animation
            transform.rotation = Quaternion.LookRotation(dir);
            anim.SetTrigger("dodge");
        }
    }

    //function for a larger ranged, combo triggered dodge.
    //Unlike the standard dodge this will take up higher stamina but result in a longer reaching dodge for getting the player out of a bad position
    public void slide_dodge() {
        if (!lily_play_stats.stamina_out) {
            //reset the attack triggers to prevent them from acting unwillingly after the animation is concluded
            anim.ResetTrigger("light_attack");
            anim.ResetTrigger("heavy_attack");
            //remove the base stamina points for the action, use dodge_stam to remove the calculated dodge stamina, calculate the stamina recovery cooldown time and set the cooldown timer to zero
            gameObject.GetComponent<lily_play_stats>().Stamina -= 10;
            gameObject.GetComponent<lily_play_stats>().dodge_stam();
            gameObject.GetComponent<lily_play_stats>().stamina_cooldown = 0;
            //find the direction the player is wanting to dodge in 3d space using the camera and the direction they are trying to move.
            //if the player isnt moving the stick or directional keys, the direction of the dodge will automatically choose forward
            float z = input_vector.y * movement_multiplyer;
            float y = input_vector.x * movement_multiplyer;
            var forward = Camera.main.transform.forward;
            var right = Camera.main.transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            forward.Normalize();
            Vector3 dir = forward * z + right * y;
            //align the player model to the direction chosen for dodging and perform the animation
            transform.rotation = Quaternion.LookRotation(dir);
            anim.SetTrigger("slide dodge");
        }
    }

    //Very similar to the dodge function, except this function is called automatically when the player is hit by an attack when the auto dodge buff is enabled.
    //calling the dodge functions automatically didn't produce the results desired since auto dodges arent supposed to use any stamina
    public void Evade() {
        float z = input_vector.y * movement_multiplyer;
        float y = input_vector.x * movement_multiplyer;
        var forward = Camera.main.transform.forward;
        var right = Camera.main.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        forward.Normalize();
        Vector3 dir = forward * z + right * y;
        transform.rotation = Quaternion.LookRotation(dir);
        anim.SetTrigger("dodge");
    }

    public void Stunned(float time) {
        anim.SetBool("stun", true);
        dizzy = true;
        StartCoroutine(Dizzy(time));
    }

    IEnumerator Dizzy(float time) {
        if (dizzy_time <= time) {
            dizzy = true;
            anim.SetBool("stun", true);
            dizzy_time += 0.2f;
            yield return new WaitForSecondsRealtime(0.2f);
        }
        else {
            dizzy = false;
            anim.SetBool("stun", false);
        }
    }

    //I dont know what this does...
    public void relocate() {
        state = 1;
        gameObject.GetComponent<lily_play_stats>().Health = gameObject.GetComponent<lily_play_stats>().Health_max;
    }

    //checks everytime the player exits a trigger to see if its an active enemy area.
    //If this is the case then the player will trigger exit_combat_mode and deactivate the enemy area.
    private void OnTriggerExit(Collider other) {
        if (other.tag == "Player in range")
            toggle_combat_mode(false);
    }
}