using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RobotSequenceAnimator))]
public class MyRobotController : MonoBehaviour
{
    public static bool startSequenceOnLoad = false;

    // Joints
    [SerializeField] private Transform joint_0_Base;
    [SerializeField] private Transform joint_1_Shoulder;
    [SerializeField] private Transform joint_2_Elbow;
    [SerializeField] private Transform joint_3_Wrist;
    [SerializeField] private Transform joint_4_MiniElbow;
    [SerializeField] private Transform joint_5_GripperRotate;

    // End effector ref
    [SerializeField] private Transform endEffectorTarget;

    // Grab
    [SerializeField] private Transform gripPoint;
    [SerializeField] private float grabRadius = 0.5f;
    [SerializeField] private LayerMask grabbableLayer;

    // Velocidades
    [SerializeField] private float manualRotationSpeed = 50.0f;
    [SerializeField] private float animationMoveSpeed = 1.0f;

    // Límites por eje
     private float base_MinY = -180f;
     private float base_MaxY = 180f;
     private float shoulder_MinX = -90.0f;
    private float shoulder_MaxX = 90.0f;
     private float elbow_MinX = 0.0f;
     private float elbow_MaxX = 150.0f;
     private float wrist_MinY = -180.0f;
     private float wrist_MaxY = 180.0f;
    private float miniElbow_MinX = -45.0f;
     private float miniElbow_MaxX = 90.0f;
     private float gripper_MinY = -180.0f;
   private float gripper_MaxY = 180.0f;

    // Evitación por software
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float linkRadius = 0.08f;
    [SerializeField] private float effectorProbeRadius = 0.12f;
    [SerializeField] private float safeHeight = 1.2f;
    [SerializeField] private int probeSteps = 10;
    [SerializeField] private bool blockManualOnCollision = true;

    public bool isBusy { get; private set; } = false;
    private GameObject heldObject = null;
    private RobotSequenceAnimator sequenceAnimator;

    // Estado (FK)
    private float baseAngleY = 0f;
    private float shoulderAngleX = 0f;
    private float elbowAngleX = 0f;
    private float wristAngleY = 0f;
    private float miniElbowAngleX = 0f;
    private float gripperAngleY = 0f;

