using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// Press V to switch between the third-person orbit camera and a first-person camera.
//
// Lives on the Player. The third-person camera is the one already in the scene
// (GameManager.playerCamera). The first-person camera is built here at runtime, so
// the scene needs no extra objects: a CinemachineCamera locked to an eye anchor on
// the player, rotated by CinemachinePanTilt, which this script drives from the Look
// action.
//
// While in first person:
//   - the body is rendered as shadows only, so the camera does not look into the head
//   - the player always faces where the camera looks (see LateUpdate), so attacks go
//     where the player is looking
public class PlayerViewSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("Third-person camera. Left empty, GameManager.playerCamera is used.")]
    [SerializeField] private CinemachineCamera thirdPersonCamera;
    [Tooltip("Seconds to blend between the two views.")]
    [SerializeField] private float blendTime = 0.3f;

    [Header("First person")]
    [Tooltip("Point the first-person camera sits at. Left empty, one is created at head height.")]
    [SerializeField] private Transform eyeAnchor;
    [Tooltip("Degrees per pixel of mouse movement.")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [Tooltip("Degrees per second at full stick deflection.")]
    [SerializeField] private float stickSensitivity = 150f;
    [Tooltip("How far the player can look up (x) and down (y), in degrees.")]
    [SerializeField] private Vector2 tiltRange = new Vector2(-70f, 70f);
    [SerializeField] private bool hideBody = true;
    [Tooltip("Keep the weapon in the hand visible in first person.")]
    [SerializeField] private bool keepWeaponVisible = true;

    public bool IsFirstPerson { get; private set; }

    // False while something else owns the camera (dialogue). Blocks looking and switching.
    public bool LookEnabled { get; set; } = true;

    private PlayerControl playerControl;
    private InputSystem_Actions inputActions;
    private CinemachineCamera firstPersonCamera;
    private CinemachinePanTilt panTilt;

    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private readonly List<ShadowCastingMode> hiddenRendererModes = new List<ShadowCastingMode>();
    private float hideBodyTimer = -1f;

    private void Awake()
    {
        playerControl = GetComponent<PlayerControl>();
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Look.Enable();
        CinemachineCore.GetBlendOverride += GetViewBlend;
    }

    private void OnDisable()
    {
        inputActions.Player.Look.Disable();
        CinemachineCore.GetBlendOverride -= GetViewBlend;
    }

    private void OnDestroy()
    {
        ShowBody();
        inputActions.Dispose();
        if (firstPersonCamera != null)
        {
            Destroy(firstPersonCamera.gameObject);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame && CanSwitch())
        {
            SetFirstPerson(!IsFirstPerson);
        }

        if (IsFirstPerson && CanLook())
        {
            Look();
        }
    }

    private void LateUpdate()
    {
        if (!IsFirstPerson)
        {
            return;
        }

        if (playerControl != null && playerControl.isDead)
        {
            // show the death animation from outside
            SetFirstPerson(false);
            return;
        }

        if (hideBodyTimer >= 0f)
        {
            hideBodyTimer -= Time.unscaledDeltaTime;
            if (hideBodyTimer < 0f)
            {
                HideBody();
            }
        }

        // After Update and after the animator's root motion, so nothing turns the
        // player away from the view direction this frame.
        if (CanLook())
        {
            transform.rotation = Quaternion.Euler(0f, panTilt.PanAxis.Value, 0f);
        }
    }

    public void SetFirstPerson(bool firstPerson)
    {
        if (firstPerson == IsFirstPerson)
        {
            return;
        }

        if (firstPerson)
        {
            if (!EnsureFirstPersonCamera())
            {
                return;
            }

            // start looking the way the camera looks now
            Vector3 viewAngles = GetViewAngles();
            panTilt.PanAxis.Value = panTilt.PanAxis.ClampValue(viewAngles.y);
            panTilt.TiltAxis.Value = panTilt.TiltAxis.ClampValue(viewAngles.x);
            firstPersonCamera.PreviousStateIsValid = false;
            firstPersonCamera.gameObject.SetActive(true);

            // hide the body once the camera is most of the way in
            hideBodyTimer = blendTime * 0.7f;
        }
        else
        {
            // keep looking the same way when the orbit camera takes over
            CinemachineCamera orbitCamera = GetThirdPersonCamera();
            CinemachineOrbitalFollow orbit = orbitCamera != null ? orbitCamera.GetComponent<CinemachineOrbitalFollow>() : null;
            if (orbit != null)
            {
                orbit.HorizontalAxis.Value = orbit.HorizontalAxis.ClampValue(panTilt.PanAxis.Value);
                orbit.VerticalAxis.Value = orbit.VerticalAxis.ClampValue(panTilt.TiltAxis.Value);
                orbitCamera.PreviousStateIsValid = false;
            }

            firstPersonCamera.gameObject.SetActive(false);
            hideBodyTimer = -1f;
            ShowBody();
        }

        IsFirstPerson = firstPerson;
    }

    // ---------- Input ----------

    private bool CanSwitch()
    {
        if (!LookEnabled || Time.timeScale == 0f)
        {
            return false;
        }
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            return false;
        }
        return playerControl == null || !playerControl.isDead;
    }

    private bool CanLook()
    {
        // The cursor is unlocked while a menu is open or Alt is held.
        return CanSwitch() && Cursor.lockState == CursorLockMode.Locked;
    }

    private void Look()
    {
        InputAction look = inputActions.Player.Look;
        Vector2 delta = look.ReadValue<Vector2>();
        if (delta == Vector2.zero)
        {
            return;
        }

        // A pointer reports pixels moved this frame, a stick reports how far it is pushed.
        bool isPointer = look.activeControl != null && look.activeControl.device is Pointer;
        delta *= isPointer ? mouseSensitivity : stickSensitivity * Time.deltaTime;

        panTilt.PanAxis.Value = panTilt.PanAxis.ClampValue(panTilt.PanAxis.Value + delta.x);
        panTilt.TiltAxis.Value = panTilt.TiltAxis.ClampValue(panTilt.TiltAxis.Value - delta.y);
    }

    // ---------- Cameras ----------

    private CinemachineCamera GetThirdPersonCamera()
    {
        if (thirdPersonCamera == null && GameManager.Instance != null && GameManager.Instance.playerCamera != null)
        {
            thirdPersonCamera = GameManager.Instance.playerCamera.GetComponent<CinemachineCamera>();
        }
        return thirdPersonCamera;
    }

    private bool EnsureFirstPersonCamera()
    {
        if (firstPersonCamera != null)
        {
            return true;
        }

        CinemachineCamera orbitCamera = GetThirdPersonCamera();

        GameObject cameraObject = new GameObject("FirstPersonCamera");
        cameraObject.SetActive(false);

        firstPersonCamera = cameraObject.AddComponent<CinemachineCamera>();
        firstPersonCamera.Follow = GetEyeAnchor();
        firstPersonCamera.Priority = (orbitCamera != null ? orbitCamera.Priority.Value : 0) + 10;
        if (orbitCamera != null)
        {
            firstPersonCamera.Lens = orbitCamera.Lens;
            firstPersonCamera.OutputChannel = orbitCamera.OutputChannel;
        }

        cameraObject.AddComponent<CinemachineHardLockToTarget>();

        panTilt = cameraObject.AddComponent<CinemachinePanTilt>();
        panTilt.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;
        panTilt.TiltAxis.Range = tiltRange;
        panTilt.TiltAxis.Center = 0f;
        return true;
    }

    private Transform GetEyeAnchor()
    {
        if (eyeAnchor != null)
        {
            return eyeAnchor;
        }

        // A fixed point on the player instead of the head bone itself, so attack and
        // dodge animations do not shake the view.
        float eyeHeight;
        Animator animator = GetComponent<Animator>();
        Transform head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        CharacterController controller = GetComponent<CharacterController>();
        if (head != null)
        {
            // the head bone is at the base of the skull, the eyes are a little above it
            eyeHeight = transform.InverseTransformPoint(head.position).y * 1.04f;
        }
        else if (controller != null)
        {
            eyeHeight = (controller.center.y + controller.height * 0.5f) * 0.93f;
        }
        else
        {
            eyeHeight = 1.6f;
        }

        eyeAnchor = new GameObject("EyeAnchor").transform;
        eyeAnchor.SetParent(transform, false);
        eyeAnchor.localPosition = new Vector3(0f, eyeHeight, 0f);
        return eyeAnchor;
    }

    // Pitch (x) and yaw (y) of the rendered view, both in -180..180.
    private Vector3 GetViewAngles()
    {
        Camera viewCamera = playerControl != null && playerControl.PlayerCamera != null ? playerControl.PlayerCamera : Camera.main;
        Vector3 angles = viewCamera != null ? viewCamera.transform.eulerAngles : transform.eulerAngles;
        return new Vector3(Mathf.DeltaAngle(0f, angles.x), Mathf.DeltaAngle(0f, angles.y), 0f);
    }

    // Short blend for the view switch instead of the brain's default blend.
    private CinemachineBlendDefinition GetViewBlend(
        ICinemachineCamera fromVcam, ICinemachineCamera toVcam,
        CinemachineBlendDefinition defaultBlend, Object owner)
    {
        if (firstPersonCamera != null
            && (ReferenceEquals(fromVcam, firstPersonCamera) || ReferenceEquals(toVcam, firstPersonCamera)))
        {
            return new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime);
        }
        return defaultBlend;
    }

    // ---------- Body visibility ----------

    private void HideBody()
    {
        if (!hideBody || hiddenRenderers.Count > 0)
        {
            return;
        }

        Transform weaponRoot = null;
        if (keepWeaponVisible)
        {
            WeaponController weapon = GetComponentInChildren<WeaponController>(true);
            weaponRoot = weapon != null ? weapon.transform : null;
        }

        foreach (Renderer bodyRenderer in GetComponentsInChildren<Renderer>(true))
        {
            // effects stay visible
            if (bodyRenderer is ParticleSystemRenderer || bodyRenderer is TrailRenderer || bodyRenderer is LineRenderer)
            {
                continue;
            }
            if (weaponRoot != null && bodyRenderer.transform.IsChildOf(weaponRoot))
            {
                continue;
            }

            hiddenRenderers.Add(bodyRenderer);
            hiddenRendererModes.Add(bodyRenderer.shadowCastingMode);
            bodyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    private void ShowBody()
    {
        for (int i = 0; i < hiddenRenderers.Count; i++)
        {
            if (hiddenRenderers[i] != null)
            {
                hiddenRenderers[i].shadowCastingMode = hiddenRendererModes[i];
            }
        }
        hiddenRenderers.Clear();
        hiddenRendererModes.Clear();
    }
}
