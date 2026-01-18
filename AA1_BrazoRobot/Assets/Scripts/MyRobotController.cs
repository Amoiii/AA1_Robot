using UnityEngine;
using System.Collections;

public class MyRobotController : MonoBehaviour
{
    [Header("1. Articulaciones")]
    public Transform joint_0_Base;
    public Transform joint_1_Shoulder;
    public Transform joint_2_Elbow;
    public Transform joint_3_Wrist;
    public Transform joint_4_MiniElbow;
    public Transform joint_5_GripperRotate;

    [Header("2. Referencias")]
    public Transform endEffectorTarget;
    public Transform gripPoint;

    [Header("3. Configuración")]
    public LayerMask obstacleLayer;
    public LayerMask grabbableLayer;
    public float grabRadius = 0.5f;
    public float manualRotationSpeed = 50.0f;
    public float animationMoveSpeed = 2.0f;

    // Seguridad
    public bool blockManualOnCollision = true;

    // Estado
    public bool isBusy { get; private set; } = false;
    public bool manualMode = true;
    private GameObject heldObject = null;

    // Ángulos internos (FK State)
    private float baseAngleY, shoulderAngleX, elbowAngleX, wristAngleY, miniElbowAngleX, gripperAngleY;

    void Awake() => SyncJoints();

    void Update()
    {
        // Selector de Modos
        if (Input.GetKeyDown(KeyCode.Alpha1)) { manualMode = true; StopAllCoroutines(); isBusy = false; Debug.Log("Modo MANUAL"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { manualMode = false; } // El Animator gestiona el resto
        if (Input.GetKeyDown(KeyCode.P)) StartCoroutine(ResetArm());

        // Modo Manual (Solo si no está en secuencia automática)
        if (manualMode && !isBusy)
        {
            ControlManual();
            HandleActionInput();
        }
    }

    private void HandleActionInput()
    {
        if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Space))
        {
            if (heldObject == null) TryGrabObject(); else ReleaseObject();
        }
    }

    // --- 1. CONTROL MANUAL (Lógica propia) ---
    private void ControlManual()
    {
        float dt = manualRotationSpeed * Time.deltaTime;
        float b = baseAngleY, s = shoulderAngleX, e = elbowAngleX;
        float w = wristAngleY, m = miniElbowAngleX, g = gripperAngleY;

        // Inputs
        if (Input.GetKey(KeyCode.A)) b -= dt;
        if (Input.GetKey(KeyCode.D)) b += dt;
        if (Input.GetKey(KeyCode.W)) s -= dt;
        if (Input.GetKey(KeyCode.S)) s += dt;
        if (Input.GetKey(KeyCode.Q)) e += dt;
        if (Input.GetKey(KeyCode.E)) e -= dt;
        if (Input.GetKey(KeyCode.Z)) w -= dt;
        if (Input.GetKey(KeyCode.C)) w += dt;
        if (Input.GetKey(KeyCode.R)) m += dt;
        if (Input.GetKey(KeyCode.F)) m -= dt;
        if (Input.GetKey(KeyCode.T)) g += dt;
        if (Input.GetKey(KeyCode.Y)) g -= dt;

        // Limites usando TU LIBRERÍA
        s = MyMath.Clamp(s, -100, 100);
        e = MyMath.Clamp(e, -10, 160);

        TryApplyAnglesSafely(b, s, e, w, m, g);
    }

    private void TryApplyAnglesSafely(float b, float s, float e, float w, float m, float g)
    {
        float oldB = baseAngleY, oldS = shoulderAngleX, oldE = elbowAngleX;
        float oldW = wristAngleY, oldM = miniElbowAngleX, oldG = gripperAngleY;

        baseAngleY = b; shoulderAngleX = s; elbowAngleX = e;
        wristAngleY = w; miniElbowAngleX = m; gripperAngleY = g;
        ApplyAllRotations();

        if (blockManualOnCollision && CheckCollisionInternal())
        {
            // Revertir si choca
            baseAngleY = oldB; shoulderAngleX = oldS; elbowAngleX = oldE;
            wristAngleY = oldW; miniElbowAngleX = oldM; gripperAngleY = oldG;
            ApplyAllRotations();
        }
    }

    // --- 2. AUTOMÁTICO CON MATH PROPIO (Evasión Grúa) ---
    public void MoveToTarget(Vector3 unityTargetPos)
    {
        // Convertimos el vector de Unity a TU vector para los cálculos
        MyVec3 target = MyVec3.FromUnity(unityTargetPos);
        StartCoroutine(MoveToTargetRoutine(target));
    }

