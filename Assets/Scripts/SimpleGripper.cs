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

    //private void OnCollisionEnter(Collision collision)
    //{
    //    // 1. 닫는 신호 수신, 2. 타겟(큐브)와 충돌 시작, 3. 아직 잡지는 못함
    //    if ((isClosing == true) && (collision.gameObject.CompareTag("Target") == true) && (joint == null))
    //    {
    //        // 물리적으로 접착
    //        joint = gameObject.AddComponent<FixedJoint>();
    //        joint.connectedBody = collision.rigidbody;
    //        joint.breakForce = breakForce;
    //    }
    //}
    private void OnTriggerEnter(Collider other)
    {
        // 1. 이미 잡은 게 있거나, 대상이 'Target'이 아니면 무시
        if (joint != null || !other.gameObject.CompareTag("Target")) return;

        // 2. 잡기 로직 (충격 없이 부드럽게 연결)
        Rigidbody targetRb = other.attachedRigidbody; // 충돌한 물체의 리지드바디 가져오기

        if (targetRb != null)
        {
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = targetRb;
            joint.breakForce = 5000f; // 힘을 넉넉하게 주세요 (잘 안 끊어지게)

            Debug.Log("부드럽게 잡았습니다!");
        }
    }


    private void OnJointBreak(float breakForce)
    {
        Debug.Log("Grasp Failed: Object slipped!");
        joint = null;
    }
}
