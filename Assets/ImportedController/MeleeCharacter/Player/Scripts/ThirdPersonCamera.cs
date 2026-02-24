using System;
using System.Collections;
using Input;
using Player;
using UnityEngine;

//using DG.Tweening;

public class ThirdPersonCamera : MonoBehaviour
{
    public delegate void StopLockOn();
    public static event StopLockOn OnStopLockOn;
    
    [Header("Camera Settings")]
    [SerializeField] private PlayerController player;
    [SerializeField] private float stationaryDistanceFromTarget = 3f;
    [SerializeField] private float movingDistanceFromTarget = 3.2f;
    [SerializeField] private float heightOffset = 1.5f;
    [SerializeField] private float distanceSmoothTime = 0.6f;
    [SerializeField] private Vector2 rotationXMinMax = new Vector2(-7, 40);

    [Header("Camera Lock On Settings")]
    [SerializeField] public float lockOnDistance = 15f;
    [SerializeField] public LayerMask lockOnLayerMask;
    [SerializeField] private Transform lockOnTarget;
    [SerializeField] private ScriptableFloatCurve lockOnTargetCurve;
    [SerializeField] private ScriptableFloatCurve lockOnOffsetCurve;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask cameraCollisionMask;
    [SerializeField] private float cameraCollisionRadius = 0.25f;
    [SerializeField] private float cameraCollisionOffset = 0.2f;
    [SerializeField] private float minCameraDistance = 0.5f;


    [Header("Camera rotation curve, create a new one and assign it her to play around with it!")]
    [SerializeField] ScriptableFloatCurve smoothCurve;
    [Header("Options for camera speeds will be saved here if we implement that")]
    [SerializeField] private ScriptablePlayerPrefsKeys playerPrefsKeys;

    private float m_lockOnHorizontalOffset = 0f;
    private Transform m_lookAtTarget;
    [HideInInspector]public float m_cameraRotSensitivity = 0.151f;
    private float m_rotationY;
    private float m_rotationX;
    private Vector3 m_currentRotation;
    private Vector3 m_smoothVelocity = Vector3.zero;
    private float m_distanceToTarget;
    private float m_targetDistanceToTarget;
    private float m_refVelocity;
    private CameraInputHandler  m_cameraInputHandler;
    private bool m_isDead = false;
    private bool m_isLockedOn = false;
    private float m_lerpedLockOnHorizontalOffset;
    private float m_targetHorizontalOffset;
    private float m_currentHorizontalOffset = 1f;
    private Vector3 m_lastPosition = Vector3.zero;
    
