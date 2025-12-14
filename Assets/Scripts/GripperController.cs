using UnityEngine;

public class GripperController : MonoBehaviour
{
    [Header("=== Objects ===")]
    public  Transform       leftFinger      = null;         // 왼쪽 집게
    public  Transform       rightFinger     = null;         // 오른쪽 집게

    // 각각의 집게에 붙어있는 물리 스크립트
    public  SimpleGripper   leftPhysic      = null;
    public  SimpleGripper   rightPhysic     = null;

    [Header("=== Setting (Check Inspector) ===")]
    public  float           openPos         = 0.0f;         // 열렸을 때 로컬 좌표
    public  float           closePos        = -0.0125f;     // 닫혔을 때 로컬 좌표
    public  float           moveSpeed       = 5.0f;

    // 움직임 축 설정 (x, y, z 중 집게가 움직이는 축 선택)
    public  enum            Axis            { X, Y, Z }
    public  Axis            movementAxis    = Axis.X;

    private float           currentTarget   = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OpenGripper();  // 시작 시 열기
    }

    private void FixedUpdate()
    {
        // 부드러운 움직임 (Lerp)
        float currentVal = GetAxisValue(leftFinger);
        float nextVal = Mathf.Lerp(currentVal, currentTarget, Time.deltaTime * moveSpeed);

        SetAxisValue(leftFinger, nextVal);
        SetAxisValue(rightFinger, -nextVal);
    }


    public void OpenGripper()
    {
        currentTarget = openPos;

        if (leftPhysic == true)
        {
            leftPhysic.Open();
        }

        if (rightPhysic == true)
        {
            rightPhysic.Open();
        }
    }

    public void CloseGripper()
    {
        currentTarget = closePos;

        if (leftPhysic == true)
        {
            leftPhysic.Close();
        }

        if (rightPhysic == true)
        {
            rightPhysic.Close();
        }
    }

    private float GetAxisValue(Transform tr)
    {
        if (movementAxis == Axis.X)
        {
            return tr.localPosition.x;
        }

        if (movementAxis == Axis.Y)
        {
            return tr.localPosition.y;
        }

        return tr.localPosition.z;
    }

    private void SetAxisValue(Transform tr, float val)
    {
        Vector3 pos = tr.localPosition;

        if (movementAxis == Axis.X)
        {
            tr.localPosition = new Vector3(val, pos.y, pos.z);
        }
        else if (movementAxis == Axis.Y)
        {
            tr.localPosition = new Vector3(pos.x, val, pos.z);
        }
        else
        {
            tr.localPosition = new Vector3(pos.x, pos.y, val);
        }
    }
}
