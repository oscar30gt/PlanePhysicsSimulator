using UnityEngine;
using UnityEngine.InputSystem;

public static class CameraInput
{
    // Range: [-1,1]
    public static float Yaw;
}

public class CameraManager : MonoBehaviour
{
    [SerializeField] private PlayerController plane;
    [Space]
    [SerializeField] private float maxX;
    [SerializeField] private float sensX;

    [SerializeField, Min(0)] private float returnSlowness;

    private Vector3 positionOffset;

    private float yawOffset;

    // Start is called before the first frame update
    void Start()
    {
        positionOffset = transform.position - plane.transform.position;
        yawOffset = 0;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        yawOffset += Time.deltaTime * sensX * CameraInput.Yaw;
        yawOffset = Mathf.Clamp(yawOffset, -maxX, maxX);

        transform.position = plane.transform.position + positionOffset;

        float lerp = -plane.FacingAirflow / returnSlowness;
        yawOffset = Mathf.Lerp(yawOffset, 0, Time.deltaTime * lerp);

        transform.localEulerAngles = new Vector3(
            transform.localEulerAngles.x,
            plane.transform.localEulerAngles.y + yawOffset,
            transform.localEulerAngles.z
        );
    }

    public void OnCameraRotate(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();

        CameraInput.Yaw = direction.x;
    }
}