    private void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        m_cameraInputHandler = GetComponent<CameraInputHandler>();
        Cursor.visible = false;
        //UpdateCamRotSpeed();
        m_lookAtTarget = player.transform;
    }
    
    private void OnEnable()
    {
        PlayerController.OnLockOnModeUpdate += UpdateLockOn;
        //Do things on death or when settings change
        //PlayerData.OnPlayerDeath += OnDeath;
        //SetCamRotSpeed.OnCamRotSpeedChanged += UpdateCamRotSpeed;
    }

    private void OnDisable()
    {
        PlayerController.OnLockOnModeUpdate -= UpdateLockOn;
        //Do things on death or when settings change
        //PlayerData.OnPlayerDeath -= OnDeath;
        //SetCamRotSpeed.OnCamRotSpeedChanged -= UpdateCamRotSpeed;
    }

    private void UpdateLockOn(bool enabled, Transform target,ElementTypes.ElementType elementWeakness) //lessgåå
    {
        m_isLockedOn = enabled;
        
        if (enabled)
        {
            m_lookAtTarget = target;
            m_isLockedOn = enabled;
        }
        else
        {
            m_lookAtTarget = player.transform;
        }
    }
    
    private void OnDeath()
    {
        m_isDead = true;
    }

    private Vector3 HandleCameraCollision(Vector3 desiredPosition)
    {
        Vector3 targetPosition = player.transform.position + Vector3.up * heightOffset;
        Vector3 direction = (desiredPosition - targetPosition).normalized;
        float distance = Vector3.Distance(targetPosition, desiredPosition);

        if (Physics.SphereCast(
            targetPosition,
            cameraCollisionRadius,
            direction,
            out RaycastHit hit,
            distance,
            cameraCollisionMask))
        {
            float correctedDistance = Mathf.Max(hit.distance - cameraCollisionOffset, minCameraDistance);
            return targetPosition + direction * correctedDistance;
        }

        return desiredPosition;
    }

    private void UpdateCamRotSpeed()
    {
        if (!PlayerPrefs.HasKey(playerPrefsKeys.cameraRotationSpeedKey))
        {
            PlayerPrefs.SetFloat(playerPrefsKeys.cameraRotationSpeedKey, m_cameraRotSensitivity);
        }
        m_cameraRotSensitivity = PlayerPrefs.GetFloat(playerPrefsKeys.cameraRotationSpeedKey);
        print("updated cam rot speed??");
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
    
    void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }
        
        if (m_isDead)
        {
            m_currentRotation = Vector3.SmoothDamp(m_currentRotation, Vector3.zero, ref m_smoothVelocity, 0.1f);
      
            transform.localEulerAngles = m_currentRotation;
        }

        if (m_isLockedOn)
        {
            transform.LookAt(GetLockOnTransform());
            Vector3 euler = transform.eulerAngles;
            // Convert to -180 to 180 range
            euler.x = NormalizeAngle(euler.x);
            // Clamp pitch
            euler.x = Mathf.Clamp(euler.x, -10f, 15f);

            Quaternion lockOnRotation = Quaternion.Slerp(Quaternion.Euler(m_currentRotation),
                Quaternion.Euler(euler), Time.deltaTime * 15f);
            transform.rotation = lockOnRotation;

            m_currentRotation = transform.rotation.eulerAngles;
            m_rotationX = m_currentRotation.x;
            m_rotationY = m_currentRotation.y;
        }
        else
        {
            SetMouseRotation();
            transform.rotation = Quaternion.Euler(m_currentRotation);
        }
        
        float distanceMoveVelocity = player.moveVelocity.normalized.magnitude;
        
        if (distanceMoveVelocity > 0.01f)
        {
            m_targetDistanceToTarget = movingDistanceFromTarget;
        }
        else
        {
            m_targetDistanceToTarget = stationaryDistanceFromTarget;
        }
        

        m_distanceToTarget = Mathf.SmoothDamp(m_distanceToTarget,
            m_targetDistanceToTarget, ref m_refVelocity, distanceSmoothTime);

        Vector3 newPosition;
        if(m_isLockedOn)
        {
            m_currentHorizontalOffset = Mathf.SmoothDamp(m_currentHorizontalOffset,
                m_lerpedLockOnHorizontalOffset, ref m_refVelocity, distanceSmoothTime);
            
            newPosition = player.transform.position -
                transform.forward * m_distanceToTarget  + transform.up * heightOffset
                + transform.right * m_lerpedLockOnHorizontalOffset;
        }
        else
        {
            
            // Subtract the forward vector of the GameObject to point its forward vector to the target
            newPosition = player.transform.position -
                transform.forward * m_distanceToTarget + transform.up * heightOffset;
        }

        newPosition = HandleCameraCollision(newPosition);

        transform.position = newPosition;
    }

    private void SetMouseRotation()
    {
        float mouseX = m_cameraInputHandler.lookAction.ReadValue<Vector2>().x * m_cameraRotSensitivity;
        float mouseY = m_cameraInputHandler.lookAction.ReadValue<Vector2>().y * m_cameraRotSensitivity * -1f;

        m_rotationY += mouseX;
        m_rotationX += mouseY;

        // Apply clamping for x rotation 
        m_rotationX = Mathf.Clamp(m_rotationX, rotationXMinMax.x, rotationXMinMax.y);
        
        float evaluatedSmoothCurve = smoothCurve.floatCurve.Evaluate(m_cameraInputHandler.lookAction.ReadValue<Vector2>().magnitude);
        
        Vector3 nextRotation = new Vector3(m_rotationX, m_rotationY);

        // Apply damping between rotation changes
        m_currentRotation = Vector3.SmoothDamp(m_currentRotation, nextRotation, ref m_smoothVelocity, evaluatedSmoothCurve);
    }

    private Transform GetLockOnTransform()
    {
        float distance = Vector3.Distance(player.transform.position, m_lookAtTarget.position);
        float maxDistance = lockOnDistance;
        
        if(distance < maxDistance)
        {
            float alpha = distance / maxDistance;
            float evaluatedLockOnTargetCurve = lockOnTargetCurve.floatCurve.Evaluate(alpha);
            float evaluatedLockOnOffsetCurve = lockOnOffsetCurve.floatCurve.Evaluate(alpha);
            lockOnTarget.position = Vector3.Lerp(player.transform.position, m_lookAtTarget.position, evaluatedLockOnTargetCurve);
            m_lerpedLockOnHorizontalOffset = Mathf.Lerp(0f, m_lockOnHorizontalOffset, evaluatedLockOnOffsetCurve);
            return lockOnTarget;
        }
        
        if(distance > maxDistance)
        {
            OnStopLockOn?.Invoke();
        }
        
        return m_lookAtTarget;
    }
}