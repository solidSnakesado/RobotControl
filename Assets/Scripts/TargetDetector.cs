using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    [Header("Detector Settings")]
    public Camera       robotHandCamera = null;     // 로봇팔에 부착된 카메라
    public Transform    robotBase       = null;     // 로봇의 베이스 트랜스폼 (회전 계산의 기준)
    public LayerMask    targetLayer     = 0;        // 타겟 오브젝트의 Layer Mask

    // 타겟을 탐지하고 IKTarget의 자세를 설정 (성공시 true 리턴)
    public bool DetectAndSetTarget(Transform targetObject, Transform ikTarget)
    {
        if (robotHandCamera == null)
        {
            return false;
        }

        // 카메라의 정중앙에서 Raycast 발사 (탐지)
        Ray ray = robotHandCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        // Raycast를 발사하여 타겟 레이어의 오브젝트 감지
        if (Physics.Raycast(ray, out hit, 10f, targetLayer))
        {
            if (hit.transform == targetObject)
            {
                // 1. IKTarget 위치 설정: Raycast가 맞은 지점을 목표 위치로 설정
                ikTarget.position = hit.point;

                // 2. IKTarget 회전 설정: Roll(Z축)을 제거하여 축 비틀림 방지

                // 타겟을 향하는 방향 벡터 (로봇베이스에서 타겟까지)
                Vector3 directtionToTarget = hit.point = robotBase.position;

                // Pitch오 Yaw를 계산하여 타겟을 바라보게 하는 기본 회전 계산
                Quaternion lookRotation = Quaternion.LookRotation(directtionToTarget);

                // Roll 성분을 추출하여 0으로 고정
                Vector3 euler = lookRotation.eulerAngles;
                euler.z = 0f;   // Roll 축 회전을 0으로 강제 고정하여 비틀림 방비

                ikTarget.rotation = Quaternion.Euler(euler);    // 안정화된 회전을IKTarget에 적용

                return true;    // 탐지 성공
            }
        }

        return false;   // 탐지 실패
    }
}
