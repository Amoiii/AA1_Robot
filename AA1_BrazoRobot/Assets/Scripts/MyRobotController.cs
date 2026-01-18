using UnityEngine;
using System.Collections;

public class MyRobotController : MonoBehaviour
{
    [Header("1. ARRASTRA LOS JOINTS DEL ROBOT")]
    public Transform joint_0_Base;
    public Transform joint_1_Shoulder;
    public Transform joint_2_Elbow;
    public Transform joint_3_Wrist;
    public Transform joint_4_MiniElbow;
    public Transform joint_5_GripperRotate;

    [Header("2. ARRASTRA LOS OBJETIVOS")]
    public Transform endEffectorTarget; // La punta del robot (la mano)
    public Transform gripPoint;         // Donde se pega el objeto (hijo de la mano)

    [Header("3. CONFIGURACIÓN IMPORTANTE")]
    public LayerMask obstacleLayer;     // <--- ¡PON ESTO EN 'OBSTACLE' EN EL INSPECTOR!
    public float moveSpeed = 2.0f;      // Velocidad de movimiento automático
    public float manualSpeed = 50.0f;   // Velocidad de movimiento manual

    // VARIABLES INTERNAS (ÁNGULOS)
    private float baseY, shoulderX, elbowX, wristY, miniX, gripY;

    public bool isBusy { get; private set; } = false; // Para saber si está ocupado
    public bool manualMode = true; // Empieza en manual

    private GameObject heldObject = null;

    void Awake()
    {
        // Al arrancar, leemos cómo está el robot en la escena
        SyncJoints();
    }

    void Update()
    {
        // MODO MANUAL (Solo funciona si no está ejecutando una secuencia automática)
        if (manualMode && !isBusy)
        {
            HandleManualInput();
        }
    }

