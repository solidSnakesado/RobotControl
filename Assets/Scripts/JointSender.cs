using RosMessageTypes.Sensor;                                   // ROS 표준 데이터 타입
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.UrdfImporter;                              // 로봇 관정 정보 접근
using UnityEngine;
using System;                                                   // Math.Abs 사용을 위해 추가

public class JointSender : MonoBehaviour
{
    public  string                  topicName           = "joint_state";    // 데이터 전송 채널 이름
    public  float                   publishRate         = 0.5f;             // 전송 주기 (0.5초)

    private ROSConnection           ros                 = null;
    // private ArticulationBody[]      jointArr    = null;
    private float                   timeElapsed         = 0;
    private List<ArticulationBody>  activeJointList     = new List<ArticulationBody>(); // 움직이는 관절만 담을 리스트

    // 너무 빠른 연속 전송을 막기 위해 인터벌 추가 (0.02f, 50fps)
    private float                   minPublishInterval  = 0.02f;

    // 변화 감지 임계값 (라디안 단위)
    // 0.1 rad = 약 5.7도, 이보다 작은 값은 무시
    private float                   changeThreshold     = 0.1f;

    // 이전 프레임의 관절 각도 저장
    private double[]                prevPositions       = null;

    // 처음 한번은 일단 보내기 위한 변수
    private bool                    isFirstSend         = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 1. ROS 연결
        ros = ROSConnection.GetOrCreateInstance();

        // 2. 토픽 등록 (JointState 메시지를 전달한 이름 지정)
        ros.RegisterPublisher<JointStateMsg>(topicName);

        //// 3. 로봇의 모든 관절 찾기
        //jointArr = GetComponentsInChildren<ArticulationBody>();

        // 3. 모든 ArticulationBody 가져오기
        ArticulationBody[] allJointArr = GetComponentsInChildren<ArticulationBody>();

        // 4. 움직이는 관절(자유도가 0보다 크 것)만 가져오기
        foreach(ArticulationBody joint in allJointArr)
        {
            // jointtype이 Fixed가 아니고 자유도(dofCount)가 0보다 큰 경우만 추가
            if ((joint.jointType != ArticulationJointType.FixedJoint) && (joint.dofCount > 0))
            {
                activeJointList.Add(joint);
            }
        }

        prevPositions = new double[activeJointList.Count];
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed < minPublishInterval)
        {
            return;
        }
            

        //if (timeElapsed > publishRate)
        //{
        //    if (timeElapsed < minPublishInterval)
        //    {
        //        return;
        //    }

        //    // 4. 메시지 만들기
        //    JointStateMsg jointState = new JointStateMsg();

        //    // 5. 골라낸 관절 개수만큼(자유도가 있는 관절 개수) 배열 생성
        //    int jointLength = activeJointList.Count;
        //    jointState.name = new string[jointLength];
        //    jointState.position = new double[jointLength];

        //    // 각 관절의 현재 위치(각도)
        //    for (int i = 0; i < jointLength; ++i)
        //    {
        //        jointState.name[i] = activeJointList[i].name;

        //        // Unity(도) -> ROS(라디안) 변환이 필요하지만 일단 값이 제대로 전송되는 확인용으로 그대로 전달
        //        jointState.position[i] = activeJointList[i].jointPosition[0];
        //    }

        //    // 파이썬 서버로 전달
        //    ros.Publish(topicName, jointState);

        //    timeElapsed = 0;
        //}

        // 1. 현재 관절 데이터 수집 및 변화 감지
        if (CheckAndSendJointState() == true)
        {
            // 값 전송 후 시간 초기화
            timeElapsed = 0;
        }
    }

    private bool CheckAndSendJointState()
    {
        int         jointLength     = activeJointList.Count;
        bool        isChanged       = false;

        // 현재 프레임의 각도값들을 임시로 담을 배열
        double[]    curPositions    = new double[jointLength];

        // 1. 현재 값 수집 및 변화 비교
        for (int i = 0; i < jointLength; ++i)
        {
            // ArticulationBody의 각도는 라디안으로 반환
            curPositions[i] = activeJointList[i].jointPosition[0];

            // 이전 값과 차이가 임계값보다 큰지 확인
            if (Math.Abs(curPositions[i] - prevPositions[i]) > changeThreshold)
            {
                isChanged = true;
            }
        }

        // 2. 첫 전송 이거나, 변화가 감지 되었다면 데이터 전송
        if (isFirstSend == true || isChanged == true)
        {
            isFirstSend = false;

            JointStateMsg jointState = new JointStateMsg();
            jointState.name = new string[jointLength];
            jointState.position = new double[jointLength];

            for (int i = 0; i < jointLength; ++i)
            {
                jointState.name[i] = activeJointList[i].name;
                jointState.position[i] = curPositions[i];

                // 이전 값 업데이트
                prevPositions[i] = curPositions[i]; 
            }

            ros.Publish(topicName, jointState);
        }

        return isChanged;
    }
}
