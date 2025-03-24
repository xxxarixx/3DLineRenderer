using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using LineRenderer3D;
using LineRenderer3D.Datas;
using UnityEditor.ShortcutManagement;

namespace LinerRenderer3D.Datas.Editor
{
    [EditorTool("Path manipulator", typeof(LineRenderer3DExe))]
    public class LRConfigEditor : EditorTool
    {
        [Shortcut("Active LR3D Path Manipulator", KeyCode.D)]
        static void Active3DLRPath()
        {
            if (Selection.GetFiltered<LineRenderer3DExe>(SelectionMode.TopLevel).Length > 0)
                ToolManager.SetActiveTool<LRConfigEditor>();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView)
                return;

            // Get the current camera position
            Camera sceneCamera = SceneView.currentDrawingSceneView.camera;
            Vector3 cameraPosition = sceneCamera.transform.position;
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(sceneCamera);

            foreach (var item in targets)
            {
                if (item is not LineRenderer3DExe exe)
                    continue;

                if (exe.Data.Config == null)
                    continue;

                var config = exe.Data.Config;
                for (int i = 0; i < config.PointsCount; i++)
                {
                    Vector3 point = config.GetPoint(i);
                    // Calculate the distance between the camera and the point
                    float distanceToCamera = Vector3.Distance(cameraPosition, point);

                    // Check if the point is within the camera's view frustum
                    Bounds pointBounds = new(point, Vector3.zero);
                    
                    bool isInView = GeometryUtility.TestPlanesAABB(frustumPlanes, pointBounds);

                    bool isVisible = !IsPointOccluded(sceneCamera, point);

                    if (distanceToCamera <= config.DisplayThreshold && isInView && isVisible)
                    {

                        EditorGUI.BeginChangeCheck();

                        // Draw the position handle for the point
                        Quaternion rotation = Quaternion.identity;
                        if (i == config.PointsCount - 1)
                            rotation = Quaternion.LookRotation((point - config.GetPoint(i - 1)).normalized);
                        else if (i > 0)
                            rotation = Quaternion.LookRotation(-(point - config.GetPoint(i - 1)).normalized);
                        else
                        {
                            LRData.SegmentInfo segmentInfo = exe.Data.GetSegmentInfo(i);
                            var dir = (segmentInfo.startSegmentCenter - segmentInfo.endSegmentCenter).normalized;
                            rotation = Quaternion.LookRotation(dir);
                        }
                        point = Handles.PositionHandle(point, rotation);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(config, "3DLR Change point position");
                            config.UpdatePointPosition(i, point);
                            EditorUtility.SetDirty(config);
                        }

                        // Draw buttons near the point
                        Handles.BeginGUI();
                        Vector2 guiPoint = HandleUtility.WorldToGUIPoint(point);

                        // Button for creating a new offset line
                        if (GUI.Button(new Rect(guiPoint.x + 30, guiPoint.y + 50, 50, 30), "+"))
                        {
                            AddPoint(exe.Data, i);
                        }

                        // Button for removing the point
                        if (GUI.Button(new Rect(guiPoint.x + 30, guiPoint.y + 40 + 50, 50, 30), "-"))
                        {
                            RemovePoint(config, i);
                        }
                        Handles.EndGUI();
                    }
                }
            }
        }

        bool IsPointOccluded(Camera camera, Vector3 point)
        {
            // Perform a raycast from the camera to the point
            Ray ray = new(camera.transform.position, (point - camera.transform.position).normalized);
            float distanceToPoint = Vector3.Distance(camera.transform.position, point);

            if (Physics.Raycast(ray, out RaycastHit hit, distanceToPoint))
            {
                // If the ray hits something before reaching the point, the point is occluded
                return true;
            }

            // The point is not occluded
            return false;
        }

        void AddPoint(LRData data, int index)
        {
            LRConfig config = data.Config;
            float offsetAmount = data.Config.Radius * 3f;
            Vector3 newPoint = Vector3.zero;
            if(index == 0)
            {
                LRData.SegmentInfo segmentInfo = data.GetSegmentInfo(index);
                newPoint = config.GetPoint(index) + (segmentInfo.startSegmentCenter - segmentInfo.endSegmentCenter).normalized * offsetAmount;
            }
            else
            {
                newPoint = config.GetPoint(index) + (config.GetPoint(index) - config.GetPoint(index - 1)).normalized * offsetAmount;
            }
            Debug.Log($"Added point index: {index}");
            Undo.RecordObject(config, "3DLR Created new point");
            config.InsertPoint(index == 0? index : index + 1, newPoint);
            EditorUtility.SetDirty(config);
        }

        void RemovePoint(LRConfig config, int index)
        {
            Undo.RecordObject(config, "3DLR Remove Point");
            config.RemovePoint(index);
            EditorUtility.SetDirty(config);
        }
    }
}