using UnityEngine;

/// <summary>
/// 四元数旋转变换 —— 核心交互演示（挂到四元数模式的蓝色立方体上，运行后操作 Game 视图右上角面板）
///
/// 设计：两个并排立方体同时接收相同输入，直观对比两种旋转表示：
///  - 蓝色立方体（四元数模式）：q = Δq·q（世界轴）或 q = q·Δq（局部轴），任意旋转无万向节死锁；
///  - 橙色立方体（欧拉角模式）：三个欧拉角直接累加，X 转到 ±90° 时出现万向节死锁。
///
/// 面板操作：旋转轴 X/Y/Z、角度滑块、旋转一次 / 自动旋转、世界轴/局部轴、重置。
/// Scene 视图配合 QuaternionVisualizer 观察旋转轴与基向量。
/// 坐标系说明：Unity 左手坐标系，绕 +Y 轴正向旋转 90° 时，+X 轴转到 −Z 轴方向。
/// </summary>
public class QuaternionRotationDemo : MonoBehaviour
{
    [Header("两种模式的演示物体（同步接收相同输入，便于对比）")]
    [Tooltip("四元数模式物体（蓝色）")]
    public Transform quaternionTarget;
    [Tooltip("欧拉角模式物体（橙色）")]
    public Transform eulerTarget;

    [Header("参数（面板与代码共用）")]
    public Vector3 rotationAxis = Vector3.up;   // 旋转轴（世界方向），面板可调
    public float rotateAngle = 30f;             // 单次旋转角度（度）；自动旋转时表示度/秒

    // ---- 运行状态 ----
    private Quaternion currentQuat = Quaternion.identity;
    private Vector3 currentEuler = Vector3.zero;

    // ---- GUI 状态 ----
    private Rect windowRect = new Rect(10, 10, 430, 470);
    private float axisX = 0f, axisY = 1f, axisZ = 0f;
    private bool useWorldAxis = true;           // true=左乘(世界轴) false=右乘(局部轴)
    [Tooltip("默认开启：进入 Play 后两个立方体自动连续旋转，便于一眼看出演示在运行")]
    public bool autoSpin = true;                // true=自动旋转（默认开启）

    // ================= 生命周期 =================

    private void Reset()
    {
        if (quaternionTarget == null && eulerTarget == null)
            quaternionTarget = transform;   // 挂到蓝色立方体上时默认旋转自身
    }

    private void Start()
    {
        currentQuat = quaternionTarget != null ? quaternionTarget.rotation : Quaternion.identity;
        currentEuler = eulerTarget != null ? eulerTarget.eulerAngles : Vector3.zero;
        axisX = rotationAxis.x; axisY = rotationAxis.y; axisZ = rotationAxis.z;
    }

    private void Update()
    {
        if (autoSpin) ApplyRotation(rotateAngle * Time.deltaTime);

        if (quaternionTarget != null) quaternionTarget.rotation = currentQuat;
        if (eulerTarget != null) eulerTarget.rotation = Quaternion.Euler(currentEuler);
    }

    // ================= 旋转操作（同时驱动两个立方体） =================

    /// <summary>两个立方体同时旋转：蓝=四元数累乘，橙=欧拉角累加</summary>
    public void ApplyRotation(float angle)
    {
        ApplyQuaternionRotation(angle, CurrentAxis);
        ApplyEulerRotation(CurrentAxis.normalized * angle);
    }

    /// <summary>四元数模式：绕方向 axis 旋转 angle 度</summary>
    public void ApplyQuaternionRotation(float angle, Vector3 axis)
    {
        if (axis.sqrMagnitude < 1e-6f) return;   // 零轴不做任何旋转
        Quaternion delta = Quaternion.AngleAxis(angle, axis.normalized);
        if (useWorldAxis)
            currentQuat = delta * currentQuat;   // 左乘：Δq 先发生（世界坐标轴下）
        else
            currentQuat = currentQuat * delta;   // 右乘：Δq 后发生（物体局部坐标轴下）
    }

