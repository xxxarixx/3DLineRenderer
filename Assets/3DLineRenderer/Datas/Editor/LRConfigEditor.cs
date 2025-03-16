using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using LineRenderer3D;

namespace LinerRenderer3D.Datas.Editor
{
    [EditorTool("Path manipulator", typeof(LineRenderer3DExe))]
    public class LRConfigEditor : EditorTool
    {
        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView)
                return;

            foreach (var item in targets)
            {
                if (item is not LineRenderer3DExe exe)
                    continue;

                if (exe.Data.Config == null)
                    continue;

                var config = exe.Data.Config;
                for (int i = 0; i < config.Points.Count; i++)
                {
                    Vector3 point = config.Points[i];

                    EditorGUI.BeginChangeCheck();

                    point = Handles.PositionHandle(point, Quaternion.identity);

                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(config, "3DLR Change point position");
                        config.Points[i] = point;
                        EditorUtility.SetDirty(config);
                    }
                }
            }
        }
    }
}