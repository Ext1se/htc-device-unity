using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VIVE_Trackers;

public class TrackerCalibration : MonoBehaviour
{
    [SerializeField] bool isStarted = false;
    [SerializeField] Transform controller;

    Vector3 lastPos;
    [Header("Calibrated data")]
    [SerializeField] Vector3 scale = Vector3.one;
    [SerializeField] Vector3 posOffset = Vector3.one;
    [SerializeField] Quaternion rotOffset = Quaternion.identity;

    List<Vector3> trackerPoints = new List<Vector3>();
    List<Vector3> controllerPoints = new List<Vector3>();

    List<Vector3> calibratedPositions = new List<Vector3>();
    List<Quaternion> calibratedRotations = new List<Quaternion>();
    List<Vector3> calibratedScales = new List<Vector3>();

    int indexdevice = -1;
    Quaternion trackerRotation;
    XRLabViveTracker tracker;

    [Header("Example")]
    [SerializeField] bool example_Calibrate;
    [SerializeField] bool example_started;
    [SerializeField] Material trackerLineMat;
    [SerializeField] LineRenderer example_trackerParent;
    [SerializeField] LineRenderer example_controller;
    [SerializeField] Vector3[] pts;

    IEnumerator Start()
    {
        yield return new WaitWhile(()=> IVIVEDongle.Instance == null);

        IVIVEDongle.Instance.OnButtonClicked += Instance_OnButtonClicked;
        IVIVEDongle.Instance.OnDisconnected += Instance_OnDisconnected;
    }

    private void Instance_OnDisconnected(int trackerIndx)
    {
        if (indexdevice == trackerIndx)
        {
            indexdevice = -1;
        }
    }

    private void Instance_OnButtonClicked(int trackerIndx)
    {
        if (indexdevice == -1 || indexdevice == trackerIndx)
        {
            indexdevice = trackerIndx;
            tracker = XRLabViveTracker.Get(indexdevice);
            isStarted = !isStarted;

            if (isStarted)
            {
                trackerPoints.Clear();
                controllerPoints.Clear();

                calibratedPositions.Clear();
                calibratedRotations.Clear();
                calibratedScales.Clear(); 
            }
        }
    }

    LineRenderer CreateParentObject()
    {
        var go = new GameObject("ParentTracker");
        go.transform.position = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
        go.transform.localRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        var lr = go.AddComponent<LineRenderer>();
        lr.widthCurve = new AnimationCurve(new Keyframe(0, 0.008f));
        lr.useWorldSpace = false;
        lr.material = trackerLineMat;
        return lr;
    }

    Vector3[] GenerateLinePoints()
    {
        Vector3[] points = new Vector3[3];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
        }

