using UnityEngine;

public class FirstPersonCameraRig : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;    // 내 플레이어.
    public Transform cameraPivot;   // 카메라 피봇.
    public Camera worldCamera;  // 메인 카메라.
    public Camera viewmodelCamera;  // 뷰 모델 카메라.
    public Transform viewmodelRoot; // 뷰 모델 루트.

    [Header("Settings")]
    public float eyeHeight = 1.6f;
    public float mouseSensitivity = 3.0f;
    public bool lockCursorOnStart = true;

    [Header("Alignment")]
    public float yawOffsetDegrees = 0.0f;      // 프리팹이 -Z 전방이면 180으로 보정
    public bool alignYawOnBind = true;         // 바인딩 직후 한 번 정렬
    public bool autoDetectReverse = true;      // 런타임에 전방 뒤집힘 감지 후 1회 자동수정

    private float yaw;
    private float pitch;
    private bool reverseFixedOnce;             // 자동수정 1회만 수행

    private void Start()
    {
        if (lockCursorOnStart == true)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (cameraPivot == null)
        {
            GameObject go = new GameObject("FPCameraPivot");
            cameraPivot = go.transform;
        }

        //// 자식 연결(로컬 회전 0을 강제)
        //if (worldCamera != null)
        //{
        //    //worldCamera.transform.SetParent(cameraPivot, false);
        //    //worldCamera.transform.localPosition = Vector3.zero;
        //    //worldCamera.transform.localRotation = Quaternion.identity;
        //}

        //if (viewmodelCamera != null)
        //{
        //    //viewmodelCamera.transform.SetParent(cameraPivot, false);
        //    //viewmodelCamera.transform.localPosition = Vector3.zero;
        //    //viewmodelCamera.transform.localRotation = Quaternion.identity;
        //}

        //if (viewmodelRoot != null)
        //{
        //    //viewmodelRoot.SetParent(cameraPivot, false);
        //}
    }

    private void Update()
    {
        // 1) 타깃 눈 위치로 이동
        if (target != null)
        {
            Vector3 eye = target.position + new Vector3(0.0f, eyeHeight, 0.0f);
            cameraPivot.position = eye;
        }

        // 2) 마우스 입력으로 yaw/pitch 누적
        float mdx = Input.GetAxis("Mouse X");
        float mdy = Input.GetAxis("Mouse Y");

        yaw = yaw + (mdx * mouseSensitivity);
        pitch = pitch - (mdy * mouseSensitivity);

        if (pitch < -80.0f)
        {
            pitch = -80.0f;
        }
        if (pitch > 80.0f)
        {
            pitch = 80.0f;
        }

        // 3) 기본 회전 적용
        cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0.0f);

        // 4) (선택) 역방향 자동 교정: 타깃 전방과 카메라 전방(dot<0)이면 180도 뒤집기 (1회)
        if (autoDetectReverse == true && reverseFixedOnce == false && target != null)
        {
            Vector3 tf = target.forward;
            tf.y = 0.0f;

            Vector3 cf = cameraPivot.forward;
            cf.y = 0.0f;

            if (tf.sqrMagnitude > 0.0001f && cf.sqrMagnitude > 0.0001f)
            {
                tf = tf.normalized;
                cf = cf.normalized;

                float dot = Vector3.Dot(tf, cf);
                if (dot < -0.5f)
                {
                    yaw = yaw + 180.0f;
                    cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0.0f);
                    reverseFixedOnce = true;
                }
            }
        }
    }

    public void SetAnglesFromState(float stateYaw, float statePitch)
    {
        yaw = stateYaw;
        pitch = statePitch;
    }

    public void AlignYawToTargetForward()
    {
        if (target == null)
        {
            return;
        }

        Vector3 f = target.forward;
        f.y = 0.0f;
        if (f.sqrMagnitude < 0.0001f)
        {
            return;
        }

        f = f.normalized;
        float baseYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        yaw = baseYaw + yawOffsetDegrees;

        // 1) yaw/pitch를 월드 전방 기준으로 리셋
        yaw = 0.0f;                                     // 수평각 0도;
        pitch = 0.0f;                                   // 수직각 0도;

        // 2) 즉시 반영
        if (cameraPivot != null)
        {
            cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0.0f);   // 피벗 회전 적용;
        }
    }
}
