    using UnityEngine;

public class SpineGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject JointPrefab;

    [SerializeField]
    private float jointDistance;

    [SerializeField, Range(1, 100)]
    private int[] jointSizes;

    [SerializeField]
    private int finNumber;

    [SerializeField]
    private MeshFilter meshFilter;

    [SerializeField]
    private Material material;

    private Joint[] joints;

    private Bounds meshBounds;
    private MovementComponent movementComp;

    Vector3 extentsPosMax;
    Vector3 extendsPosMin;

    bool drawSpine;
    float jointSizeMultiplier = 0.005f;

    Vector4[] spinePointsUV;
    Vector4[] outlinePointsUV;
    Vector4[] eyePointsUV;
    Vector4[] finPointsUV;

    float[] finAngles;
    int[] finJointIndices = { 3, 7 };

    private void Start()
    {
        if (!JointPrefab)
            return;

        InstantiateJoints();

        movementComp = GetComponent<MovementComponent>();
        if (movementComp)
        {
            movementComp.head = joints[0];
        }

        meshBounds = meshFilter.sharedMesh.bounds;
        finAngles = new float[finNumber / 2];

        float[] sizeMult = new float[finNumber / 2];
        for (int i = 0; i < finJointIndices.Length; i++)
        {
            sizeMult[i] = jointSizes[i] * jointSizeMultiplier;
        }
        material.SetFloatArray("_FinSizeMult", sizeMult);
    }

    private void Update()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            joints[i].UpdatePosition();

            if (i > 1 && i < joints.Length)
            {
                Vector3 anchorPosition = joints[i].anchor.transform.position;

                Vector3 prevSegment = anchorPosition - joints[i].transform.position;
                Vector3 prevprevSegment = joints[i].anchor.anchor.transform.position - anchorPosition;

                float angle = Vector3.Angle(prevSegment, prevprevSegment);

                if (angle > movementComp.jointFlexibility)
                {
                    // Define how much to rotate (small incremental correction)
                    float angleDiff = angle - movementComp.jointFlexibility;

                    // Rotation axis based on cross product
                    Vector3 axis = Vector3.Cross(prevprevSegment, prevSegment).normalized;

                    // Apply small rotation to nudge joint back within the angle constraint
                    Quaternion rotation = Quaternion.AngleAxis(-angleDiff, axis);

                    // Rotate the vector and set the new position, ensuring the distance remains correct
                    Vector3 rotatedSegment = rotation * prevSegment;
                    joints[i].transform.position = anchorPosition - rotatedSegment.normalized * jointDistance;
                }
            }

            spinePointsUV = SpinePosToUV(joints);
            material.SetVectorArray("_SpinePoints", spinePointsUV);

            outlinePointsUV = OutlineToUV(joints);
            material.SetVectorArray("_OutlinePoints", outlinePointsUV);

            eyePointsUV = EyesToUV(joints);
            material.SetVectorArray("_EyePoints", eyePointsUV);

            
            finPointsUV = FinsToUV(joints, finJointIndices);
            material.SetVectorArray("_FinPoints", finPointsUV);
            material.SetFloatArray("_FinAngles", finAngles);
        }

        if(Input.GetKeyDown(KeyCode.S))
        {
            drawSpine = !drawSpine;
            material.SetInteger("_DrawSpine", drawSpine ? 1 : 0);
        }
    }

    Vector4[] FinsToUV(Joint[] joints, int[] jointIndices)
    {
        Vector3[] finPoints = new Vector3[finNumber];
        Vector4[] pointsUV = new Vector4[finNumber];

        for (int i = 0, j = 0; i < jointIndices.Length; i++, j += 2)
        {
            Vector3 finBone = joints[jointIndices[i]].transform.position - joints[jointIndices[i]].anchor.transform.position;

            Vector3 leftFinPoint = Quaternion.Euler(0, 0, -90.0f) * finBone;
            finPoints[j] = joints[jointIndices[i]].transform.position + (leftFinPoint.normalized * jointSizes[jointIndices[i]] * jointSizeMultiplier);

            Vector3 rightFinPoint = Quaternion.Euler(0, 0, 90.0f) * finBone;
            finPoints[j + 1] = joints[jointIndices[i]].transform.position + (rightFinPoint.normalized * jointSizes[jointIndices[i]] * jointSizeMultiplier);

            Vector3 finBoneNormalized = finBone.normalized;
            float angleToRight = Vector3.SignedAngle(finBone, Vector3.right, Vector3.back);
            finAngles[i] = angleToRight;
        }

        for (int i = 0; i < pointsUV.Length; i++)
        {
            pointsUV[i] = WorldPosToUV(finPoints[i]);
        }

        return pointsUV;
    }

    Vector4[] EyesToUV(Joint[] joints)
    {
        Vector3[] eyePoints = new Vector3[2];
        Vector4[] pointsUV = new Vector4[2];

        Vector3 headBone = joints[0].transform.position - joints[0].follower.transform.position;

        Vector3 leftHeadPoint = Quaternion.Euler(0, 0, -90.0f) * headBone;
        eyePoints[0] = joints[0].transform.position + (leftHeadPoint.normalized * jointSizes[0] * jointSizeMultiplier * 0.8f);

        Vector3 rightHeadPoint = Quaternion.Euler(0, 0, 90.0f) * headBone;
        eyePoints[1] = joints[0].transform.position + (rightHeadPoint.normalized * jointSizes[0] * jointSizeMultiplier * 0.8f);

        for (int i = 0; i < pointsUV.Length; i++)
        {
            pointsUV[i] = WorldPosToUV(eyePoints[i]);
        }

        return pointsUV;
    }

    Vector4[] OutlineToUV(Joint[] joints)
    {
        int numPoints = (joints.Length * 2) + 5;
        Vector3[] outlinePoints = new Vector3[numPoints];
        Vector4[] pointsUV = new Vector4[numPoints];

        // Left side points
        for (int i = 0; i < 10; i++)
        {
            Vector3 toNextJoint;
            Vector3 jointPosition = joints[i].transform.position;

            if (joints[i].follower)
            {
                toNextJoint = joints[i].follower.transform.position - jointPosition;
            }
            else
            {
                toNextJoint = -(joints[i].anchor.transform.position - jointPosition);
            }

            Vector3 leftPoint = Quaternion.Euler(0, 0, -90.0f) * toNextJoint;
            leftPoint = jointPosition + (leftPoint.normalized * jointSizes[i] * jointSizeMultiplier);

            outlinePoints[i] = leftPoint;
        }

        // Tail tip point
        Vector3 tailBone = -(joints[joints.Length - 1].anchor.transform.position - joints[joints.Length - 1].transform.position);
        Vector3 tailPoint = joints[joints.Length - 1].transform.position + (tailBone.normalized * jointSizes[joints.Length - 1] * jointSizeMultiplier);
        outlinePoints[10] = tailPoint;

        // Right side points
        for (int i = 9, j = 0; i > -1; i--, j++)
        {
            Vector3 toNextJoint;
            Vector3 jointPosition = joints[i].transform.position;

            if (joints[i].anchor)
            {
                toNextJoint = joints[i].anchor.transform.position - jointPosition;
            }
            else
            {
                toNextJoint = -(joints[i].follower.transform.position - jointPosition);
            }

            Vector3 leftPoint = Quaternion.Euler(0, 0, -90.0f) * toNextJoint;
            leftPoint = jointPosition + (leftPoint.normalized * jointSizes[i] * jointSizeMultiplier);

            outlinePoints[j + 11] = leftPoint;
        }

        // Head points
        Vector3 headBone = joints[0].transform.position - joints[0].follower.transform.position;

        Vector3 leftHeadPoint = Quaternion.Euler(0, 0, -45.0f) * headBone;
        outlinePoints[numPoints - 4] = joints[0].transform.position + (leftHeadPoint.normalized * jointSizes[0] * jointSizeMultiplier);

        outlinePoints[numPoints - 3] = joints[0].transform.position + (headBone.normalized * jointSizes[0] * jointSizeMultiplier);

        Vector3 rightHeadPoint = Quaternion.Euler(0, 0, 45.0f) * headBone;
        outlinePoints[numPoints - 2] = joints[0].transform.position + (rightHeadPoint.normalized * jointSizes[0] * jointSizeMultiplier);

        // Close the loop
        outlinePoints[numPoints - 1] = outlinePoints[0];
            
        for (int i = 0; i < pointsUV.Length; i++)
        {
            pointsUV[i] = WorldPosToUV(outlinePoints[i]);
        }

        return pointsUV;
    }

    Vector4[] SpinePosToUV(Joint[] joints)
    {
        Vector4[] pointsUV = new Vector4[10];
        for (int i = 0; i < pointsUV.Length; i++)
        {
            pointsUV[i] = WorldPosToUV(joints[i].transform.position);
        }

        return pointsUV;
    }

    private Vector2 WorldPosToUV(Vector3 worldPos)
    {
        Vector3 uvPos = new Vector3();
        Vector3 extendsWorld = Vector3.Scale(meshBounds.extents, meshFilter.transform.localScale);

        extentsPosMax = extendsWorld + transform.position;
        extendsPosMin = extentsPosMax - (extendsWorld * 2.0f);

        uvPos.x = (worldPos.x - extendsPosMin.x) / (extentsPosMax.x - extendsPosMin.x);
        uvPos.y = (worldPos.y - extendsPosMin.y) / (extentsPosMax.y - extendsPosMin.y);

        return uvPos;
    }

    void InstantiateJoints()
    {
        joints = new Joint[jointSizes.Length];

        for (int i = 0; i < jointSizes.Length; i++)
        {
            GameObject spawned = Instantiate(JointPrefab, transform.position + new Vector3(i * jointDistance, 0, 0), Quaternion.identity);
            spawned.transform.SetParent(transform);

            joints[i] = spawned.GetComponent<Joint>();
            if (joints[i] && i > 0)
            {
                joints[i].anchor = joints[i - 1];
            }
        }
    }
}
