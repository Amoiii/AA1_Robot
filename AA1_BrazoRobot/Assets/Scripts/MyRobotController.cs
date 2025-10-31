using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RobotSequenceAnimator))]
public class MyRobotController : MonoBehaviour
{
    public static bool startSequenceOnLoad = false;

    [Header("Articulaciones (Pivotes)")]
    [SerializeField] private Transform joint_0_Base;
    [SerializeField] private Transform joint_1_Shoulder;
    [SerializeField] private Transform joint_2_Elbow;
    [SerializeField] private Transform joint_3_Wrist;
    [SerializeField] private Transform joint_4_MiniElbow;
    [SerializeField] private Transform joint_5_GripperRotate; 

    [Header("Punto Final (End Effector)")]
    [SerializeField] private Transform endEffectorTarget;

    [Header("Sistema de Agarre")]
    [SerializeField] private Transform gripPoint;
    [SerializeField] private float grabRadius = 0.5f;
    [SerializeField] private LayerMask grabbableLayer;

    [Header("Parámetros de Movimiento")]
    [SerializeField] private float manualRotationSpeed = 50.0f;
    [SerializeField] private float animationMoveSpeed = 1.0f;

    [Header("Límites de Articulaciones (Grados)")]
    [SerializeField] private float shoulder_MinX = -90.0f;
    [SerializeField] private float shoulder_MaxX = 90.0f;
    [SerializeField] private float elbow_MinX = 0.0f;
    [SerializeField] private float elbow_MaxX = 150.0f;
    [SerializeField] private float wrist_MinY = -180.0f;
    [SerializeField] private float wrist_MaxY = 180.0f;
    [SerializeField] private float miniElbow_MinX = -45.0f;
    [SerializeField] private float miniElbow_MaxX = 90.0f;
    [SerializeField] private float gripper_MinY = -180.0f; 
    [SerializeField] private float gripper_MaxY = 180.0f;

    public bool isBusy { get; private set; } = false;
    private GameObject heldObject = null;
    private RobotSequenceAnimator sequenceAnimator;

    // 6 Ángulos de estado
    private float baseAngleY = 0f;
    private float shoulderAngleX = 0f;
    private float elbowAngleX = 0f;
    private float wristAngleY = 0f;
    private float miniElbowAngleX = 0f;
    private float gripperAngleY = 0f; 