    // --- CONTROL MANUAL (WASD) ---
    private void HandleManualInput()
    {
        float dt = Time.deltaTime * manualSpeed;

        // Base (A/D)
        if (Input.GetKey(KeyCode.A)) baseY -= dt;
        if (Input.GetKey(KeyCode.D)) baseY += dt;

        // Hombro (W/S) - Invertido para que sea natural (W sube)
        if (Input.GetKey(KeyCode.W)) shoulderX -= dt;
        if (Input.GetKey(KeyCode.S)) shoulderX += dt;

        // Codo (Flechas)
        if (Input.GetKey(KeyCode.UpArrow)) elbowX -= dt;
        if (Input.GetKey(KeyCode.DownArrow)) elbowX += dt;

        // Límites de seguridad manuales
        shoulderX = Mathf.Clamp(shoulderX, -90, 90);
        elbowX = Mathf.Clamp(elbowX, 0, 150);

        ApplyRotations(); // Aplicar visualmente

        // Agarre Manual (Espacio)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (heldObject == null) TryGrabManual();
            else ReleaseObject();
        }
    }

    // --- CEREBRO AUTOMÁTICO (FK + EVASIÓN) ---
    public void MoveToTarget(Vector3 targetPos)
    {
        StartCoroutine(MoveToTargetRoutine(targetPos));
    }

    private IEnumerator MoveToTargetRoutine(Vector3 targetPos)
    {
        isBusy = true;
        SyncJoints(); // Sincronizar antes de empezar

        // 1. CÁLCULOS MATEMÁTICOS (Sin librerías complejas)
        Vector3 dir = targetPos - transform.position;
        // Atan2 nos da el ángulo de giro necesario para mirar al objetivo
        float targetBaseAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // Calculamos la distancia para saber cuánto estirar el brazo después
        float dist = Vector3.Distance(transform.position, targetPos);
        // Heurística simple: Más lejos = Hombro más bajo (45º), Más cerca = Hombro más alto (0º)
        float targetShoulder = Mathf.Clamp(dist * 10f, 0f, 45f);
        float targetElbow = Mathf.Clamp(dist * 5f, 20f, 60f);

        // 2. DETECCIÓN DE OBSTÁCULOS (El Rayo Rojo)
        // Lanzamos un rayo desde la mano actual hacia el destino
        bool hayMuro = CheckCollision(endEffectorTarget.position, targetPos);

        if (hayMuro)
        {
            Debug.LogWarning("¡MURO DETECTADO! ACTIVANDO ESTRATEGIA DE GRÚA (ARRIBA-GIRO-ABAJO).");

            // --- PASO 1: RETRAER (SUBIR AL CIELO) ---
            // Ponemos el hombro a -90 (Vertical) y el codo a 0 (Recto)
            // Mantenemos la base actual para no chocar al subir
            float[] poseCielo = { baseY, -90f, 0f, 0f, 0f, gripY };
            yield return StartCoroutine(MoveToPose(poseCielo, 1.0f));

            // --- PASO 2: GIRAR BASE (CON BRAZO ARRIBA) ---
            // Ahora que el brazo apunta al techo, giramos la base hacia el objetivo
            float[] poseGiroSeguro = { targetBaseAngle, -90f, 0f, 0f, 0f, gripY };
            yield return StartCoroutine(MoveToPose(poseGiroSeguro, 1.5f));

            // --- PASO 3: EXTENDER (BAJAR AL OBJETIVO) ---
            // Ya estamos encima del objetivo, bajamos el brazo
            float[] poseFinal = { targetBaseAngle, targetShoulder, targetElbow, 0f, 40f, gripY };
            yield return StartCoroutine(MoveToPose(poseFinal, 1.0f));
        }
        else
        {
            // --- CAMINO LIBRE ---
            Debug.Log("Camino despejado. Movimiento directo.");
            // Vamos directamente interpolando todos los ejes a la vez
            float[] poseDirecta = { targetBaseAngle, targetShoulder, targetElbow, 0f, 40f, gripY };
            yield return StartCoroutine(MoveToPose(poseDirecta, 1.5f));
        }

        isBusy = false;
    }

    // Mueve suavemente todos los motores a la vez (FK Interpolada)
    public IEnumerator MoveToPose(float[] targetAngles, float duration)
    {
        float time = 0;
        float[] start = { baseY, shoulderX, elbowX, wristY, miniX, gripY };

        while (time < duration)
        {
            float t = time / duration;
            t = t * t * (3f - 2f * t); // Suavizado (SmoothStep)

            baseY = Mathf.LerpAngle(start[0], targetAngles[0], t);
            shoulderX = Mathf.LerpAngle(start[1], targetAngles[1], t);
            elbowX = Mathf.LerpAngle(start[2], targetAngles[2], t);
            wristY = Mathf.LerpAngle(start[3], targetAngles[3], t);
            miniX = Mathf.LerpAngle(start[4], targetAngles[4], t);
            gripY = Mathf.LerpAngle(start[5], targetAngles[5], t);

            ApplyRotations();
            time += Time.deltaTime * moveSpeed;
            yield return null;
        }

        // Asegurar posición final exacta
        baseY = targetAngles[0]; shoulderX = targetAngles[1]; elbowX = targetAngles[2];
        wristY = targetAngles[3]; miniX = targetAngles[4]; gripY = targetAngles[5];
        ApplyRotations();
    }

    // --- DETECCIÓN DE COLISIONES ---
    private bool CheckCollision(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float dist = dir.magnitude;

        // Debug visual: Rojo si detecta, Verde si no
        bool hit = Physics.SphereCast(start, 0.2f, dir.normalized, out RaycastHit info, dist, obstacleLayer);

        if (hit) Debug.DrawLine(start, info.point, Color.red, 2.0f);
        else Debug.DrawLine(start, end, Color.green, 2.0f);

        return hit;
    }

    // --- HERRAMIENTAS INTERNAS ---
    private void SyncJoints()
    {
        if (joint_0_Base) baseY = joint_0_Base.localEulerAngles.y;
        if (joint_1_Shoulder) shoulderX = CheckAngle(joint_1_Shoulder.localEulerAngles.x);
        if (joint_2_Elbow) elbowX = CheckAngle(joint_2_Elbow.localEulerAngles.x);
    }

    private void ApplyRotations()
    {
        joint_0_Base.localEulerAngles = new Vector3(0, baseY, 0);
        joint_1_Shoulder.localEulerAngles = new Vector3(shoulderX, 0, 0);
        joint_2_Elbow.localEulerAngles = new Vector3(elbowX, 0, 0);
        joint_3_Wrist.localEulerAngles = new Vector3(0, wristY, 0);
        joint_4_MiniElbow.localEulerAngles = new Vector3(miniX, 0, 0);
        joint_5_GripperRotate.localEulerAngles = new Vector3(0, gripY, 0);
    }

    private float CheckAngle(float angle)
    {
        if (angle > 180) angle -= 360;
        return angle;
    }

    // --- AGARRE ---
    public void ForceGrab(GameObject obj)
    {
        if (!obj) return;
        heldObject = obj;
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
        heldObject.transform.SetParent(gripPoint);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
    }

    public void ReleaseObject()
    {
        if (!heldObject) return;
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;
        heldObject.transform.SetParent(null);
        heldObject = null;
    }

    private void TryGrabManual()
    {
        Collider[] hits = Physics.OverlapSphere(endEffectorTarget.position, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<Rigidbody>() && !hit.isTrigger)
            {
                ForceGrab(hit.gameObject);
                break;
            }
        }
    }
}