        return points;
    }

    IEnumerator Example()
    {
        if (example_trackerParent != null) Destroy(example_trackerParent.gameObject);
        example_trackerParent = CreateParentObject();
        //if(pts.Length == 0)
            pts = GenerateLinePoints();
        example_trackerParent.positionCount = pts.Length;
        example_trackerParent.SetPositions(pts);
        example_controller.positionCount = pts.Length;
        example_controller.SetPositions(pts);
        yield return new WaitForSeconds(0.5f);
        Calculate(pts, pts, example_trackerParent.transform, out _, out var offset);
        example_trackerParent.transform.position = offset;
    }

    static void Calculate(Vector3[] trackerLocalPoints, Vector3[] controllerWorldPoints, Transform trackerParent, out Vector3 scale, out Vector3 offset, Transform controller = null, Transform tracker = null)
    {
        #region Internal functions
        Vector3 Divide(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
        }
        Vector3 GetWorldDirection(Vector3[] pts, Transform tr, int indxTo, int indxFrom, bool normalize = true)
        {
            var vec = (tr.TransformPoint(pts[indxTo]) - tr.TransformPoint(pts[indxFrom]));
            if (normalize)
                vec.Normalize();
            return vec;
        }
        float GetAngleOnAxis(Vector3 from, Vector3 to, Vector3 axis, bool clockwise = false)
        {
            Vector3 right;
            if (clockwise)
            {
                right = Vector3.Cross(to, axis);
                to = Vector3.Cross(axis, right);
            }
            else
            {
                right = Vector3.Cross(axis, to);
                to = Vector3.Cross(right, axis);
            }
            return Mathf.Atan2(Vector3.Dot(from, right), Vector3.Dot(from, to)) * 57.2957795130823f;///Mathf.Rad2Deg;
        }
        #endregion

        Vector3 dir_tracker10 = GetWorldDirection(trackerLocalPoints, trackerParent, 1, 0, false);
        Vector3 dir_controller10 = (controllerWorldPoints[1] - controllerWorldPoints[0]);
        Vector3 dir_controller21 = (controllerWorldPoints[2] - controllerWorldPoints[1]);
        Vector3 dir_tracker21 = GetWorldDirection(trackerLocalPoints, trackerParent, 2, 1, false);

        Debug.Log($"tracker len 10: {dir_tracker10.magnitude}, controller len: {dir_controller10.magnitude}");
        Debug.Log($"tracker len 21: {dir_tracker21.magnitude}, controller len: {dir_controller21.magnitude}");

        var sc10 = Divide(dir_tracker10, dir_controller10);//new Vector3(dir_controller10.x / dir_tracker10.x, dir_controller10.y / dir_tracker10.y, dir_controller10.z / dir_tracker10.z);
        var sc21 = Divide(dir_tracker21, dir_controller21);// new Vector3(dir_controller21.x / dir_tracker21.x, dir_controller21.y / dir_tracker21.y, dir_controller21.z / dir_tracker21.z);
        scale = (sc10 + sc21) * 0.5f;
        dir_controller10.Normalize();
        dir_controller21.Normalize();
        dir_tracker10.Normalize();
        dir_tracker21.Normalize();

        var quat = trackerParent.rotation;
        var q1 = Quaternion.FromToRotation(dir_tracker10, dir_controller10); // это работает нормально
        trackerParent.rotation = q1 * quat;

        dir_tracker21 = GetWorldDirection(trackerLocalPoints, trackerParent, 2, 1);
        var ang = GetAngleOnAxis(dir_tracker21, dir_controller21, dir_controller10); // вот эта функция вычисляет правильный угол вместо Vector3.SignedAngle
        var q2 = Quaternion.Inverse(Quaternion.AngleAxis(ang, dir_controller10));
        //Debug.Log("ang2: " + ang);
        trackerParent.rotation *= q2;

        dir_tracker21 = GetWorldDirection(trackerLocalPoints, trackerParent, 2, 1);
        ang = Vector3.SignedAngle(dir_tracker21, dir_controller21, dir_controller10);
        if (controller != null && tracker != null)
            offset = controller.position - tracker.position;
        else
        {
            offset = controllerWorldPoints[0] - trackerParent.TransformPoint(trackerLocalPoints[0]);
            trackerParent.position += offset;
        }
        //trackerParent.position += diff;
        //Debug.Log("ang3: " + ang);
    }

    private void Update()
    {
        if(example_Calibrate)
        {
            example_Calibrate = false;
            StartCoroutine(Example());
        }

        if (!isStarted || tracker == null) return;

        lastPos = tracker.Device.trackData.position;
        //lastPos = Mul(lastPos, scale);
        if (example_trackerParent == null)
        {
            example_trackerParent = tracker.transform.parent.gameObject.AddComponent<LineRenderer>();
            example_trackerParent.widthCurve = new AnimationCurve(new Keyframe(0, 0.01f));
            example_trackerParent.useWorldSpace = false;
            example_trackerParent.positionCount = 3000;
            example_trackerParent.material = trackerLineMat;
            example_controller = controller.GetComponent<LineRenderer>();
            example_controller.positionCount = 3000;
        }
        if (calibratedPositions.Count * 3 < example_trackerParent.positionCount)
        {
            example_trackerParent.SetPosition(calibratedPositions.Count * 3 + trackerPoints.Count, lastPos);
            example_controller.SetPosition(calibratedPositions.Count * 3 + controllerPoints.Count, controller.position);
        }

        if (trackerPoints.Count < 3)
        {
            if (trackerPoints.Count == 0)
                trackerPoints.Add(lastPos);
        }
        if (controllerPoints.Count < 3)
        {
            if (controllerPoints.Count == 0)
                controllerPoints.Add(controller.position);
        }
        if (Vector3.Distance(trackerPoints[^1], lastPos) > 0.2f || Vector3.Distance(controllerPoints[^1], controller.position) > 0.2f)
        {
            trackerPoints.Add(lastPos);
            controllerPoints.Add(controller.position);
        }

        if (controllerPoints.Count == 3 && trackerPoints.Count == 3)
        {
            var trackerParent = tracker.transform.parent;
            Calculate(trackerPoints.ToArray(), controllerPoints.ToArray(), trackerParent, out scale, out var offset, controller, tracker.transform);
            calibratedPositions.Add(offset);
            calibratedRotations.Add(trackerParent.rotation);
            calibratedScales.Add(scale);

            Vector3 sc = calibratedScales[0];
            for (int i = 1; i < calibratedScales.Count; i++)
                sc += calibratedScales[i];
            sc /= calibratedScales.Count;
            scale = sc;

            Vector3 pos = calibratedPositions[0];
            for (int i = 1; i < calibratedPositions.Count; i++)
                pos += calibratedPositions[i];
            pos /= calibratedPositions.Count;

            float x = calibratedRotations[0].x, y = calibratedRotations[0].y, z = calibratedRotations[0].z, w = calibratedRotations[0].w;
            for (int i = 1; i < calibratedRotations.Count; i++)
            {
                x += calibratedRotations[i].x;
                y += calibratedRotations[i].y;
                z += calibratedRotations[i].z;
                w += calibratedRotations[i].w;
            }

            trackerParent.position = offset;
            trackerParent.rotation = new Quaternion(x / calibratedRotations.Count, y / calibratedRotations.Count, z / calibratedRotations.Count, w / calibratedRotations.Count);
            //Debug.Log("pos: " + tracker.transform.parent.position);
            //Debug.Log("rot: " + tracker.transform.parent.eulerAngles);
            posOffset = pos;
            rotOffset = trackerParent.rotation;
            trackerPoints.Clear();
            controllerPoints.Clear();
            if(calibratedPositions.Count > 10)
            {
                calibratedPositions.Clear();
                calibratedRotations.Clear();
            }
        }
    }

    void CheckAndSet()
    {
        Vector3 pos = calibratedPositions[0];
        for (int i = 1; i < calibratedPositions.Count; i++)
            pos += calibratedPositions[i];
        pos /= calibratedPositions.Count;

        //float x = calibratedRotations[0].x, y = calibratedRotations[0].y, z = calibratedRotations[0].z, w = calibratedRotations[0].w;
        //for (int i = 1; i < calibratedRotations.Count; i++)
        //{
        //    x += calibratedRotations[i].x;
        //    y += calibratedRotations[i].y;
        //    z += calibratedRotations[i].z;
        //    w += calibratedRotations[i].w;
        //}
        //var midQuat = new Quaternion(x / calibratedRotations.Count, y / calibratedRotations.Count, z / calibratedRotations.Count, w / calibratedRotations.Count);
        var midQuat = calibratedRotations[0];
        for (int i = 1; i < calibratedRotations.Count; i++)
        {
            midQuat = Quaternion.Lerp(midQuat, calibratedRotations[i], 0.5f);
        }
    }
}