    void Awake() => sequenceAnimator = GetComponent<RobotSequenceAnimator>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2)) { startSequenceOnLoad = true; ResetScene(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha1)) { startSequenceOnLoad = false; ResetScene(); return; }
        if (isBusy) return;

        ControlManual();
        HandleActionInput();
    }

    private void HandleActionInput()
    {
        if (Input.GetKeyDown(KeyCode.G)) { if (heldObject == null) TryGrabObject(); else ReleaseObject(); }
        if (Input.GetKeyDown(KeyCode.P)) StartCoroutine(ResetArm());
    }

    // Input → aplica si no colisiona (revirtiendo si hace falta)
    private void ControlManual()
    {
        float b = baseAngleY, s = shoulderAngleX, e = elbowAngleX, w = wristAngleY, m = miniElbowAngleX, g = gripperAngleY;

        if (Input.GetKey(KeyCode.A)) b -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) b += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.W)) s += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) s -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) e += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) e -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Z)) w -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.C)) w += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.R)) m += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.F)) m -= manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.T)) g += manualRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Y)) g -= manualRotationSpeed * Time.deltaTime;

        TryApplyAnglesSafely(b, s, e, w, m, g);
    }

    private void TryApplyAnglesSafely(float b, float s, float e, float w, float m, float g)
    {
        float pb = baseAngleY, ps = shoulderAngleX, pe = elbowAngleX, pw = wristAngleY, pm = miniElbowAngleX, pg = gripperAngleY;
        Vector3 effPrev = endEffectorTarget ? endEffectorTarget.position : Vector3.zero;

        baseAngleY = b; shoulderAngleX = s; elbowAngleX = e; wristAngleY = w; miniElbowAngleX = m; gripperAngleY = g;
        ApplyAllRotations();

        if (!blockManualOnCollision) return;

        bool clearChain = ChainClear();
        bool clearSweep = endEffectorTarget ? EffectorPathClear(effPrev, endEffectorTarget.position) : true;

        if (!clearChain || !clearSweep)
        {
            baseAngleY = pb; shoulderAngleX = ps; elbowAngleX = pe; wristAngleY = pw; miniElbowAngleX = pm; gripperAngleY = pg;
            ApplyAllRotations();
        }
    }

    private void ApplyAllRotations()
    {
        baseAngleY = Mathf.Clamp(baseAngleY, base_MinY, base_MaxY);
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
        yield return StartCoroutine(MoveToPose(0, 0, 0, 0, 0, 0, 1.0f));
        isBusy = false;
    }

    // Interpoladores
    public IEnumerator MoveToPose(float[] angles, float duration)
    {
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
            float t = time / duration; t = t * t * (3f - 2f * t);
            baseAngleY = Mathf.LerpAngle(startB, b, t);
            shoulderAngleX = Mathf.Lerp(startS, s, t);
            elbowAngleX = Mathf.Lerp(startE, e, t);
            wristAngleY = Mathf.LerpAngle(startW, w, t);
            miniElbowAngleX = Mathf.Lerp(startM, m, t);
            gripperAngleY = Mathf.LerpAngle(startG, g, t);

            ApplyAllRotations();
            time += Time.deltaTime * animationMoveSpeed;
            yield return null;
        }
        baseAngleY = b; shoulderAngleX = s; elbowAngleX = e;
        wristAngleY = w; miniElbowAngleX = m; gripperAngleY = g;
        ApplyAllRotations();
        isBusy = false;
    }

    // Cortar si hay coolision (no sigue
    public IEnumerator MoveToPoseSafe(float b, float s, float e, float w, float m, float g, float duration)
    {
        isBusy = true;

        float sb = baseAngleY, ss = shoulderAngleX, se = elbowAngleX, sw = wristAngleY, sm = miniElbowAngleX, sg = gripperAngleY;
        int steps = Mathf.Max(probeSteps, 3);
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps; t = t * t * (3f - 2f * t);
            baseAngleY = Mathf.LerpAngle(sb, b, t);
            shoulderAngleX = Mathf.Lerp(ss, s, t);
            elbowAngleX = Mathf.Lerp(se, e, t);
            wristAngleY = Mathf.LerpAngle(sw, w, t);
            miniElbowAngleX = Mathf.Lerp(sm, m, t);
            gripperAngleY = Mathf.LerpAngle(sg, g, t);

            ApplyAllRotations();

            if (!ChainClear()) { isBusy = false; yield break; }
            yield return new WaitForSeconds(duration / (steps * animationMoveSpeed));
        }
        isBusy = false;
    }

    // Grab
    private void TryGrabObject()
    {
        Collider[] colliders = Physics.OverlapSphere(endEffectorTarget.position, grabRadius, grabbableLayer);
        if (colliders.Length > 0) ForceGrab(colliders[0].gameObject);
    }

    public void ForceGrab(GameObject objToGrab)
    {
        if (heldObject != null) ReleaseObject();
        heldObject = objToGrab;
        if (heldObject.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        heldObject.transform.SetParent(gripPoint);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
    }

    public void ReleaseObject()
    {
        if (heldObject == null) return;
        if (heldObject.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
        heldObject.transform.SetParent(null, true);
        heldObject = null;
    }

    // Waypoints altos + barridos del efector
    public void MoveToTarget(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (isBusy) return;
        StartCoroutine(MoveToTargetRoutine(targetPosition, targetRotation));
    }

    private IEnumerator MoveToTargetRoutine(Vector3 targetPos, Quaternion targetRot)
    {
        isBusy = true;

        float[] poseUp = { baseAngleY, 10f, 20f, wristAngleY, miniElbowAngleX, gripperAngleY };
        yield return StartCoroutine(MoveToPoseSafe(poseUp[0], poseUp[1], poseUp[2], poseUp[3], poseUp[4], poseUp[5], 0.8f));

        Vector3 flatDir = new Vector3(targetPos.x - transform.position.x, 0f, targetPos.z - transform.position.z);
        float targetBaseY = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;
        float[] poseTurn = { targetBaseY, shoulderAngleX, elbowAngleX, wristAngleY, miniElbowAngleX, gripperAngleY };
        yield return StartCoroutine(MoveToPoseSafe(poseTurn[0], poseTurn[1], poseTurn[2], poseTurn[3], poseTurn[4], poseTurn[5], 0.6f));

        Vector3 hover = new Vector3(targetPos.x, targetPos.y + safeHeight, targetPos.z);
        Vector3 start = endEffectorTarget.position;
        if (!EffectorPathClear(start, hover)) { isBusy = false; yield break; }

        int steps = Mathf.Max(probeSteps, 5);
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 p = Vector3.Lerp(start, hover, t);
            if (!EffectorPathClear(endEffectorTarget.position, p)) { isBusy = false; yield break; }
            yield return null;
        }

        Vector3 descendStart = endEffectorTarget.position;
        if (!EffectorPathClear(descendStart, targetPos)) { isBusy = false; yield break; }

        isBusy = false;
    }

    // Colisiones: barrido efector + cápsulas por eslabón
    private bool EffectorPathClear(Vector3 a, Vector3 b)
    {
        Vector3 dir = (b - a);
        float dist = dir.magnitude;
        if (dist <= 1e-4f) return true;
        dir /= dist;
        return !Physics.SphereCast(a, effectorProbeRadius, dir, out _, dist, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    private bool SegmentClear(Transform tA, Transform tB)
    {
        Vector3 a = tA.position, b = tB.position;
        if (Vector3.Distance(a, b) <= 1e-4f) return true;
        Collider[] hits = Physics.OverlapCapsule(a, b, linkRadius, obstacleLayer, QueryTriggerInteraction.Ignore);
        return hits == null || hits.Length == 0;
    }

    private bool ChainClear()
    {
        return
            SegmentClear(joint_0_Base, joint_1_Shoulder) &&
            SegmentClear(joint_1_Shoulder, joint_2_Elbow) &&
            SegmentClear(joint_2_Elbow, joint_3_Wrist) &&
            SegmentClear(joint_3_Wrist, joint_4_MiniElbow) &&
            SegmentClear(joint_4_MiniElbow, joint_5_GripperRotate);
    }

    void OnDrawGizmosSelected()
    {
        if (endEffectorTarget == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(endEffectorTarget.position, grabRadius);
    }
}
