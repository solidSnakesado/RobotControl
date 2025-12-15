using UnityEngine;

public class RobotArmController : MonoBehaviour
{
    [Header("Arm Links")]
    public Transform        toolLink            = null;     // 로복 팔의 말단 (CCDIK의 Tip)

    [Header("Niryo Gripper Settings")]
    // Niryo One은 두개의 손가락이 움직이므로 두개의 관절을 참조
    public ArticulationBody leftFingerJoint     = null;     // 집게 좌측
    public ArticulationBody rightFingerJoint    = null;     // 집게 우측

    // Niryo One 그리퍼는 주로 Prismatic(미닫이) 또는 Revolute(회전) 방식을 사용
    // Unity Inspector에서 테스트해보며 적절한 값 설정 필요
    public float            gripperOpenValue    = 0.0f;     // 그리퍼가 열렸을 때의 xDrive 목표값
    public float            gripperCloseValue   = -0.2f;    // 그리퍼가 닫혔을 때의 xDrive 목표값

    [Header("Debug")]
    public bool             testGrasp           = false;
    public bool             testRelease         = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 인스펙터에서 테스트 하기 위한 디버그 코드
        if (testGrasp == true)
        {
            SetGripperState(true);
            testGrasp = false;
        }
        if (testRelease == true)
        {
            SetGripperState(false);
            testRelease = false;    
        }
    }

    // 현재 ToolLink가 목표(Target) 위치에 도달했는지 확인 (CCDIKSolve와 연동)
    public bool isAtTarget(Transform target, float tolerance)
    {
        if (toolLink == null || target == null)
        {
            return false;
        }

        // ToolLink의 현재 위치와 목표 위치의 거리를 비교
        return Vector3.Distance(toolLink.position, target.position) < tolerance;
    }

    // 외부 호출용, 그리퍼를 닫고 타겟을 로봇의 자식으로 설정 (물제 집기)
    public void PerformGrasp(Transform targetObject)
    {
        // 1. 물리적으로 그리퍼 닫기
        SetGripperState(true);

        // 2. 시뮬레이션 상 안정성을 위해 타겟을 ToolLink의 자식으로 설정 (물리 고정)
        if (targetObject != null)
        {
            targetObject.SetParent(toolLink);

            // 물리 연산 충돌 방지를 위해 타겟의 Rigidbody를 잠시 키네마틱으로 변경
            Rigidbody rb = targetObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;  
            }

            // (옵션) 타겟을 그리퍼의 중앙으로 살짝 보정
            // 이 부분은 확인 후에 추가할지 여부 판단
            // targetObject.localPosition = new Vector3(0, 0, 0.05f);
        }

        Debug.Log("그리퍼 액션 Performed");
    }

    // 외부 호출용, 그리퍼 열기 및 타겟 놓기
    public void ReleaseGrasp(Transform targetObject)
    {
        // 1. 물리적으로 그리퍼 열기
        SetGripperState(false);

        // 2. 부모 관계 해제
        if (targetObject != null)
        {
            targetObject.SetParent(null);   // 최상위로 분리하거나 Scene의 다른 특정 부모로 설정

            // 물리 연산 재개
            Rigidbody rb = targetObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
    }

    // 실제 ArticulateBody를 움직이는 내부 함수
    private void SetGripperState(bool isClosed)
    {
        float targetValue = isClosed ? gripperCloseValue : gripperOpenValue;

        // 왼쪽 손가락 제어
        if (leftFingerJoint != null)
        {
            ArticulationDrive drive = leftFingerJoint.xDrive;
            drive.target = targetValue;
            leftFingerJoint.xDrive = drive;
        }

        // 오른쪽 손가락 제어 (보통 왼손과 대칭으로 움직임)
        if (rightFingerJoint != null)
        {
            ArticulationDrive drive = rightFingerJoint.xDrive;
            drive.target = targetValue;     // 왼손과 반대 방향이라면 -1 곱해 주어야 함
            rightFingerJoint.xDrive = drive;
        }
    }
}
