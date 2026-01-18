using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MyRobotController))]
public class RobotSequenceAnimator : MonoBehaviour
{
    public Transform targetCube;
    public Transform dropZone;

    private MyRobotController bot;
    private bool sequenceRunning = false;

    void Awake() => bot = GetComponent<MyRobotController>();

    void Update()
    {
        // Tecla 1: Volver a control manual
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            bot.manualMode = true;
            sequenceRunning = false;
            StopAllCoroutines();
            Debug.Log("Modo MANUAL activado.");
        }

        // Tecla 2: Iniciar secuencia automática
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (!sequenceRunning)
            {
                bot.manualMode = false; // Bloquea el teclado manual
                StartCoroutine(FullSequence());
            }
        }
    }

    private IEnumerator FullSequence()
    {
        sequenceRunning = true;
        Debug.Log("--- INICIANDO SECUENCIA ---");

        // 1. IR AL CUBO (El Controller decide si esquiva o va recto)
        bot.MoveToTarget(targetCube.position);
        while (bot.isBusy) yield return null;

        // 2. AGARRAR
        bot.ForceGrab(targetCube.gameObject);
        yield return new WaitForSeconds(0.5f);

        // 3. CAMBIO DE ORIENTACIÓN (Requisito del profesor)
        // Hacemos una pequeña floritura: Levantar un poco y girar pinza
        float currentBase = bot.joint_0_Base.localEulerAngles.y;
        float[] poseGiro = { currentBase, -20f, 45f, 0f, 0f, 90f }; // Pinza a 90 grados
        yield return StartCoroutine(bot.MoveToPose(poseGiro, 1.5f));

        // 4. IR AL DROPZONE (Si el muro sigue ahí, lo esquivará otra vez)
        bot.MoveToTarget(dropZone.position);
        while (bot.isBusy) yield return null;

        // 5. SOLTAR
        bot.ReleaseObject();
        yield return new WaitForSeconds(0.5f);

        // 6. VOLVER A CASA
        float[] home = { 0f, 0f, 0f, 0f, 0f, 0f };
        yield return StartCoroutine(bot.MoveToPose(home, 1.5f));

        Debug.Log("--- SECUENCIA FINALIZADA ---");
        sequenceRunning = false;
        bot.manualMode = true; // Devolvemos el control al usuario
    }
}