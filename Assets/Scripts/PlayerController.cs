using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PlaneInput
{
    // Range: [-1,1]
    public static float Yaw;
    public static float Pitch;
    public static float Roll;

    // Range: [0,1]
    public static float ThrusterPower;
    public static float BrakePower;
}

[Serializable]
public class Flaps
{
    public Transform Right;
    public Transform Left;
    public float MaxPitch;
    public float BrakeExtraPitch;
}

[Serializable]
public class Ailerons
{
    public Transform Right;
    public Transform Left;
    public float MaxPitch;
}

[Serializable]
public class Elevators
{
    public Transform Right;
    public Transform Left;
    public float MaxPitch;
    public float TurnExtraPitch;
}

[Serializable]
public class Rudders
{
    public Transform Right;
    public Transform Left;
    public float MaxPitch;
    [Range(0, 1), Tooltip("On a curve, the outer rudder just tilts a percentage of the other rudder's angle")] public float outerRudderPitchPercentage;
}

public class PlayerController : MonoBehaviour
{
    #region Variables

    // ========== INSPECTOR ========== //

    [Header("Speed")]
    [SerializeField, Tooltip("Measured in meters/second. Min speed the plane has to reach to keep flying horizontally without flaps")] private float minSpeedToCounterGravity;

    [Header("Controller")]
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;
    [Space]
    [SerializeField] private bool automaticWheelDeploy;
    [SerializeField, Min(0)] private float deployAtHeight;

    [Header("Control Surfaces (Visuals)")]
    [SerializeField] private bool lerp;
    [SerializeField, Min(1)] private float lerpSpeed;
    [Space]

    // Flaps change the wings' lift and drag coefficient
    [SerializeField] private Flaps flaps;

    // Ailerons change the roll axis
    [SerializeField] private Ailerons ailerons;

    // Elevators change the pitch axis
    [SerializeField] private Elevators elevators;

    // Rudder changes the yaw axis
    [SerializeField] private Rudders rudders;

    [Header("Effects")]
    [SerializeField, Tooltip("Gameobject with every thruster particle systems insided")] private Thrusters thrusters;

    [Header("UI")]
    [SerializeField] private Overlay overlay;

    // ========== HIDDEN ========== //

    [HideInInspector] public AerodynamicObject physics;
    [HideInInspector] public Animator animator;

    private bool wheelsDeployed;

    #endregion

    #region UnityEventSystem

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        animator = GetComponent<Animator>();
        physics = GetComponent<AerodynamicObject>();

