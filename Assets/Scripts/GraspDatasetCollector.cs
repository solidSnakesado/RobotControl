using UnityEngine;
using System.Collections;
using System.IO;

public class GraspDatasetCollector : MonoBehaviour
{
    [Header("=== 1. References ===")]
    public  Transform           targetCube          = null;                         // 큐브 (Target)
    public  Transform           robotIKTarget       = null;                         // 로봇 팔이 따라가는 투명 핸들
    public  GripperController   gripper             = null;                         // 그리퍼 스크립트
    public  Camera              handCamera          = null;                         // 로봇 손에 달린 카메라

    [Header("=== 2. Settings ===")]
    public  Vector2             spawnRangeX         = new Vector2(-0.2f, 0.2f);     // 큐브 생선 범위 X
    public  Vector2             spawnRangeZ         = new Vector2(-0.2f, 0.2f);     // 큐브 생선 범위 Z
    public  float               tableHeight         = 0.64372f;                     // 큐브 생선 범위 Y
    public  float               robotSpeed          = 0.125f;                         // 로봇 이동 속도
    public  float               observationHeight   = 0.25f;                        // 큐브위 어느정도 높이 에서 데이터셋 이미지를 추출할지 지정

    [Header("=== 3. Failure Generator (Noise) ===")]
    [Range(0f, 0.1f)]
    public  float               positionNoise       = 0.03f;                        // 이 값을 조정해 실패율 조정

    [Header("=== 4. Save Paths ===")]
    public  string              datasetFolder       = "Assets/Dataset/";
    private string              csvPath             = string.Empty;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 1. 폴더 생성
        if (Directory.Exists(datasetFolder + "images/") == false)
        {
            Directory.CreateDirectory(datasetFolder + "images/");
        }

        // 2. CSV 파일 헤더 작성
        csvPath = datasetFolder + "label.csv";
        if (File.Exists(csvPath) == false)
        {
            // 헤더 : 이미지이름, 시도한x, 시도한y, 시도한z, 성공여부
            File.WriteAllText(csvPath, "image_id,grasp_x,grasp_y,grasp_z,label_success\n");
        }

        // 3. 자동 수집 루프 시작
        StartCoroutine(CollectionLoop());
    }

    IEnumerator CollectionLoop()
    {
        int episode = 0;

        // 기존 파일이 있다면 이어서 번호 지정
        string[] existingFiles = Directory.GetFiles(datasetFolder + "images/", "*.png");
        episode = existingFiles.Length;

        while (true)
        {
           ++episode;
            string imgName = $"img_{episode:D5}.png";   // ex> img_00001.png

            // 1. 초기화 (Reset)
            gripper.OpenGripper();

            float realX = Random.Range(spawnRangeX.x, spawnRangeX.y);
            float realZ = Random.Range(spawnRangeZ.x, spawnRangeZ.y);

            Vector3 realCubePos = new Vector3(realX, tableHeight, realZ);

            targetCube.position = realCubePos;
            targetCube.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            // 2. 관찰, 집게의 카메라로 찍기 위해 오브젝트 위호 이동
            Vector3 observePos = new Vector3(realX, tableHeight + observationHeight, realZ);
            yield return MoveRobot(observePos);
            yield return new WaitForSeconds(0.3f);   // 카메라 흔들림 안정화

            // 3. 이미지 캡쳐 및 저장
            CaptureAndSaveImage(imgName);

            // 4. 실패 데이터를 만들기 위해 좌표에 노이지(오차) 추가
            float noiseX = Random.Range(-positionNoise, positionNoise);
            float noiseZ = Random.Range(-positionNoise, positionNoise);

            // 로봇이 실제로 가게 될 좌표 (오차 반영)
            Vector3 attemptPos = new Vector3(realX + noiseX, tableHeight, realZ + noiseZ);

            yield return MoveRobot(attemptPos);

            // 5. 그리퍼 닫기 (물리 체크)
            gripper.CloseGripper();
            yield return new WaitForSeconds(0.5f);

            // 6. 들어 올리기
            yield return MoveRobot(observePos);
            yield return new WaitForSeconds(0.5f);

            // 7. 판정, 큐브가 잡혀있으면 성공(1), 아니면 실패(0)
            bool isSuccess = targetCube.position.y > (tableHeight + -0.05f);
            int label = isSuccess ? 1 : 0;

            // 8. 저장, CSV 기록\
            // 학습할 때는 시도한 좌표(attemptPos)와 결과(label) 관계를 학습
            SaveToCSV(imgName, attemptPos, label);

            Debug.Log($"Ep {episode}: {(isSuccess ? "<color=green>Success</color>" : "<color=red>Fail</color>")}");
            yield return new WaitForSeconds(0.5f);
        }

        yield return null;
    }

    IEnumerator MoveRobot(Vector3 targetPos)
    {
        Vector3 startPos = robotIKTarget.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / robotSpeed;     // 거리에 따라 조절
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / duration;
            robotIKTarget.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        robotIKTarget.position = targetPos;
    }

    private void CaptureAndSaveImage(string fileName)
    {
        RenderTexture rt = new RenderTexture(224, 224, 24);
        handCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(224, 224, TextureFormat.RGB24, false);

        handCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 224, 224), 0, 0);

        handCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        File.WriteAllBytes(datasetFolder + "images/" + fileName, screenShot.EncodeToPNG());
    }

    private void SaveToCSV(string imgName, Vector3 pos, int label)
    {
        string line = $"{imgName},{pos.x},{pos.y},{pos.z},{label}\n";
        File.AppendAllText(csvPath, line);
    }
}
