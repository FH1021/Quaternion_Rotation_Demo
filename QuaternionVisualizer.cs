using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 四元数可视化（Scene 视图 Gizmos），作用于两个并排演示立方体：
///  - 蓝色立方体（四元数模式）：红色旋转轴、灰色原始基向量、RGB 旋转后基向量、
///    洋红示例向量（验证 v' = q·v·q⁻¹）；
///  - 橙色立方体（欧拉角模式）：灰色原始基向量 + 半透明 RGB 旋转后基向量（显示当前姿态）。
/// 选中物体时，上方显示文字标签与当前四元数/欧拉角分量。
/// 需要与 QuaternionRotationDemo 挂在同一物体上。
/// </summary>
[RequireComponent(typeof(QuaternionRotationDemo))]
public class QuaternionVisualizer : MonoBehaviour
{
    public float axisLength = 2.5f;    // 旋转轴绘制长度
    public float vectorLength = 1.5f;  // 基向量绘制长度

    private void OnDrawGizmos()
    {
        QuaternionRotationDemo demo = GetComponent<QuaternionRotationDemo>();
        if (demo == null) return;

        // ---- 四元数模式（蓝色立方体） ----
        Transform qt = demo.QuaternionTarget;
        if (qt != null)
        {
            Vector3 origin = qt.position;
            Quaternion q = qt.rotation;

            // 1. 旋转轴（红色双向箭头）
            Vector3 axis = demo.CurrentAxis;
            if (axis.sqrMagnitude > 0.001f)
            {
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                Gizmos.DrawRay(origin, axis * axisLength);
                Gizmos.DrawRay(origin, -axis * axisLength);
            }

            // 2. 未旋转时的基向量（灰色）
            Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.85f);
            Gizmos.DrawRay(origin, Vector3.right * vectorLength);
            Gizmos.DrawRay(origin, Vector3.up * vectorLength);
            Gizmos.DrawRay(origin, Vector3.forward * vectorLength);

            // 3. 旋转后的基向量（RGB）
            DrawRotatedBasis(origin, q, vectorLength, dim: false);

            // 4. 示例向量 v=(1,0,0) 旋转前后对比（洋红色）
            Vector3 vRot = q * Vector3.right;
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(origin, vRot * vectorLength);
            Gizmos.DrawSphere(origin + vRot * vectorLength, 0.08f);
        }

        // ---- 欧拉角模式（橙色立方体） ----
        Transform et = demo.EulerTarget;
        if (et != null)
        {
            Vector3 origin = et.position;
            Quaternion q = et.rotation;

            Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.85f);
            Gizmos.DrawRay(origin, Vector3.right * vectorLength);
            Gizmos.DrawRay(origin, Vector3.up * vectorLength);
            Gizmos.DrawRay(origin, Vector3.forward * vectorLength);

            DrawRotatedBasis(origin, q, vectorLength, dim: true);
        }
    }

    private static void DrawRotatedBasis(Vector3 origin, Quaternion rotation, float length, bool dim)
    {
        Color r = dim ? new Color(1f, 0.55f, 0.55f, 0.8f) : Color.red;
        Color g = dim ? new Color(0.55f, 1f, 0.55f, 0.8f) : Color.green;
        Color b = dim ? new Color(0.55f, 0.55f, 1f, 0.8f) : Color.blue;

        Gizmos.color = r;
        Gizmos.DrawRay(origin, rotation * Vector3.right * length);
        Gizmos.color = g;
        Gizmos.DrawRay(origin, rotation * Vector3.up * length);
        Gizmos.color = b;
        Gizmos.DrawRay(origin, rotation * Vector3.forward * length);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        QuaternionRotationDemo demo = GetComponent<QuaternionRotationDemo>();
        if (demo == null) return;

        if (demo.QuaternionTarget != null)
        {
            Transform t = demo.QuaternionTarget;
            Handles.color = new Color(0.15f, 0.4f, 0.9f);
            Handles.Label(t.position + Vector3.up * 0.7f,
                "四元数模式（蓝）\n" +
                $"q = ({demo.CurrentQuaternion.w:F3}, {demo.CurrentQuaternion.x:F3}, " +
                $"{demo.CurrentQuaternion.y:F3}, {demo.CurrentQuaternion.z:F3})");
        }
        if (demo.EulerTarget != null)
        {
            Transform t = demo.EulerTarget;
            Vector3 e = demo.CurrentEulerAngles;
            Handles.color = new Color(0.9f, 0.55f, 0.15f);
            Handles.Label(t.position + Vector3.up * 0.7f,
                "欧拉角模式（橙）\n" +
                $"euler = ({e.x:F1}°, {e.y:F1}°, {e.z:F1}°)");
        }
    }
#endif
}
