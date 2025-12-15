using UnityEngine;

public class CCDIKSolver : MonoBehaviour
{
    // === IK Chain 설정 ===
    [Header("IK Chain Settings")]
    public  Transform           target                  = null;     // IK 목표지정 (IKTarget 오브젝트)
    public  Transform           gripperTip              = null;     // 로봇 그리퍼의 끝 부분
    public  Transform[]         jointArr                = null;     // IK 계산에 사용할 로봇 관절 배열 (Base 에서 Tip 순)

    // === 임계값 파라미터 ===
    [Header("Parameters")]
    public  int                 iterations              = 10;       // IK 계산 반복 횟수 (정확도 조절)
    public  float               maxAngleChange          = 10f;      // 한 번의 반복에서 관절이 회전할 수 있는 최대 각도 (안정성 조절)
    public  float               tolerance               = 0.01f;    // 목표에 도달했다고 판단할 거리 임계값

    private ArticulationBody[]  articulationJointArr    = null;     // ArticulationBody 컴포넌트 저장 배열

    void Start()
    {
        // 모든 관절의 ArticulationBody 컴포넌트를 가져와 지정
        if (jointArr != null && jointArr.Length > 0)
        {
            articulationJointArr = new ArticulationBody[jointArr.Length];
            for (int i = 0; i < jointArr.Length; ++i)
            {
                articulationJointArr[i] = jointArr[i].GetComponent<ArticulationBody>();
                if (articulationJointArr[i] == null)
                {
                    Debug.LogError($"Joint {i} ({jointArr[i].name}) 에 ArticulationBody 컴포넌트가 없음");
                }
            }
        }
    }

    void FixedUpdate()
    {
        // FixedUpdata (물리 업데이트) 루프에서 IK 계산을 수행 하여 ArticulationBody 제어의 안정성 확보
        SolveIK();
    }

    public void SolveIK()
    {
        // 1. 설정 및 목표 달성 검사

        // 필수 요소가 할당이 안되어 잇으면 진행 중지
        if (target == null || gripperTip == null || jointArr == null || articulationJointArr == null)
        {
            return;
        }

        // 목표(target)와 끝점(tip) 사이의 거리가 허용 오차(tolerance)보다 작으면 진행 중지
        if ((target.position - gripperTip.position).sqrMagnitude < (tolerance * tolerance))
        {
            return;
        }

        // 2. CCD (Cyclic Coordinate Descent) 알고리즘
        // 좌표계(Coordinate)를 순회하면서(Cyclic) 목표와의 차이(Descent)f를 줄여나가는 방식

        // 설정된 반복횟수만큼 반복
        for (int i = 0; i < iterations; ++i)
        {
            // CCD: Tip 에 가장 가까운 관절부터 Base 까지 역순회
            for (int j = jointArr.Length - 1; j >= 0; --j)
            {
                Transform currentJoint = jointArr[j];
                ArticulationBody currentBody = articulationJointArr[j];

                // 회전 관절이 아니면 continue
                if (currentBody.dofCount == 0)
                {
                    continue;
                }

                // A.회전 계산을 위한 벡터 정의
                Vector3 currentTipVec       = gripperTip.position - currentJoint.position;      // 관절 -> 현재 Tip 벡터
                Vector3 currentTargetVec    = target.position - currentJoint.position;          // 관절 -> 목표 target 벡터

                currentTipVec.Normalize();
                currentTargetVec.Normalize();

                // B. 두 벡터 사이의 회전량 계산 (필요한 Axis와 Angle)
                Quaternion rotationDelta = Quaternion.FromToRotation(currentTipVec, currentTargetVec);
                rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

                // C. 각도 변화량 제한 (급격한 움직임 방지)
                if (angle > maxAngleChange)
                {
                    angle = maxAngleChange;     // 최대 회전 각도 제한 (안정성 확보)
                }

                // D. 새로운 회전값 계산 (World Rotation)
                Quaternion newRotation = Quaternion.AngleAxis(angle, axis) * currentJoint.rotation;

                // E. ArticulationBody 제어를 위해 Local Space로 변환
                // ArticulationBody의 목표회전을 부모의 역회전을 곱해 로컬 공간으로 변환
                Quaternion localRotationTarget = Quaternion.Inverse(currentJoint.parent.rotation) * newRotation;

                // F. ArticulationBody의 xDrive.target에 적용
                // 대부분의 로봇 관절은 X축으로 회전하므로, x축의 목표 각도를 추출
                // ArticulationBody는 Degree (각도)를 사용
                float targetAngleX = localRotationTarget.eulerAngles.x;

                // eulerAngles는 0~360도 사용, -180~180으로 변환해야 관절에 적용가능
                if (targetAngleX > 180f)
                {
                    targetAngleX -= 360f;
                }

                // G. ArticulationBody의 xDrive.target 설정 (관절 제어 명령)
                ArticulationDrive drive = currentBody.xDrive;
                drive.target = targetAngleX;    // 계산된 목표 각도를 Drive에 설정
                currentBody.xDrive = drive;

                // H. CCD 알고리즘의 다음 반복 계산을 위해 Transform를 즉시 업데이트
                currentJoint.rotation = newRotation;
            }

            // 목표 도달 검사 (반목문 중간에 한번 더 확인)
            if ((target.position - gripperTip.position).sqrMagnitude < tolerance * tolerance)
            {
                break;  // 목표에 도달했으면 중단
            }
        }
    }
}