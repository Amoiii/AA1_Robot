using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MyRobotController))]
public class RobotSequenceAnimator : MonoBehaviour
{
    public Transform targetCube;
    public Transform dropZone;

    private MyRobotController bot;
    private bool isSequenceRunning = false;

    void Awake() => bot = GetComponent<MyRobotController>();

    void Update()
    {
        // Tecla 2 inicia la secuencia automática
        if (Input.GetKeyDown(KeyCode.Alpha2) && !isSequenceRunning)
        {
            StartCoroutine(PerformSequence());
        }
    }

    private IEnumerator PerformSequence()
    {
        isSequenceRunning = true;
        bot.manualMode = false; // Bloquea manual
        Debug.Log("--- SECUENCIA AUTO START ---");

        // 1. IR AL CUBO (Si hay muro, usa tu MyMath para calcular evasión)
        bot.MoveToTarget(targetCube.position);
        while (bot.isBusy) yield return null;

        // 2. AGARRAR
        bot.ForceGrab(targetCube.gameObject);
        yield return new WaitForSeconds(0.5f);

        // 3. MOVIMIENTOS EXTRA (Requisito: Mover articulaciones libres)
        // Base, Hombro, Codo, Muñeca, MiniCodo, Pinza
        float currentBase = bot.joint_0_Base.localEulerAngles.y;
        float[] poseInspect = { currentBase, -30f, 45f, 0f, 45f, 90f }; // Movemos MiniCodo y Pinza
        yield return StartCoroutine(bot.MoveToPose(poseInspect, 2.0f));

        yield return new WaitForSeconds(0.5f);

        // 4. IR AL DROPZONE
        bot.MoveToTarget(dropZone.position);
        while (bot.isBusy) yield return null;

        // 5. SOLTAR
        bot.ReleaseObject();
        yield return new WaitForSeconds(0.5f);

        // 6. VOLVER A CASA
        yield return StartCoroutine(bot.ResetArm());

        Debug.Log("--- SECUENCIA AUTO FIN ---");
        bot.manualMode = true; // Devuelve control manual
        isSequenceRunning = false;
    }
}