    private IEnumerator MoveToTargetRoutine(MyVec3 targetPos)
    {
        isBusy = true;
        SyncJoints();

        // 1. CÁLCULOS CON TU LIBRERÍA
        MyVec3 currentPos = MyVec3.FromUnity(transform.position); // Posición base del robot
        MyVec3 dir = targetPos - currentPos;

        // Atan2 propio para el ángulo de la base
        float targetBase = MyMath.Atan2(dir.x, dir.z) * MyMath.Rad2Deg;

        // Distancia propia
        float dist = MyVec3.Distance(currentPos, targetPos);

        // Heurística FK (Simple aproximación basada en distancia)
        float targetShoulder = MyMath.Clamp(dist * 10f, 0, 45);
        float targetElbow = MyMath.Clamp(dist * 5f, 20, 80);

        // 2. DETECCIÓN DE PARED (Aquí necesitamos Unity Physics, convertimos de vuelta)
        // Usamos Linecast porque es preciso para "mirar" si hay algo en medio
        bool hayMuro = Physics.Linecast(endEffectorTarget.position, targetPos.ToUnity(), obstacleLayer);

        if (hayMuro)
        {
            Debug.LogWarning("¡MURO DETECTADO! Usando Lógica Grúa (-90º).");

            // PASO A: SUBIR (Grúa) - Hombro vertical (-90)
            float[] poseCielo = { baseAngleY, -90f, 0f, 0f, 0f, gripperAngleY };
            yield return StartCoroutine(MoveToPose(poseCielo, 1.0f));

            // PASO B: GIRAR ARRIBA
            float[] poseGiro = { targetBase, -90f, 0f, 0f, 0f, gripperAngleY };
            yield return StartCoroutine(MoveToPose(poseGiro, 1.5f));

            // PASO C: BAJAR
            float[] poseFinal = { targetBase, targetShoulder, targetElbow, 0f, 40f, gripperAngleY };
            yield return StartCoroutine(MoveToPose(poseFinal, 1.0f));
        }
        else
        {
            Debug.Log("Camino libre. Trayectoria directa.");
            float[] poseDirecta = { targetBase, targetShoulder, targetElbow, 0f, 40f, gripperAngleY };
            yield return StartCoroutine(MoveToPose(poseDirecta, 1.5f));
        }

        isBusy = false;
    }

    // --- INTERPOLACIÓN CON TU LIBRERÍA ---
    public IEnumerator MoveToPose(float[] target, float duration)
    {
        float t = 0;
        float[] start = { baseAngleY, shoulderAngleX, elbowAngleX, wristAngleY, miniElbowAngleX, gripperAngleY };

        while (t < 1)
        {
            t += Time.deltaTime * animationMoveSpeed / duration;
            // Smoothstep propio: t*t*(3 - 2*t)
            float k = t * t * (3f - 2f * t);

            // Usamos MyMath.LerpAngle y MyMath.Lerp
            baseAngleY = MyMath.LerpAngle(start[0], target[0], k);
            shoulderAngleX = MyMath.LerpAngle(start[1], target[1], k);
            elbowAngleX = MyMath.LerpAngle(start[2], target[2], k);
            wristAngleY = MyMath.LerpAngle(start[3], target[3], k);
            miniElbowAngleX = MyMath.LerpAngle(start[4], target[4], k);
            gripperAngleY = MyMath.LerpAngle(start[5], target[5], k);

            ApplyAllRotations();
            yield return null;
        }

        // Finalizar exacto
        baseAngleY = target[0]; shoulderAngleX = target[1]; elbowAngleX = target[2];
        wristAngleY = target[3]; miniElbowAngleX = target[4]; gripperAngleY = target[5];
        ApplyAllRotations();
    }

    // --- UTILIDADES ---
    public IEnumerator ResetArm()
    {
        float[] home = { 0, 0, 0, 0, 0, 0 };
        yield return StartCoroutine(MoveToPose(home, 1.0f));
    }

    private void SyncJoints()
    {
        if (joint_0_Base) baseAngleY = joint_0_Base.localEulerAngles.y;
        if (joint_1_Shoulder) shoulderAngleX = FixAngle(joint_1_Shoulder.localEulerAngles.x);
        if (joint_2_Elbow) elbowAngleX = FixAngle(joint_2_Elbow.localEulerAngles.x);
    }
    private float FixAngle(float a) => a > 180 ? a - 360 : a;

    private void ApplyAllRotations()
    {
        // Al final, Unity necesita Vector3 nativos para los Transforms
        if (joint_0_Base) joint_0_Base.localEulerAngles = new Vector3(0, baseAngleY, 0);
        if (joint_1_Shoulder) joint_1_Shoulder.localEulerAngles = new Vector3(shoulderAngleX, 0, 0);
        if (joint_2_Elbow) joint_2_Elbow.localEulerAngles = new Vector3(elbowAngleX, 0, 0);
        if (joint_3_Wrist) joint_3_Wrist.localEulerAngles = new Vector3(0, wristAngleY, 0);
        if (joint_4_MiniElbow) joint_4_MiniElbow.localEulerAngles = new Vector3(miniElbowAngleX, 0, 0);
        if (joint_5_GripperRotate) joint_5_GripperRotate.localEulerAngles = new Vector3(0, gripperAngleY, 0);
    }

    // --- AGARRE Y FÍSICA ---
    private void TryGrabObject()
    {
        // Usamos Physics nativo de Unity para colisiones (esto es correcto)
        Collider[] hits = Physics.OverlapSphere(endEffectorTarget.position, grabRadius, grabbableLayer);
        if (hits.Length > 0) ForceGrab(hits[0].gameObject);
    }

    public void ForceGrab(GameObject obj)
    {
        heldObject = obj;
        var rb = obj.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
        obj.transform.SetParent(gripPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    public void ReleaseObject()
    {
        if (!heldObject) return;
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;
        heldObject.transform.SetParent(null);
        heldObject = null;
    }

    private bool CheckCollisionInternal()
    {
        if (Hit(joint_0_Base, joint_1_Shoulder)) return true;
        if (Hit(joint_1_Shoulder, joint_2_Elbow)) return true;
        if (Hit(joint_2_Elbow, joint_3_Wrist)) return true;
        if (Physics.CheckSphere(endEffectorTarget.position, 0.15f, obstacleLayer)) return true;
        return false;
    }
    private bool Hit(Transform a, Transform b) => Physics.CheckCapsule(a.position, b.position, 0.1f, obstacleLayer);
}