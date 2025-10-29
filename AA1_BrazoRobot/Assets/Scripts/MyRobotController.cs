using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 

[RequireComponent(typeof(RobotSequenceAnimator))]
public class MyRobotController : MonoBehaviour
{
    
    // Esta variable 'sobrevive' al reinicio de la escena.
    public static bool startSequenceOnLoad = false;

   
    [Header("Articulaciones (Pivotes)")]
    [SerializeField] private Transform joint_0_Base;
    [SerializeField] private Transform joint_1_Shoulder;
    [SerializeField] private Transform joint_2_Elbow;
    [SerializeField] private Transform joint_3_Wrist;
    [SerializeField] private Transform joint_4_MiniElbow;

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

  
    public bool isBusy { get; private set; } = false;

    private GameObject heldObject = null;
    private RobotSequenceAnimator sequenceAnimator;

    
    private float baseAngleY = 0f;
    private float shoulderAngleX = 0f;
    private float elbowAngleX = 0f;
    private float wristAngleY = 0f;
    private float miniElbowAngleX = 0f;


    void Awake()
    {
        sequenceAnimator = GetComponent<RobotSequenceAnimator>();
    }

    void Update()
    {
        
        // Si aprietas '2', activa la bandera estática y reinicia la escena
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Bandera 'Start On Load' activada. Reiniciando escena...");
            startSequenceOnLoad = true; // Activa la bandera
            ResetScene();
            return;
        }

        // Si aprietas '1', reinicia la escena pero SIN la bandera (modo manual)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Reiniciando escena a Modo Manual...");
            startSequenceOnLoad = false; // Asegúrate de que la bandera esté apagada
            ResetScene();
            return;
        }

        if (isBusy) return;

        // Si no se está reiniciando, funciona en modo manual
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
            // 'P' sigue funcionando para lanzar la anim manual
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
        ApplyAllRotations();
    }

    private void ApplyAllRotations()
    {
        shoulderAngleX = Mathf.Clamp(shoulderAngleX, shoulder_MinX, shoulder_MaxX);
        elbowAngleX = Mathf.Clamp(elbowAngleX, elbow_MinX, elbow_MaxX);
        wristAngleY = Mathf.Clamp(wristAngleY, wrist_MinY, wrist_MaxY);
        miniElbowAngleX = Mathf.Clamp(miniElbowAngleX, miniElbow_MinX, miniElbow_MaxX);
        joint_0_Base.localRotation = Quaternion.Euler(0, baseAngleY, 0);
        joint_1_Shoulder.localRotation = Quaternion.Euler(shoulderAngleX, 0, 0);
        joint_2_Elbow.localRotation = Quaternion.Euler(elbowAngleX, 0, 0);
        joint_3_Wrist.localRotation = Quaternion.Euler(0, wristAngleY, 0);
        joint_4_MiniElbow.localRotation = Quaternion.Euler(miniElbowAngleX, 0, 0);
    }

    private void ResetScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    

    public IEnumerator ResetArm()
    {
        if (isBusy) yield break;
        isBusy = true;
        Debug.Log("Reseteando brazo...");
        yield return StartCoroutine(MoveToPose(0, 0, 0, 0, 0, 1.0f));
        isBusy = false;
        Debug.Log("Brazo reseteado.");
    }

    public IEnumerator MoveToPose(float[] angles, float duration)
    {
        yield return StartCoroutine(MoveToPose(angles[0], angles[1], angles[2], angles[3], angles[4], duration));
    }

    public IEnumerator MoveToPose(float b, float s, float e, float w, float m, float duration)
    {
        if (isBusy) yield break;
        isBusy = true;
        float startB = baseAngleY; float startS = shoulderAngleX; float startE = elbowAngleX;
        float startW = wristAngleY; float startM = miniElbowAngleX;
        float time = 0;
        while (time < duration)
        {
            float t = time / duration; t = t * t * (3f - 2f * t);
            baseAngleY = Mathf.Lerp(startB, b, t);
            shoulderAngleX = Mathf.Lerp(startS, s, t);
            elbowAngleX = Mathf.Lerp(startE, e, t);
            wristAngleY = Mathf.Lerp(startW, w, t);
            miniElbowAngleX = Mathf.Lerp(startM, m, t);
            ApplyAllRotations();
            time += Time.deltaTime * animationMoveSpeed;
            yield return null;
        }
        baseAngleY = b; shoulderAngleX = s; elbowAngleX = e; wristAngleY = w; miniElbowAngleX = m;
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