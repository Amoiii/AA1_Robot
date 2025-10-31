using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MyRobotController))]
public class RobotSequenceAnimator : MonoBehaviour
{
    [Header("Objetivos de Animación")]
    [SerializeField] private Transform targetCube;
    [SerializeField] private Transform dropZone;

    private MyRobotController myRobotController;
    private bool isSequenceRunning = false;


    void Awake()
    {
        myRobotController = GetComponent<MyRobotController>();
    }

    void Start()
    {
        // Esta lógica comprueba si debe auto-iniciar la animación
        if (MyRobotController.startSequenceOnLoad)
        {
            MyRobotController.startSequenceOnLoad = false;
            StartFullSequence();
        }
    }

    public void StartFullSequence()
    {
        if (isSequenceRunning || myRobotController.isBusy)
        {
            Debug.LogWarning("Ya hay una secuencia en marcha o el brazo está ocupado.");
            return;
        }
        // Inicia la *NUEVA* corrutina de animación "Showcase"
        StartCoroutine(PerformShowcaseSequence());
    }

    /// <summary>
    /// Corrutina de animación "Showcase" para demostrar todos los ejes.
    /// </summary>
    private IEnumerator PerformShowcaseSequence()
    {
        isSequenceRunning = true;
        Debug.Log("INICIANDO SECUENCIA SHOWCASE...");

        // --- TODO: AJUSTA ESTAS 4 POSES CLAVE ---
        // Sigue siendo tu tarea principal calibrar estas 4 poses.
        // Formato: { Base, Hombro, Codo, Muñeca, MiniCodo, GiroPinza }

        float[] pose_HoverCube = { 45f, 30f, 60f, 0f, 45f, 0f };
        float[] pose_GrabCube = { 45f, 35f, 60f, 0f, 50f, 0f };
        float[] pose_HoverDrop = { -90f, 30f, 60f, 0f, 45f, 90f };
        float[] pose_PlaceDrop = { -90f, 35f, 60f, 0f, 50f, 90f };


        // --- CREACIÓN DE POSES INTERMEDIAS (AUTOMÁTICO) ---
        // Usaremos estas poses para los movimientos extra

        // Pose "Lift" (levantar el cubo)
        float[] pose_Lift = {
            pose_GrabCube[0], 10f, 20f, 0f, 50f, 0f
        };

        // Poses para "Inspeccionar" (relativas a la pose Lift)
        float[] pose_Inspect_WristLeft = { pose_Lift[0], pose_Lift[1], pose_Lift[2], 45f, pose_Lift[4], pose_Lift[5] };
        float[] pose_Inspect_WristRight = { pose_Lift[0], pose_Lift[1], pose_Lift[2], -45f, pose_Lift[4], pose_Lift[5] };
        float[] pose_Inspect_GripLeft = { pose_Lift[0], pose_Lift[1], pose_Lift[2], pose_Lift[3], pose_Lift[4], 45f };
        float[] pose_Inspect_GripRight = { pose_Lift[0], pose_Lift[1], pose_Lift[2], pose_Lift[3], pose_Lift[4], -45f };

        // Pose de transición antes de soltar
        float[] pose_LiftDrop = {
            pose_HoverDrop[0], 10f, 20f, pose_HoverDrop[3], pose_HoverDrop[4], pose_HoverDrop[5]
        };

        // Pose de "Despedida" (relativa a la pose HoverDrop)
        float[] pose_Wave = {
            pose_HoverDrop[0], pose_HoverDrop[1], 90f, -45f, pose_HoverDrop[4], pose_HoverDrop[5]
        };


        // --- INICIO DE LA SECUENCIA DE MOVIMIENTO ---

        // 1. IR A COGER
        Debug.Log("Paso 1: Moviendo para coger...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverCube, 2.5f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_GrabCube, 1.5f));

        // 2. AGARRAR
        Debug.Log("Paso 2: Agarrando...");
        myRobotController.ForceGrab(targetCube.gameObject);
        yield return new WaitForSeconds(0.5f);

        // 3. LEVANTAR
        Debug.Log("Paso 3: Levantando objeto...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Lift, 1.5f));

        // 4. INSPECCIONAR (EJE 4 - MUÑECA)
        Debug.Log("Paso 4: Inspeccionando (Eje 4: Muñeca)...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Inspect_WristLeft, 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Inspect_WristRight, 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Lift, 1.0f)); // Volver al centro

        // 5. INSPECCIONAR (EJE 6 - PINZA)
        Debug.Log("Paso 5: Inspeccionando (Eje 6: Pinza)...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Inspect_GripLeft, 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Inspect_GripRight, 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Lift, 1.0f)); // Volver al centro

        // 6. GIRAR A ZONA DE SOLTAR
        Debug.Log("Paso 6: Girando hacia la zona de soltar...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_LiftDrop, 3.0f));

        // 7. IR A SOLTAR
        Debug.Log("Paso 7: Moviendo para soltar...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverDrop, 1.5f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_PlaceDrop, 1.5f));

        // 8. SOLTAR
        Debug.Log("Paso 8: Soltando...");
        myRobotController.ReleaseObject();
        yield return new WaitForSeconds(0.5f);

        // 9. DESPEDIDA (NUEVO)
        Debug.Log("Paso 9: Despidiéndose...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverDrop, 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Wave, 0.8f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverDrop, 0.8f));

        // 10. VOLVER A CASA
        Debug.Log("Paso 10: Volviendo al inicio...");
        yield return StartCoroutine(myRobotController.ResetArm()); // Resetea los 6 ejes

        Debug.Log("SECUENCIA COMPLETADA.");
        isSequenceRunning = false;
    }
}