    /// <summary>欧拉角模式：直接累加欧拉角增量（这正是万向节死锁的根源）</summary>
    public void ApplyEulerRotation(Vector3 eulerDelta)
    {
        currentEuler += eulerDelta;
        currentEuler.x = Wrap(currentEuler.x);
        currentEuler.y = Wrap(currentEuler.y);
        currentEuler.z = Wrap(currentEuler.z);
    }

    public void ResetRotation()
    {
        currentQuat = Quaternion.identity;
        currentEuler = Vector3.zero;
        if (quaternionTarget) quaternionTarget.rotation = Quaternion.identity;
        if (eulerTarget) eulerTarget.rotation = Quaternion.identity;
    }

    private static float Wrap(float v)
    {
        while (v >= 360f) v -= 360f;
        while (v < 0f) v += 360f;
        return v;
    }

    // ================= 供其他脚本读取 =================

    public Transform QuaternionTarget => quaternionTarget;
    public Transform EulerTarget => eulerTarget;
    public Quaternion CurrentQuaternion => currentQuat;
    public Vector3 CurrentEulerAngles => currentEuler;
    public Vector3 CurrentAxis => new Vector3(axisX, axisY, axisZ).normalized;

    // ================= Game 视图控制面板 =================

    private void OnGUI()
    {
        windowRect = GUI.Window(0, windowRect, DrawWindow, "四元数旋转变换演示");
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        GUILayout.Label("蓝 = 四元数累乘   橙 = 欧拉角累加（同步输入）");

        GUILayout.Space(4);
        GUILayout.Label("旋转轴方向（自动单位化）");
        SliderRow("X", ref axisX, -1f, 1f);
        SliderRow("Y", ref axisY, -1f, 1f);
        SliderRow("Z", ref axisZ, -1f, 1f);
        rotationAxis = new Vector3(axisX, axisY, axisZ);

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.Label("旋转角", GUILayout.Width(40));
        rotateAngle = GUILayout.HorizontalSlider(rotateAngle, -180f, 180f);
        GUILayout.Label(rotateAngle.ToString("F0") + "°", GUILayout.Width(44));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("旋转一次")) ApplyRotation(rotateAngle);
        autoSpin = GUILayout.Toggle(autoSpin, "自动旋转");
        GUILayout.EndHorizontal();

        GUILayout.Space(2);
        useWorldAxis = GUILayout.Toggle(useWorldAxis, "世界轴（左乘  q = Δq·q）");
        bool localAxis = GUILayout.Toggle(!useWorldAxis, "局部轴（右乘  q = q·Δq）");
        useWorldAxis = !localAxis;

        GUILayout.Space(6);
        if (GUILayout.Button("重置为初始姿态")) ResetRotation();

        GUILayout.Space(8);
        GUILayout.Box("实时状态");
        GUILayout.Label($"蓝·四元数 q = ({currentQuat.w:F4}, {currentQuat.x:F4}, {currentQuat.y:F4}, {currentQuat.z:F4})");
        float norm = Mathf.Sqrt(Quaternion.Dot(currentQuat, currentQuat));
        GUILayout.Label($"蓝·|q| = {norm:F4}   （归一化四元数应恒为 1）");
        Vector3 e = currentQuat.eulerAngles;
        GUILayout.Label($"蓝·等效欧拉角 = ({e.x:F1}°, {e.y:F1}°, {e.z:F1}°)");
        GUILayout.Label($"橙·欧拉角 = ({currentEuler.x:F1}°, {currentEuler.y:F1}°, {currentEuler.z:F1}°)");
        GUILayout.Label("提示：角度设 90° 连点多次，橙球在死锁点后行为异常");

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private static void SliderRow(string label, ref float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(36));
        value = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.Label(value.ToString("F2"), GUILayout.Width(52));
        GUILayout.EndHorizontal();
    }
}
