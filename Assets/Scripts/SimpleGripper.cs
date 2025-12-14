using UnityEngine;

public class SimpleGripper : MonoBehaviour
{
    private bool isClosing = false;
    private FixedJoint joint;

    [Header("Physics Settings")]
    public float breakForce = 600f; // 이 힘을 넘기면 미끄러짐 (실패 유도)

    public void Open()
    {
        isClosing = false;
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }
    }

    public void Close()
    {
        isClosing = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. 닫는 신호 수신, 2. 타겟(큐브)와 충돌 시작, 3. 아직 잡지는 못함
        if ((isClosing == true) && (collision.gameObject.CompareTag("Target") == true) && (joint == null))
        {
            // 물리적으로 접착
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = collision.rigidbody;
            joint.breakForce = breakForce;
        }
    }

    private void OnJointBreak(float breakForce)
    {
        Debug.Log("Grasp Failed: Object slipped!");
        joint = null;
    }
}
