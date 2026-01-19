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
        if (Input.GetKeyDown(KeyCode.Alpha2) && !isSequenceRunning)
        {
            StartCoroutine(PerformSequence());
        }
    }

    private IEnumerator PerformSequence()
    {
        isSequenceRunning = true;
        bot.manualMode = false;
        Debug.Log("--- INICIO AUTO ---");

        // 1. IR AL CUBO
        Debug.Log("Yendo al cubo...");
        bot.MoveToTarget(targetCube.position);

        // Esperamos a llegar
        while (bot.isBusy) yield return null;
        yield return new WaitForSeconds(0.5f); // Estabilizar

        // 2. VERIFICAR Y AGARRAR
        if (bot.CanGrab(targetCube.gameObject))
        {
            Debug.Log("Contacto confirmado. Agarrando.");
            bot.ForceGrab(targetCube.gameObject);
        }
        else
        {
            Debug.LogError("FALLO: El robot llegó pero no está tocando el cubo. Revisar posición.");
            // Intentamos un agarre de emergencia por si está muy cerca
            bot.ForceGrab(targetCube.gameObject);
        }
        yield return new WaitForSeconds(0.5f);

        // 3. INSPECCIÓN (Girar articulaciones extra)
        float currentBase = bot.joint_0_Base.localEulerAngles.y;
        float[] poseInspect = { currentBase, -30f, 45f, 0f, 45f, 90f };
        yield return StartCoroutine(bot.MoveToPose(poseInspect, 2.0f));
        yield return new WaitForSeconds(0.5f);

        // 4. IR AL DROPZONE
        Debug.Log("Llevando a DropZone...");
        bot.MoveToTarget(dropZone.position);
        while (bot.isBusy) yield return null;
        yield return new WaitForSeconds(0.5f);

        // 5. VERIFICAR Y SOLTAR
        if (bot.IsInDropZone())
        {
            Debug.Log("Zona de entrega confirmada. Soltando.");
            bot.ReleaseObject();
        }
        else
        {
            Debug.LogWarning("No detecto la DropZone, pero suelto igual por seguridad.");
            bot.ReleaseObject();
        }
        yield return new WaitForSeconds(0.5f);

        // 6. CASA
        yield return StartCoroutine(bot.ResetArm());

        Debug.Log("--- FIN AUTO ---");
        bot.manualMode = true;
        isSequenceRunning = false;
    }
}