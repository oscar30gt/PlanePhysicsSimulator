using System.Collections;
using UnityEngine;

public class CrashManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator blackScreenAnimator;
    [SerializeField] private CameraManager cameraManager;

    public IEnumerator OnCrashRoutine(PlayerController controller, Vector3 crashPosition)
    {
        // Turns the screen black while updating the transform of the objects
        blackScreenAnimator.SetBool("Visible", true);

        yield return new WaitForSecondsRealtime(1.5f);

        // Updates the respawn position
        controller.transform.position = new(crashPosition.x, crashPosition.y + 120, crashPosition.z);
        controller.transform.Rotate(transform.right, -controller.transform.eulerAngles.x);
        cameraManager.transform.position = controller.transform.position;
        blackScreenAnimator.SetBool("Visible", false);
        controller.gameObject.SetActive(true);
        controller.lockInput = true;

        // Sets the speed of the plane for the respawn
        const int respawnSpeed = 120;
        controller.physics.SetVelocity(transform.forward * respawnSpeed);
        PlaneInput.ThrusterPower = 1;
        controller.thrusters.SetPower(PlaneInput.ThrusterPower);

        yield return new WaitForSecondsRealtime(1f);
        controller.lockInput = false;
        
        // Lets the player move the plane by itself
        PlaneInput.ThrusterPower = 0;
        controller.thrusters.SetPower(PlaneInput.ThrusterPower);
    }
}
