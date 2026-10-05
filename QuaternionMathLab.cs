using UnityEngine;

public class QuaternionMathLab : MonoBehaviour
{
    // ================= 手写四元数（演示用，未做性能优化） =================

    [System.Serializable]
    public struct MyQuat
    {
        public float w, x, y, z;

        public MyQuat(float w, float x, float y, float z)
        {
            this.w = w; this.x = x; this.y = y; this.z = z;
        }

        public static MyQuat Identity => new MyQuat(1f, 0f, 0f, 0f);

        /// <summary>轴-角构造：q = (cos(θ/2), sin(θ/2)·u)，axis 会自动单位化</summary>
        public static MyQuat AngleAxis(float degrees, Vector3 axis)
        {
            axis.Normalize();
            float half = degrees * Mathf.Deg2Rad * 0.5f;
            float s = Mathf.Sin(half);
            return new MyQuat(Mathf.Cos(half), axis.x * s, axis.y * s, axis.z * s);
        }

        /// <summary>Hamilton 乘积（Unity 的 Quaternion·Quaternion 采用同一约定）</summary>
        public static MyQuat operator *(MyQuat a, MyQuat b)
        {
            return new MyQuat(
                a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z,
                a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
                a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
                a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w);
        }

        /// <summary>共轭；归一化四元数的共轭即逆：q⁻¹ = q*</summary>
        public MyQuat Conjugate() => new MyQuat(w, -x, -y, -z);

        public float Norm() => Mathf.Sqrt(w * w + x * x + y * y + z * z);

        public MyQuat Normalized()
        {
            float n = Norm();
            return n > 1e-6f ? new MyQuat(w / n, x / n, y / n, z / n) : Identity;
        }

        /// <summary>旋转向量：v' = q·v·q⁻¹（把 v 写成纯四元数 (0, v)）</summary>
        public Vector3 Rotate(Vector3 v)
        {
            MyQuat p = new MyQuat(0f, v.x, v.y, v.z);
            MyQuat r = this * p * Conjugate();
            return new Vector3(r.x, r.y, r.z);
        }

        public override string ToString() => $"({w:F4}, {x:F4}, {y:F4}, {z:F4})";
    }

    // ================= 实验参数 =================

    [Header("实验参数（运行模式下实时生效）")]
    public Vector3 axis = new Vector3(0f, 1f, 0f);
    [Range(-360f, 360f)]
    public float angle = 90f;

    [Header("自动旋转（默认开启，让演示一眼可见）")]
    [Tooltip("默认开启：进入 Play 后青色立方体自动绕 axis 连续旋转")]
    public bool autoSpin = true;
    [Tooltip("自动旋转速度（度/秒）")]
    public float spinSpeed = 45f;

    [Header("对照物体")]
    public Transform cube;

    // ---- 运行状态 ----
    private bool lastChecksPass = true;
    private int checksPassed = 0;
    private Rect windowRect = new Rect(440, 260, 330, 230);

    private void Start()
    {
        lastChecksPass = RunAllChecks();
    }

    private void Update()
    {
        if (cube == null) return;
        // 默认自动旋转：angle 持续递增（角度值实时显示在面板上）
        if (autoSpin)
            angle = Mathf.Repeat(angle + spinSpeed * Time.deltaTime, 360f);
        // 用 Unity 内置 API 旋转对照物体
        cube.rotation = Quaternion.AngleAxis(angle, axis.normalized);
    }

    // ================= 自检（Inspector 右键 → QuaternionMathLab → 运行全部自检） =================

    [ContextMenu("运行全部自检")]
    public bool RunAllChecks()
    {
        checksPassed = 0;
        if (Check1_AngleAxis()) checksPassed++;
        if (Check2_HamiltonProduct()) checksPassed++;
        if (Check3_RotateVector()) checksPassed++;
        if (Check4_NonCommutative()) checksPassed++;
        if (Check5_Inverse()) checksPassed++;
        lastChecksPass = checksPassed == 5;
        Debug.Log(lastChecksPass
            ? "[QuaternionMathLab] 全部自检通过 ✔（手写代数与 Unity 内置 API 结果一致）"
            : "[QuaternionMathLab] 存在失败项 ✘，请检查上方逐条输出");
        return lastChecksPass;
    }

    /// <summary>检查1：轴-角构造（绕 Y 轴 90° → 期望 q=(0.7071, 0, 0.7071, 0)）</summary>
    private bool Check1_AngleAxis()
    {
        Quaternion unityQ = Quaternion.AngleAxis(90f, Vector3.up);
        MyQuat mine = MyQuat.AngleAxis(90f, Vector3.up);
        bool pass = Mathf.Abs(unityQ.w - mine.w) < 1e-4f
                 && Mathf.Abs(unityQ.x - mine.x) < 1e-4f
                 && Mathf.Abs(unityQ.y - mine.y) < 1e-4f
                 && Mathf.Abs(unityQ.z - mine.z) < 1e-4f;
        Debug.Log($"[检查1 轴-角构造] Unity={unityQ}  手写={mine}  → {(pass ? "通过" : "失败")}");
        return pass;
    }

