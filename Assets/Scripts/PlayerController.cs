using System;
using TMPro;
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

    [SerializeField] private CrashManager crashManager;

    [Header("Controller")]
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;
    [Space]
    [SerializeField] private bool automaticWheelDeploy;
    [SerializeField, Min(0)] private float deployAtHeight;
    [Space]
    [SerializeField] private bool checkRoll;

    [Header("Control Surfaces (Physics)")]
    [SerializeField] private float liftForce;
    [Space]
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
    [Tooltip("Gameobject with every thruster particle systems insided")] public Thruster thrusters;
    [SerializeField] private ParticleSystem explosion;

    [Header("UI")]
    [SerializeField] private Overlay overlay;
    [Space]
    [SerializeField] private TextMeshProUGUI totalTimeChrono;
    [SerializeField] private TextMeshProUGUI currentTimeChrono;

    private float totalTimeStart;
    private float currentTimeStart;

    private bool inRace;

    // ========== HIDDEN ========== //

    [HideInInspector] public AerodynamicObject physics;
    [HideInInspector] public Animator animator;
    [HideInInspector] public bool lockInput;

    private bool wheelsDeployed;

    #endregion

    #region UnityEventSystem

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        animator = GetComponent<Animator>();
        physics = GetComponent<AerodynamicObject>();

        wheelsDeployed = true;
        lockInput = false;
        inRace = false;
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
        ApplyLift();

        // Ray shooting is an expensive method and since it does not need to be called on EVERY FRAME, it is executed in the FixedUpdate() method.
        if (automaticWheelDeploy)
        {
            CheckWheels();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        switch (collision.gameObject.tag)
        {
            // When touching the terrain at a high speed, destroys the plane and respawns it at a higher position.
            case "Terrain":
                if (collision.impulse.magnitude < 5) return;

                Instantiate(explosion, transform.position, Quaternion.Euler(transform.up));
                crashManager.StartCoroutine(crashManager.OnCrashRoutine(this, transform.position));

                gameObject.SetActive(false);
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.gameObject.tag)
        {
            // When reaching a checkpoint, disables it and enables next
            case "CheckPoint":
                OnReachCheckpoint(other.gameObject);
                break;
        }
    }

    #endregion

    #region InputEvents

    // These events are called by the player input manager

    public void OnThrottlePressed(InputAction.CallbackContext context)
    {
        if (lockInput) return;

        PlaneInput.ThrusterPower = context.ReadValue<float>(); 
        thrusters.SetPower(PlaneInput.ThrusterPower);
    }

    public void OnBrakePressed(InputAction.CallbackContext context)
    {
        if (lockInput) return;

        PlaneInput.BrakePower = context.ReadValue<float>();
    }

    public void OnSteer(InputAction.CallbackContext context)
    {
        if (lockInput) return;

        Vector2 direction = context.ReadValue<Vector2>();

        PlaneInput.Yaw = invertX ? -direction.x : direction.x;
        PlaneInput.Pitch = invertY ? direction.y : -direction.y;

        thrusters.SetAngle(-PlaneInput.Pitch);
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (lockInput) return;

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

    #region ControlSurfacesMovement

    /// <summary>
    /// <br> Updates the rotation of the folloing control surfaces: Flaps </br>
    /// <br> Flaps slightly turn itselves for pitch rotations. In addition, they are used to create a steep "barrier" for the in-air brake </br>
    /// </summary>
    void MoveFlaps()
    {
        float angle = -flaps.MaxPitch * PlaneInput.Pitch - flaps.BrakeExtraPitch * PlaneInput.BrakePower;
        float lerpedAngle = lerp ? Mathf.LerpAngle(flaps.Left.transform.localEulerAngles.z, angle, Time.deltaTime * lerpSpeed) : angle;

        flaps.Left.transform.localEulerAngles = new Vector3(flaps.Left.transform.localEulerAngles.x, flaps.Left.transform.localEulerAngles.y, lerpedAngle);
        flaps.Right.transform.localEulerAngles = new Vector3(flaps.Right.transform.localEulerAngles.x, flaps.Right.transform.localEulerAngles.y, lerpedAngle);
    }

    /// <summary>
    /// <br> Updates the rotation of the folloing control surfaces: Ailerons </br>
    /// <br> Ailerons depend only from roll input and have opposite tilts </br>
    /// </summary>
    void MoveAilerons()
    {
        float angle = ailerons.MaxPitch * PlaneInput.Roll;
        
        // Checks the plane roll when there's no input
        const float epsilon = 6;
        bool nullInput = PlaneInput.Roll == 0 && PlaneInput.Yaw == 0;
        bool horizontal = Mathf.Abs((transform.eulerAngles.z + epsilon) % 180) < epsilon * 2;

        if (nullInput && !horizontal && checkRoll)
        {
            // Turns right on the 1st and 3rd quadrant
            const float invertedFlightPreference = -25;
            bool turnRigth = (transform.eulerAngles.z > 0 && transform.eulerAngles.z < 90 - invertedFlightPreference) || (transform.eulerAngles.z > 180 && transform.eulerAngles.z < 270 + invertedFlightPreference);
            angle = (turnRigth ? -1 : 1) * ailerons.MaxPitch / 3;
        }

        // Sets the actual position of the ailerons
        float lerpedAngle = lerp ? Mathf.LerpAngle(ailerons.Right.transform.localEulerAngles.z, angle, Time.deltaTime * lerpSpeed) : angle;

        ailerons.Left.transform.localEulerAngles = new Vector3(ailerons.Left.transform.localEulerAngles.x, ailerons.Left.transform.localEulerAngles.y, -lerpedAngle);
        ailerons.Right.transform.localEulerAngles = new Vector3(ailerons.Right.transform.localEulerAngles.x, ailerons.Right.transform.localEulerAngles.y, lerpedAngle);
    }

    /// <summary>
    /// <br> Updates the rotation of the folloing control surfaces: Elevators </br>
    /// <br> Elevators are mainly used for tilting the plane up/down. In addition, they can also help the plane turning </br>
    /// </summary>
    void MoveElevators()
    {
        float leftAngle = elevators.MaxPitch * PlaneInput.Pitch + elevators.TurnExtraPitch * PlaneInput.Yaw;
        float lerpedLeftAngle = lerp ? Mathf.LerpAngle(elevators.Left.transform.localEulerAngles.z, leftAngle, Time.deltaTime * lerpSpeed) : leftAngle;
        elevators.Left.transform.localEulerAngles = new Vector3(elevators.Left.transform.localEulerAngles.x, elevators.Left.transform.localEulerAngles.y, lerpedLeftAngle);

        float rightAngle = elevators.MaxPitch * PlaneInput.Pitch + -elevators.TurnExtraPitch * PlaneInput.Yaw;
        float lerpedRightAngle = lerp ? Mathf.LerpAngle(elevators.Right.transform.localEulerAngles.z, rightAngle, Time.deltaTime * lerpSpeed) : rightAngle;
        elevators.Right.transform.localEulerAngles = new Vector3(elevators.Right.transform.localEulerAngles.x, elevators.Right.transform.localEulerAngles.y, lerpedRightAngle);
    }

    /// <summary>
    /// <br> Updates the rotation of the folloing control surfaces: Rudders </br>
    /// <br> Rudders depend only from right/left input, but each one has a different inclination (on a turn, the inner one has a grater angle) </br>
    /// </summary>
    void MoveRudders()
    {
        float multiplier = PlaneInput.Yaw > 0 ? rudders.outerRudderPitchPercentage : 1;
        float leftAngle = -rudders.MaxPitch * PlaneInput.Yaw * multiplier;
        float lerpedLeftAngle = lerp ? Mathf.LerpAngle(rudders.Left.transform.localEulerAngles.z, leftAngle, Time.deltaTime * lerpSpeed) : leftAngle;
        rudders.Left.transform.localEulerAngles = new Vector3(rudders.Left.transform.localEulerAngles.x, rudders.Left.transform.localEulerAngles.y, lerpedLeftAngle);

        multiplier = PlaneInput.Yaw < 0 ? rudders.outerRudderPitchPercentage : 1;
        float rightRealAngle = -rudders.MaxPitch * PlaneInput.Yaw * multiplier;
        float lerpedRightAngle = lerp ? Mathf.LerpAngle(rudders.Right.transform.localEulerAngles.z, rightRealAngle, Time.deltaTime * lerpSpeed) : rightRealAngle;
        rudders.Right.transform.localEulerAngles = new Vector3(rudders.Right.transform.localEulerAngles.x, rudders.Right.transform.localEulerAngles.y, lerpedRightAngle);
    }

    #endregion

    /// <summary>
    /// <br> Adds a upwards force in order to keep he plane flying </br>
    /// <br> That force depends of the speed and the angle of the wings </br>
    /// </summary>
    private void ApplyLift()
    {
        float angleFactor = Mathf.Cos(transform.localEulerAngles.z * Mathf.Deg2Rad);
        physics.AddForceAtPosition(transform.up * liftForce * physics.SpeedPercent * angleFactor, transform.position, ForceMode.Acceleration);
    }

    /// <summary>
    /// Shoots a ray downward from the plane and measure its distance from the ground. If it's too low, deploys the wheels.
    /// </summary>
    private void CheckWheels()
    {
        LayerMask mask = LayerMask.GetMask("Terrain");

        Ray ray = new(transform.position, Vector3.down);
        bool terrain = Physics.Raycast(ray, deployAtHeight, mask);

        wheelsDeployed = terrain;
        animator.SetBool("Wheels", wheelsDeployed);
    }
    
    /// <summary>
    /// Updates the plane's data on the UI
    /// </summary>
    private void UpdateUI()
    {
        overlay.UpdateSpeedBar(physics.SpeedPercent);
        overlay.UpdateThrustChart(PlaneInput.ThrusterPower);

        Vector3 dir = transform.forward;
        float tilt = Vector3.Angle(new Vector3(dir.x, 0, dir.z), dir);
        overlay.UpdateTiltChart(tilt / 90);

        overlay.UpdateHeightBar(transform.position.y / 400);

        float inclination = transform.rotation.eulerAngles.z;
        overlay.UpdateInclinationChart(inclination);


        if (inRace)
        {
            totalTimeChrono.text = (Time.time - totalTimeStart).ToString("0.000") + "\"";
            currentTimeChrono.text = (Time.time - currentTimeStart).ToString("0.000") + "\"";
        }
        else
        {
            totalTimeChrono.text = "";
            currentTimeChrono.text = "";
        }
    }

    /// <summary>
    /// Gets the type of checpoint and updates the race state
    /// </summary>
    /// <param name="checkpoint"> Reached checkpoint gameobject </param>
    private void OnReachCheckpoint(GameObject checkpoint)
    {
        switch (checkpoint.name)
        {
            case "START":
                inRace = true;
                totalTimeStart = Time.time;
                goto default;

            case "END":
                inRace = false;
                checkpoint.transform.parent.GetChild(0).gameObject.SetActive(true);
                break;

            default: 
                currentTimeStart = Time.time;
                int index = checkpoint.transform.GetSiblingIndex();
                checkpoint.transform.parent.GetChild(index + 1).gameObject.SetActive(true);
                break;
        }

        checkpoint.gameObject.SetActive(false);
    }
}