        wheelsDeployed = true;
    }

    void Update()
    {
        MoveFlaps();
        MoveAilerons();
        MoveElevators();
        MoveRudders();

        UpdateUI();
    }

    private void FixedUpdate()
    {
        if (automaticWheelDeploy)
        {
            CheckWheels();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("CheckPoint"))
        {
            other.gameObject.SetActive(false);
            int index = other.transform.GetSiblingIndex();
            other.transform.parent.GetChild(index + 1)?.gameObject.SetActive(true);
        }
    }

    #endregion

    #region InputEvents

    public void OnThrottlePressed(InputAction.CallbackContext context)
    {
        PlaneInput.ThrusterPower = context.ReadValue<float>(); 
        thrusters.SetPower(PlaneInput.ThrusterPower);
    }

    public void OnBrakePressed(InputAction.CallbackContext context)
    {
        PlaneInput.BrakePower = context.ReadValue<float>();
    }

    public void OnSteer(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();

        PlaneInput.Yaw = invertX ? -direction.x : direction.x;
        PlaneInput.Pitch = invertY ? direction.y : -direction.y;

        thrusters.SetAngle(-PlaneInput.Pitch);
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        PlaneInput.Roll = -context.ReadValue<float>();
    }

    public void OnDeployWheels(InputAction.CallbackContext context)
    {
        bool pressStart = context.ReadValue<float>() == 1;
        if (!pressStart || automaticWheelDeploy) return;

        wheelsDeployed = !wheelsDeployed;
        animator.SetBool("Wheels", wheelsDeployed);
    }

    #endregion

    #region ControlSurfacesAnimation

    void MoveFlaps()
    {
        // Flaps slightly turn itselves for pitch rotations. In addition, they are used to create a steep "barrier" for the in-air brake
        float realAngle = -flaps.MaxPitch * PlaneInput.Pitch - flaps.BrakeExtraPitch * PlaneInput.BrakePower;
        float animatedAngle = lerp ? Mathf.LerpAngle(flaps.Left.transform.localEulerAngles.z, realAngle, Time.deltaTime * lerpSpeed) : realAngle;

        flaps.Left.transform.localEulerAngles = new Vector3(flaps.Left.transform.localEulerAngles.x, flaps.Left.transform.localEulerAngles.y, animatedAngle);
        flaps.Right.transform.localEulerAngles = new Vector3(flaps.Right.transform.localEulerAngles.x, flaps.Right.transform.localEulerAngles.y, animatedAngle);

        float flapsPositionFactor = realAngle / flaps.MaxPitch;
    }

    void MoveAilerons()
    {
        // Ailerons depend only from roll input and have opposite tilts
        float realAngle = ailerons.MaxPitch * PlaneInput.Roll;
        float animatedAngle = lerp ? Mathf.LerpAngle(ailerons.Right.transform.localEulerAngles.z, realAngle, Time.deltaTime * lerpSpeed) : realAngle;

        ailerons.Left.transform.localEulerAngles = new Vector3(ailerons.Left.transform.localEulerAngles.x, ailerons.Left.transform.localEulerAngles.y, -animatedAngle);
        ailerons.Right.transform.localEulerAngles = new Vector3(ailerons.Right.transform.localEulerAngles.x, ailerons.Right.transform.localEulerAngles.y, animatedAngle);
    }

    void MoveElevators()
    {
        // Elevators are mainly used for tilting the plane up/down. In addition, they can also help the plane turning
        float leftRealAngle = elevators.MaxPitch * PlaneInput.Pitch + elevators.TurnExtraPitch * PlaneInput.Yaw;
        float leftAnimatedAngle = lerp ? Mathf.LerpAngle(elevators.Left.transform.localEulerAngles.z, leftRealAngle, Time.deltaTime * lerpSpeed) : leftRealAngle;
        elevators.Left.transform.localEulerAngles = new Vector3(elevators.Left.transform.localEulerAngles.x, elevators.Left.transform.localEulerAngles.y, leftAnimatedAngle);

        float rightRealAngle = elevators.MaxPitch * PlaneInput.Pitch + -elevators.TurnExtraPitch * PlaneInput.Yaw;
        float rightAnimatedAngle = lerp ? Mathf.LerpAngle(elevators.Right.transform.localEulerAngles.z, rightRealAngle, Time.deltaTime * lerpSpeed) : rightRealAngle;
        elevators.Right.transform.localEulerAngles = new Vector3(elevators.Right.transform.localEulerAngles.x, elevators.Right.transform.localEulerAngles.y, rightAnimatedAngle);
    }

    void MoveRudders()
    {
        // Rudders depend only from right/left input, but each one has a different inclination (on a turn, the inner one has a grater angle)
        float multiplier = PlaneInput.Yaw > 0 ? rudders.outerRudderPitchPercentage : 1;
        float leftRealAngle = -rudders.MaxPitch * PlaneInput.Yaw * multiplier;
        float leftAnimatedAngle = lerp ? Mathf.LerpAngle(rudders.Left.transform.localEulerAngles.z, leftRealAngle, Time.deltaTime * lerpSpeed) : leftRealAngle;
        rudders.Left.transform.localEulerAngles = new Vector3(rudders.Left.transform.localEulerAngles.x, rudders.Left.transform.localEulerAngles.y, leftAnimatedAngle);

        multiplier = PlaneInput.Yaw < 0 ? rudders.outerRudderPitchPercentage : 1;
        float rightRealAngle = -rudders.MaxPitch * PlaneInput.Yaw * multiplier;
        float rightAnimatedAngle = lerp ? Mathf.LerpAngle(rudders.Right.transform.localEulerAngles.z, rightRealAngle, Time.deltaTime * lerpSpeed) : rightRealAngle;
        rudders.Right.transform.localEulerAngles = new Vector3(rudders.Right.transform.localEulerAngles.x, rudders.Right.transform.localEulerAngles.y, rightAnimatedAngle);
    }

    #endregion

    private void CheckWheels()
    {
        LayerMask mask = LayerMask.GetMask("Terrain");

        Ray ray = new(transform.position, Vector3.down);
        bool terrain = Physics.Raycast(ray, deployAtHeight, mask);

        wheelsDeployed = terrain;
        animator.SetBool("Wheels", wheelsDeployed);
    }
    
    private void UpdateUI()
    {
        overlay.UpdateSpeedBar(physics.velocity.magnitude / 40);
        overlay.UpdateThrustChart(PlaneInput.ThrusterPower);

        Vector3 dir = transform.forward;
        float tilt = Vector3.Angle(new Vector3(dir.x, 0, dir.z), dir);
        overlay.UpdateTiltChart(tilt / 90);
    }
}
