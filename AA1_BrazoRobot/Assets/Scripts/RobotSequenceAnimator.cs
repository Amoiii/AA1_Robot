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

    void Awake() => myRobotController = GetComponent<MyRobotController>();

    void Start()
    {
        if (MyRobotController.startSequenceOnLoad)
        {
            MyRobotController.startSequenceOnLoad = false;
            StartFullSequence();
        }
    }

    public void StartFullSequence()
    {
        if (isSequenceRunning || myRobotController.isBusy) { Debug.LogWarning("Secuencia en marcha o brazo ocupado."); return; }
        StartCoroutine(PerformShowcaseSequence());
    }

    // Ahora TODAS las transiciones usan MoveToPoseSafe(...)
    private IEnumerator PerformShowcaseSequence()
    {
        isSequenceRunning = true;

        // { Base, Hombro, Codo, Muñeca, MiniCodo, GiroPinza }
        float[] pose_HoverCube = { 45f, 30f, 60f, 0f, 45f, 0f };
        float[] pose_GrabCube = { 45f, 35f, 60f, 0f, 50f, 0f };
        float[] pose_HoverDrop = { -90f, 30f, 60f, 0f, 45f, 90f };
        float[] pose_PlaceDrop = { -90f, 35f, 60f, 0f, 50f, 90f };

        float[] pose_Lift = { pose_GrabCube[0], 10f, 20f, 0f, 50f, 0f };

        float[] pose_Inspect_WristLeft = { pose_Lift[0], pose_Lift[1], pose_Lift[2], 45f, pose_Lift[4], pose_Lift[5] };
        float[] pose_Inspect_WristRight = { pose_Lift[0], pose_Lift[1], pose_Lift[2], -45f, pose_Lift[4], pose_Lift[5] };
        float[] pose_Inspect_GripLeft = { pose_Lift[0], pose_Lift[1], pose_Lift[2], 0f, pose_Lift[4], 45f };
        float[] pose_Inspect_GripRight = { pose_Lift[0], pose_Lift[1], pose_Lift[2], 0f, pose_Lift[4], -45f };

        float[] pose_LiftDrop = { pose_HoverDrop[0], 10f, 20f, pose_HoverDrop[3], pose_HoverDrop[4], pose_HoverDrop[5] };
        float[] pose_Wave = { pose_HoverDrop[0], pose_HoverDrop[1], 90f, -45f, pose_HoverDrop[4], pose_HoverDrop[5] };

        // 1. Ir a coger
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_HoverCube[0], pose_HoverCube[1], pose_HoverCube[2], pose_HoverCube[3], pose_HoverCube[4], pose_HoverCube[5], 2.5f));
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_GrabCube[0], pose_GrabCube[1], pose_GrabCube[2], pose_GrabCube[3], pose_GrabCube[4], pose_GrabCube[5], 1.5f));

        // 2. Agarrar
        myRobotController.ForceGrab(targetCube.gameObject);
        yield return new WaitForSeconds(0.5f);

        // 3. Levantar
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_Lift[0], pose_Lift[1], pose_Lift[2], pose_Lift[3], pose_Lift[4], pose_Lift[5], 1.5f));

        // 4. Inspeccionar muñeca
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_Inspect_WristLeft[0], pose_Inspect_WristLeft[1], pose_Inspect_WristLeft[2], pose_Inspect_WristLeft[3], pose_Inspect_WristLeft[4], pose_Inspect_WristLeft[5], 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_Inspect_WristRight[0], pose_Inspect_WristRight[1], pose_Inspect_WristRight[2], pose_Inspect_WristRight[3], pose_Inspect_WristRight[4], pose_Inspect_WristRight[5], 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_Lift[0], pose_Lift[1], pose_Lift[2], pose_Lift[3], pose_Lift[4], pose_Lift[5], 1.0f));

        // 5. Inspeccionar pinza
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_Inspect_GripLeft[0], pose_Inspect_GripLeft[1], pose_Inspect_GripLeft[2], pose_Inspect_GripLeft[3], pose_Inspect_GripLeft[4], pose_Inspect_GripLeft[5], 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_Inspect_GripRight[0], pose_Inspect_GripRight[1], pose_Inspect_GripRight[2], pose_Inspect_GripRight[3], pose_Inspect_GripRight[4], pose_Inspect_GripRight[5], 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_Lift[0], pose_Lift[1], pose_Lift[2], pose_Lift[3], pose_Lift[4], pose_Lift[5], 1.0f));

        // 6. Girar hacia zona de soltar
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_LiftDrop[0], pose_LiftDrop[1], pose_LiftDrop[2], pose_LiftDrop[3], pose_LiftDrop[4], pose_LiftDrop[5], 3.0f));

        // 7. Ir a soltar
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_HoverDrop[0], pose_HoverDrop[1], pose_HoverDrop[2], pose_HoverDrop[3], pose_HoverDrop[4], pose_HoverDrop[5], 1.5f));
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_PlaceDrop[0], pose_PlaceDrop[1], pose_PlaceDrop[2], pose_PlaceDrop[3], pose_PlaceDrop[4], pose_PlaceDrop[5], 1.5f));

        // 8. Soltar
        myRobotController.ReleaseObject();
        yield return new WaitForSeconds(0.5f);

        // 9. Despedida
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_HoverDrop[0], pose_HoverDrop[1], pose_HoverDrop[2], pose_HoverDrop[3], pose_HoverDrop[4], pose_HoverDrop[5], 1.0f));
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_Wave[0], pose_Wave[1], pose_Wave[2], pose_Wave[3], pose_Wave[4], pose_Wave[5], 0.8f));
        yield return StartCoroutine(myRobotController.MoveToPoseSafe(pose_HoverDrop[0], pose_HoverDrop[1], pose_HoverDrop[2], pose_HoverDrop[3], pose_HoverDrop[4], pose_HoverDrop[5], 0.8f));

        // 10. Volver a casa
        yield return StartCoroutine(myRobotController.ResetArm());

        isSequenceRunning = false;
    }
}
