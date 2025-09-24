using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed;
    public float rotationSpeed = 0.15f;
    private Vector2 move, mouseLook, controllerLook;
    private Vector3 rotationalTarget;

    public bool isPC;   //to detect if the device is PC or not

    // Reference to shooting controller for auto-aim rotation
    private ShootingController shootingController;
    private bool isAutoAiming = false;
    private Vector3 autoAimDirection;

    void Start()
    {
        shootingController = GetComponent<ShootingController>();
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        move = ctx.ReadValue<Vector2>();
    }
    public void OnMouseLook(InputAction.CallbackContext ctx)
    {
        mouseLook = ctx.ReadValue<Vector2>();
    }
    public void OnControllerLook(InputAction.CallbackContext ctx)
    {
        controllerLook = ctx.ReadValue<Vector2>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if we should use auto-aim rotation
        isAutoAiming = shootingController != null && shootingController.isAutoShootingOn && shootingController.HasTarget();

        if (isAutoAiming)
        {
            HandleAutoAimRotation();
            MovePlayerWithAutoAim();
        }
        else if (isPC)
        {
            HandlePCRotation();
            MovePlayerWithAim();
        }
        else
        {
            HandleControllerRotation();
        }
    }

    void HandleAutoAimRotation()
    {
        if (shootingController.CurrentTarget != null)
        {
            autoAimDirection = (shootingController.CurrentTarget.position - transform.position).normalized;
            autoAimDirection.y = 0; // Keep rotation horizontal
            
            if (autoAimDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(autoAimDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f); // Faster rotation for aiming
            }
        }
    }

    void HandlePCRotation()
    {
        RaycastHit hit;
        // Use Mouse.current.position to get actual screen coordinates, not mouse delta
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        
        if (Physics.Raycast(ray, out hit))
        {
            rotationalTarget = hit.point;
        }
    }

    void HandleControllerRotation()
    {
        if (controllerLook.x == 0 && controllerLook.y == 0)
        {
            MovePlayer();
        }
        else
        {
            MovePlayerWithAim();
        }
    }

    public void MovePlayer()
    {
        //Prevent the player to snap back to original rotation
        if (move.sqrMagnitude > 0.1f)
        {
            Vector3 movement = new Vector3(move.x, 0f, move.y);

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), rotationSpeed);
            transform.Translate(movement * speed * Time.deltaTime, Space.World);
        }
    }

    public void MovePlayerWithAim()
    {
        if (isPC)
        {
            var lookPos = rotationalTarget - transform.position;
            lookPos.y = 0;
            var rotation = Quaternion.LookRotation(lookPos);

            Vector3 aimDir = new Vector3(rotationalTarget.x, 0f, rotationalTarget.z);

            if (aimDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed);
            }
        }
        else
        {
            Vector3 aimDir = new Vector3(controllerLook.x, 0, controllerLook.y);
            if (aimDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aimDir), rotationSpeed);
            }
        }

        Vector3 movement = new Vector3(move.x, 0, move.y);
        transform.Translate(movement * speed * Time.deltaTime, Space.World);
    }

    void MovePlayerWithAutoAim()
    {
        // Move in the direction of input, but maintain auto-aim rotation
        Vector3 movement = new Vector3(move.x, 0, move.y);
        transform.Translate(movement * speed * Time.deltaTime, Space.World);
    }

    // Public method to check if player is auto-aiming (for animation purposes)
    public bool IsAutoAiming()
    {
        return isAutoAiming;
    }

    // Public method to get auto-aim direction (for animation purposes)
    public Vector3 GetAutoAimDirection()
    {
        return autoAimDirection;
    }
}