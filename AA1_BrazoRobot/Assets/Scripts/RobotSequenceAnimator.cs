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
        // Inicia la *nueva* corrutina de animación
        StartCoroutine(PerformNaturalSequence());
    }

    /// <summary>
    /// Corrutina de animación mejorada, más fluida y natural.
    /// Combina movimientos para evitar paradas bruscas.
    /// </summary>
    private IEnumerator PerformNaturalSequence()
    {
        isSequenceRunning = true;
        Debug.Log("INICIANDO SECUENCIA NATURAL...");

        // --- TODO: AJUSTA ESTAS 4 POSES CLAVE ---
        // Estas son las únicas poses que necesitas definir.
        // El script creará los movimientos intermedios.
        // Formato: { Base, Hombro, Codo, Muñeca, MiniCodo }

        // 1. Pose de "justo encima del cubo"
        float[] pose_HoverCube = { 45f, 30f, 60f, 0f, 45f };

        // 2. Pose de "coger el cubo" (un poco más abajo)
        float[] pose_GrabCube = { 45f, 35f, 60f, 0f, 50f };

        // 3. Pose de "justo encima de la zona de soltar" (con el objeto ya rotado)
        float[] pose_HoverDrop = { -90f, 30f, 60f, 90f, 45f }; // Muñeca ya rotada a 90

        // 4. Pose de "soltar el cubo" (un poco más abajo)
        float[] pose_PlaceDrop = { -90f, 35f, 60f, 90f, 50f };


        // --- CREACIÓN DE POSES INTERMEDIAS (AUTOMÁTICO) ---
        // El brazo se levantará a una pose "segura" antes de girar

        // Pose "LiftCube" (levantar el cubo)
        float[] pose_LiftCube = {
            pose_GrabCube[0],  // Misma rotación de base (45f)
            10f, 20f,          // Hombro y codo levantados
            pose_GrabCube[3],  // Misma rotación de muñeca (0f)
            pose_GrabCube[4]   // Misma pose de mini-codo (50f)
        };

        // Pose "LiftDrop" (justo antes de girar hacia el drop)
        float[] pose_LiftDrop = {
            pose_HoverDrop[0], // Base ya girada (-90f)
            10f, 20f,          // Hombro y codo levantados (misma altura)
            pose_HoverDrop[3], // Muñeca ya rotada (90f)
            pose_HoverDrop[4]  // Mini-codo en pose de soltar (45f)
        };


        // --- INICIO DE LA SECUENCIA DE MOVIMIENTO ---

        // 1. IR A COGER (Movimiento fluido)
        Debug.Log("Paso 1: Moviendo para coger...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverCube, 2.5f)); // 2.5 seg
        yield return StartCoroutine(myRobotController.MoveToPose(pose_GrabCube, 1.5f));  // 1.5 seg

        // 2. AGARRAR
        Debug.Log("Paso 2: Agarrando...");
        myRobotController.ForceGrab(targetCube.gameObject);
        yield return new WaitForSeconds(0.5f); // Pausa para que se vea el agarre

        // 3. LEVANTAR Y GIRAR (El movimiento más natural)
        Debug.Log("Paso 3: Levantando y girando...");

        // Sube recto
        yield return StartCoroutine(myRobotController.MoveToPose(pose_LiftCube, 1.5f));

        // ¡El movimiento CLAVE! Gira la base, rota la muñeca y ajusta el brazo
        // TODO en UN SOLO MOVIMIENTO FLUIDO.
        yield return StartCoroutine(myRobotController.MoveToPose(pose_LiftDrop, 3.0f)); // 3 seg

        // 4. IR A SOLTAR
        Debug.Log("Paso 4: Moviendo para soltar...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverDrop, 1.5f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_PlaceDrop, 1.5f));

        // 5. SOLTAR
        Debug.Log("Paso 5: Soltando...");
        myRobotController.ReleaseObject();
        yield return new WaitForSeconds(0.5f); // Pausa

        // 6. VOLVER A CASA
        Debug.Log("Paso 6: Volviendo al inicio...");
        // Sube recto
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverDrop, 1.0f));
        // Vuelve a casa
        yield return StartCoroutine(myRobotController.ResetArm());

        Debug.Log("SECUENCIA COMPLETADA.");
        isSequenceRunning = false;
    }

    // (Esta es la corrutina vieja, la dejamos por si acaso o la puedes borrar)
    private IEnumerator PerformFullSequence()
    {
        isSequenceRunning = true;
        yield return new WaitForSeconds(1.0f); // No hagas nada
        isSequenceRunning = false;
    }
}