    void Awake()
    {
        sequenceAnimator = GetComponent<RobotSequenceAnimator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            startSequenceOnLoad = true;
            ResetScene();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            startSequenceOnLoad = false;
            ResetScene();
            return;
        }
        if (isBusy) return;
        ControlManual();
        HandleActionInput();
    }

    private void HandleActionInput()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (heldObject == null) { TryGrabObject(); }
            else { ReleaseObject(); }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(DodgeAnimation());
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            sequenceAnimator.StartFullSequence();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(ResetArm());
        }
    }

    private void ControlManual()
    {
        if (Input.GetKey(KeyCode.A)) baseAngleY -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) baseAngleY += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.W)) shoulderAngleX += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) shoulderAngleX -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) elbowAngleX += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) elbowAngleX -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Z)) wristAngleY -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.C)) wristAngleY += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.R)) miniElbowAngleX += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.F)) miniElbowAngleX -= manualRotationSpeed * Time.deltaTime;

        // --- NUEVAS TECLAS ---
        if (Input.GetKey(KeyCode.T)) gripperAngleY += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Y)) gripperAngleY -= manualRotationSpeed * Time.deltaTime;

        ApplyAllRotations();
    }

    private void ApplyAllRotations()
    {
        shoulderAngleX = Mathf.Clamp(shoulderAngleX, shoulder_MinX, shoulder_MaxX);
        elbowAngleX = Mathf.Clamp(elbowAngleX, elbow_MinX, elbow_MaxX);
        wristAngleY = Mathf.Clamp(wristAngleY, wrist_MinY, wrist_MaxY);
        miniElbowAngleX = Mathf.Clamp(miniElbowAngleX, miniElbow_MinX, miniElbow_MaxX);
        gripperAngleY = Mathf.Clamp(gripperAngleY, gripper_MinY, gripper_MaxY); 

        joint_0_Base.localRotation = Quaternion.Euler(0, baseAngleY, 0);
        joint_1_Shoulder.localRotation = Quaternion.Euler(shoulderAngleX, 0, 0);
        joint_2_Elbow.localRotation = Quaternion.Euler(elbowAngleX, 0, 0);
        joint_3_Wrist.localRotation = Quaternion.Euler(0, wristAngleY, 0);
        joint_4_MiniElbow.localRotation = Quaternion.Euler(miniElbowAngleX, 0, 0);
        joint_5_GripperRotate.localRotation = Quaternion.Euler(0, gripperAngleY, 0);
    }

    private void ResetScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

   

    public IEnumerator ResetArm()
    {
        isBusy = true;
        Debug.Log("Reseteando brazo...");
        
        yield return StartCoroutine(MoveToPose(0, 0, 0, 0, 0, 0, 1.0f));
        isBusy = false;
        Debug.Log("Brazo reseteado.");
    }

    public IEnumerator MoveToPose(float[] angles, float duration)
    {
        // Espera un array de 6 ángulos
        yield return StartCoroutine(MoveToPose(angles[0], angles[1], angles[2], angles[3], angles[4], angles[5], duration));
    }

    public IEnumerator MoveToPose(float b, float s, float e, float w, float m, float g, float duration)
    {
        isBusy = true;

        float startB = baseAngleY, startS = shoulderAngleX, startE = elbowAngleX;
        float startW = wristAngleY, startM = miniElbowAngleX, startG = gripperAngleY; 
        float time = 0;

        while (time < duration)
        {
            float t = time / duration; t = t * t * (3f - 2f * t); // SmoothStep
            baseAngleY = Mathf.Lerp(startB, b, t);
            shoulderAngleX = Mathf.Lerp(startS, s, t);
            elbowAngleX = Mathf.Lerp(startE, e, t);
            wristAngleY = Mathf.Lerp(startW, w, t);
            miniElbowAngleX = Mathf.Lerp(startM, m, t);
            gripperAngleY = Mathf.Lerp(startG, g, t); 

            ApplyAllRotations();
            time += Time.deltaTime * animationMoveSpeed;
            yield return null;
        }
        baseAngleY = b; shoulderAngleX = s; elbowAngleX = e;
        wristAngleY = w; miniElbowAngleX = m; gripperAngleY = g;
        ApplyAllRotations();
        isBusy = false;
    }

    private void TryGrabObject()
    {
        Collider[] colliders = Physics.OverlapSphere(endEffectorTarget.position, grabRadius, grabbableLayer);
        if (colliders.Length > 0)
        {
            ForceGrab(colliders[0].gameObject);
        }
    }

    public void ForceGrab(GameObject objToGrab)
    {
        if (heldObject != null) ReleaseObject();
        heldObject = objToGrab;
        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        heldObject.transform.SetParent(gripPoint);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
        Debug.Log("Objeto agarrado: " + heldObject.name);
    }

    public void ReleaseObject()
    {
        if (heldObject == null) return;
        Debug.Log("Objeto soltado: " + heldObject.name);
        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        heldObject.transform.SetParent(null);
        heldObject = null;
    }

    private IEnumerator DodgeAnimation()
    {
        if (isBusy) yield break;
        isBusy = true;

        float duration = 0.5f; float elapsedTime = 0f;
        Quaternion originalShoulderRot = joint_1_Shoulder.localRotation;
        Quaternion originalElbowRot = joint_2_Elbow.localRotation;
        Quaternion targetShoulderRot = originalShoulderRot * Quaternion.Euler(45f, 0, 0);
        Quaternion targetElbowRot = originalElbowRot * Quaternion.Euler(-30f, 0, 0);

        while (elapsedTime < duration) { float t = elapsedTime / duration; joint_1_Shoulder.localRotation = Quaternion.Slerp(originalShoulderRot, targetShoulderRot, t); joint_2_Elbow.localRotation = Quaternion.Slerp(originalElbowRot, targetElbowRot, t); elapsedTime += Time.deltaTime; yield return null; }
        joint_1_Shoulder.localRotation = targetShoulderRot; joint_2_Elbow.localRotation = targetElbowRot; elapsedTime = 0f;
        while (elapsedTime < duration) { float t = elapsedTime / duration; joint_1_Shoulder.localRotation = Quaternion.Slerp(targetShoulderRot, originalShoulderRot, t); joint_2_Elbow.localRotation = Quaternion.Slerp(targetElbowRot, originalElbowRot, t); elapsedTime += Time.deltaTime; yield return null; }

        joint_1_Shoulder.localRotation = originalShoulderRot; joint_2_Elbow.localRotation = originalElbowRot;
        shoulderAngleX = NormalizeAngle(originalShoulderRot.eulerAngles.x); elbowAngleX = NormalizeAngle(originalElbowRot.eulerAngles.x);

        isBusy = false;
    }

    private float NormalizeAngle(float angle) { if (angle > 180) angle -= 360; return angle; }
    public void GrabObject(GameObject objectToGrab) { Debug.Log("GrabObject público llamado"); TryGrabObject(); }
    public void MoveToTarget(Vector3 targetPosition, Quaternion targetRotation) { Debug.Log("MoveToTarget público llamado"); }
}