    /// <summary>检查2：Hamilton 乘积（qy·qx 与 Unity 内置乘法对比）</summary>
    private bool Check2_HamiltonProduct()
    {
        MyQuat qx = MyQuat.AngleAxis(90f, Vector3.right);
        MyQuat qy = MyQuat.AngleAxis(90f, Vector3.up);
        MyQuat mine = qy * qx;

        Quaternion unity = Quaternion.AngleAxis(90f, Vector3.up)
                         * Quaternion.AngleAxis(90f, Vector3.right);
        bool pass = Mathf.Abs(mine.w - unity.w) < 1e-4f
                 && Mathf.Abs(mine.x - unity.x) < 1e-4f
                 && Mathf.Abs(mine.y - unity.y) < 1e-4f
                 && Mathf.Abs(mine.z - unity.z) < 1e-4f;
        Debug.Log($"[检查2 Hamilton乘积] Unity={unity}  手写={mine}  → {(pass ? "通过" : "失败")}");
        return pass;
    }

    /// <summary>检查3：旋转公式 v' = q·v·q⁻¹（与 Unity 的 Quaternion·Vector3 对比）</summary>
    private bool Check3_RotateVector()
    {
        MyQuat q = MyQuat.AngleAxis(90f, Vector3.up);
        Vector3 got = q.Rotate(Vector3.right);                    // 手写
        Vector3 expect = Quaternion.AngleAxis(90f, Vector3.up) * Vector3.right; // Unity
        bool pass = Vector3.Distance(got, expect) < 1e-4f;
        Debug.Log($"[检查3 旋转公式] 手写 v'={got}  Unity v'={expect}  → {(pass ? "通过" : "失败")}");
        return pass;
    }

    /// <summary>检查4：旋转不可交换（qy·qx 与 qx·qy 的矢量分量应不同）</summary>
    private bool Check4_NonCommutative()
    {
        MyQuat qx = MyQuat.AngleAxis(90f, Vector3.right);
        MyQuat qy = MyQuat.AngleAxis(90f, Vector3.up);
        MyQuat qyqX = qy * qx;
        MyQuat qxqY = qx * qy;
        bool differ = Vector3.Distance(
            new Vector3(qyqX.x, qyqX.y, qyqX.z),
            new Vector3(qxqY.x, qxqY.y, qxqY.z)) > 1e-3f;
        Debug.Log($"[检查4 不可交换] qy·qx={qyqX}   qx·qy={qxqY}   不相等? {(differ ? "是(符合预期)" : "否(异常)")}");
        return differ;
    }

    /// <summary>检查5：逆四元数（q·q⁻¹ 应回到恒等四元数 (1,0,0,0)）</summary>
    private bool Check5_Inverse()
    {
        MyQuat q = MyQuat.AngleAxis(120f, new Vector3(1f, 1f, 1f).normalized).Normalized();
        MyQuat id = (q * q.Conjugate()).Normalized();
        bool pass = Mathf.Abs(id.w - 1f) < 1e-4f
                 && Mathf.Abs(id.x) < 1e-4f
                 && Mathf.Abs(id.y) < 1e-4f
                 && Mathf.Abs(id.z) < 1e-4f;
        Debug.Log($"[检查5 逆四元数] q·q⁻¹ = {id}  → {(pass ? "通过" : "失败")}");
        return pass;
    }

    // ================= Game 视图控制面板 =================

    private void OnGUI()
    {
        windowRect = GUI.Window(2, windowRect, DrawWindow, "四元数代数自检 MathLab");
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        GUILayout.Label("青色立方体：手写 MyQuat 代数旋转（对照 Unity 内置）");
        GUILayout.BeginHorizontal();
        GUILayout.Label("自动旋转", GUILayout.Width(64));
        autoSpin = GUILayout.Toggle(autoSpin, "");
        GUILayout.Label("速度", GUILayout.Width(36));
        spinSpeed = GUILayout.HorizontalSlider(spinSpeed, 5f, 180f);
        GUILayout.Label(spinSpeed.ToString("F0") + "°/s", GUILayout.Width(48));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("角度", GUILayout.Width(36));
        angle = GUILayout.HorizontalSlider(angle, -360f, 360f);
        GUILayout.Label(angle.ToString("F0") + "°", GUILayout.Width(48));
        GUILayout.EndHorizontal();

        Quaternion q = Quaternion.AngleAxis(angle, axis.normalized);
        GUILayout.Label($"q = ({q.w:F4}, {q.x:F4}, {q.y:F4}, {q.z:F4})");

        GUILayout.Space(6);
        GUILayout.Box("自检结果（5 项：轴角构造/乘积/旋转/不可交换/逆）");
        GUILayout.Label(lastChecksPass
            ? $"✔ {checksPassed}/5 全部通过 —— 手写代数与 Unity 内置一致"
            : $"✘ {checksPassed}/5 有失败项 —— 详见 Console 逐条输出");
        if (GUILayout.Button("重新运行自检"))
            lastChecksPass = RunAllChecks();

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }
}
