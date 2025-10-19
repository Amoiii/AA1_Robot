using UnityEngine;

public class MyRobotController : MonoBehaviour
{
    [Header("Articulaciones (Pivotes)")]
    [SerializeField]
    private Transform joint_0_Base;
    [SerializeField]
    private Transform joint_1_Shoulder;
    [SerializeField]
    private Transform joint_2_Elbow;
    [SerializeField]
    private Transform joint_3_Wrist;

    [Header("Punto Final (End Effector)")]
    [SerializeField]
    private Transform endEffectorTarget;

    [Header("Parámetros de Movimiento")]
    [SerializeField]
    private float rotationSpeed = 50.0f;


    void Update()
    {
        // Control de prueba con teclado
        ControlManual();
    }

    private void ControlManual()
    {
        // Joint 0: Base (Giro en Y)
        if (Input.GetKey(KeyCode.A))
        {
            joint_0_Base.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            joint_0_Base.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        // Joint 1: Hombro (Giro en X)
        if (Input.GetKey(KeyCode.W))
        {
            joint_1_Shoulder.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            joint_1_Shoulder.Rotate(Vector3.right, -rotationSpeed * Time.deltaTime);
        }

        // Joint 2: Codo (Giro en X)
        if (Input.GetKey(KeyCode.Q))
        {
            joint_2_Elbow.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            joint_2_Elbow.Rotate(Vector3.right, -rotationSpeed * Time.deltaTime);
        }

        // Joint 3: Muñeca (Giro en Y)
        if (Input.GetKey(KeyCode.Z))
        {
            joint_3_Wrist.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.C))
        {
            joint_3_Wrist.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

  

    public void GrabObject(GameObject objectToGrab)
    {
        Debug.Log("Intentando agarrar: " + objectToGrab.name);
    }

    public void ReleaseObject()
    {
        Debug.Log("Soltando objeto.");
    }

    public void MoveToTarget(Vector3 targetPosition, Quaternion targetRotation)
    {
        Debug.Log("Moviendo a: " + targetPosition);
    }
}