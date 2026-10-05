using UnityEngine;

/// <summary>
/// 四元数球面插值（Slerp）演示
///
/// 在 FromPose 与 ToPose 两个姿态之间插值：
///  - Quaternion.Slerp：沿四维单位球面的最短路径插值，角速度恒定、过渡平滑；
///  - Quaternion.Lerp：四个分量线性插值后归一化，中间段角速度明显偏快（视觉“不均匀”）；
///    Lerp 在大角度（如接近 180°）时甚至会穿过恒等姿态产生异常回卷。
///
/// 用途：相机平滑过渡、角色动画混合、机械臂/关节插值等。
/// 注：Slerp 默认走最短路径；若两姿态夹角超过 180°，应先用 -q 替换 q（q 与 -q 表示同一旋转）。
/// </summary>
public class QuaternionSlerpDemo : MonoBehaviour
{
    [Header("起点 / 终点姿态标记（场景中的两个空物体）")]
    public Transform fromPose;
    public Transform toPose;

    [Header("被插值的物体（留空 = 挂载本脚本的物体）")]
    public Transform subject;

    [Header("插值进度 t ∈ [0,1]")]
    [Range(0f, 1f)]
    public float progress = 0f;

    [Header("自动播放")]
    public bool autoPlay = true;
    public float speed = 0.3f;   // t 每秒变化量
    public bool pingPong = true;

    [Header("对比物体（可选）：使用 Lerp 线性插值，观察角速度不均匀）")]
    public Transform lerpComparison;

    private int direction = 1;
    private Rect windowRect = new Rect(440, 10, 330, 230);

    private void Reset()
    {
        subject = transform;
    }

    private void Start()
    {
        if (subject == null) subject = transform;
    }

    private void Update()
    {
        if (fromPose == null || toPose == null || subject == null) return;

        if (autoPlay)
        {
            progress += speed * Time.deltaTime * direction;
            if (progress >= 1f) { progress = 1f; if (pingPong) direction = -1; }
            if (progress <= 0f) { progress = 0f; if (pingPong) direction = 1; }
        }
        progress = Mathf.Clamp01(progress);

        Quaternion a = fromPose.rotation;
        Quaternion b = toPose.rotation;

        // 主旋转：球面插值（等角速度、最短路径）
        subject.rotation = Quaternion.Slerp(a, b, progress);

        // 对比：线性插值（角速度不均匀）
        if (lerpComparison != null)
            lerpComparison.rotation = Quaternion.Lerp(a, b, progress);
    }

    // ================= Game 视图控制面板 =================

    private void OnGUI()
    {
        windowRect = GUI.Window(1, windowRect, DrawWindow, "四元数球面插值 Slerp 演示");
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        autoPlay = GUILayout.Toggle(autoPlay, "自动播放");
        pingPong = GUILayout.Toggle(pingPong, "往复模式");

        GUILayout.BeginHorizontal();
        GUILayout.Label("速度", GUILayout.Width(40));
        speed = GUILayout.HorizontalSlider(speed, 0.05f, 1.5f);
        GUILayout.Label(speed.ToString("F2"), GUILayout.Width(44));
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.Label("进度 t", GUILayout.Width(44));
        progress = GUILayout.HorizontalSlider(progress, 0f, 1f);
        GUILayout.Label(progress.ToString("F2"), GUILayout.Width(44));
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        if (fromPose != null && toPose != null)
        {
            float diff = Quaternion.Angle(subject.rotation, toPose.rotation);
            GUILayout.Label($"当前与终点夹角 = {diff:F1}°");
        }
        GUILayout.Label("Slerp 等角速度；Lerp 中段明显更快");

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    // ================= Scene 视图标记 =================

    private void OnDrawGizmos()
    {
        if (fromPose != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(fromPose.position, 0.25f);
            Gizmos.DrawRay(fromPose.position, fromPose.forward * 0.8f);
        }
        if (toPose != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(toPose.position, 0.25f);
            Gizmos.DrawRay(toPose.position, toPose.forward * 0.8f);
        }
    }
}
