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
        // Comprueba la bandera estática al iniciar la escena
        if (MyRobotController.startSequenceOnLoad)
        {
            // Resetea la bandera para que no vuelva a ocurrir
            MyRobotController.startSequenceOnLoad = false;

            // Lanza la animación
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
        StartCoroutine(PerformFullSequence());
    }

    private IEnumerator PerformFullSequence()
    {
        isSequenceRunning = true;
        Debug.Log("INICIANDO SECUENCIA AUTOMÁTICA...");

        // --- TODO: AJUSTA ESTAS POSES ---
        // Formato: { Base, Hombro, Codo, Muñeca, MiniCodo }
        float[] pose_HoverCube = { 45f, 30f, 60f, 0f, 45f };
        float[] pose_GrabCube = { 45f, 35f, 60f, 0f, 50f };
        float[] pose_LiftUp = { 45f, -10f, 20f, 0f, 50f };
        float[] pose_Rotate = { 45f, -10f, 20f, 90f, 50f };
        float[] pose_MoveSide = { -30f, -10f, 20f, 90f, 50f };
        float[] pose_HoverDrop = { -90f, 30f, 60f, 90f, 45f };
        float[] pose_PlaceDrop = { -90f, 35f, 60f, 90f, 50f };

        // --- 1. Ir a por el cubo ---
        Debug.Log("Paso 1: Moviendo para coger el cubo...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverCube, 3.0f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_GrabCube, 2.0f));

        // --- 2. Coger ---
        Debug.Log("Paso 2: Agarrando...");
        myRobotController.ForceGrab(targetCube.gameObject);
        yield return new WaitForSeconds(0.5f);

        // --- 3. Levantar (Paso Nuevo) ---
        Debug.Log("Paso 3: Levantando objeto...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_LiftUp, 2.0f));

        // --- 4. Rotar Objeto (Paso Nuevo) ---
        Debug.Log("Paso 4: Rotando objeto...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_Rotate, 2.0f));

        // --- 5. Mover un poco (Paso Nuevo) ---
        Debug.Log("Paso 5: Moviendo a un lado...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_MoveSide, 1.5f));

        // --- 6. Ir a la zona de soltar ---
        Debug.Log("Paso 6: Moviendo a la zona de soltar...");
        yield return StartCoroutine(myRobotController.MoveToPose(pose_HoverDrop, 3.0f));
        yield return StartCoroutine(myRobotController.MoveToPose(pose_PlaceDrop, 2.0f));

        // --- 7. Soltar ---
        Debug.Log("Paso 7: Soltando objeto...");
        myRobotController.ReleaseObject();
        yield return new WaitForSeconds(0.5f);

        // --- 8. Volver al inicio ---
        Debug.Log("Paso 8: Volviendo al inicio.");
        yield return StartCoroutine(myRobotController.ResetArm());

        Debug.Log("SECUENCIA COMPLETADA.");
        isSequenceRunning = false;
    }
}