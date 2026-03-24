using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class HDRPBuoyancy : MonoBehaviour
{
    [Header("Physics Config")]
    [SerializeField] private float waterDensity = 1025f;
    [SerializeField] private float gravityAccel = 9.81f;
    
    [Header("Mesh Config")]
    [SerializeField] private MeshFilter customMeshFilter;
    [SerializeField] private bool useOwnMeshFilter = true;
    
    [Header("Water Config")]
    [SerializeField] private WaterSurface waterSurface;
    [SerializeField] private bool autoFindWater = true;
    [SerializeField] private float waterPlaneHeight = 0f;
    
    [Header("Drag Config")]
    [SerializeField] private float linearDragCoefficient = 0.05f;
    [SerializeField] private float angularDragCoefficient = 0.02f;
    [SerializeField] private float velocityDamping = 0.95f;
    [SerializeField] private float angularVelocityDamping = 0.90f;
    
    [Header("Points")]
    [SerializeField] private int PointsCount = 20;
    
    [Header("Parent Rigidbody")]
    [SerializeField] private Rigidbody ParentRigidbody;
    
    private Rigidbody rb;
    private Mesh meshData;
    private Vector3[] samplePoints;
    
    private float lastWaterHeight = 0f;
    private Vector3 lastCenterOfBuoyancy;
    private float lastDisplacedVolume;
    private WaterSurface cachedWaterSurface;
    private static WaterSurface cachedWaterSurfaceStatic;

    void Start()
    {
        if(ParentRigidbody != null)
        {
            rb = ParentRigidbody;
        }
        if(ParentRigidbody == null)
        {
            Debug.LogError("No Rigidbody found for " + gameObject.name + ".");
            enabled = false;
            return;
        }

        MeshFilter mf = null;        
        if(useOwnMeshFilter)
        {
            mf = GetComponent<MeshFilter>();
        }
        else if(customMeshFilter != null)
        {
            mf = customMeshFilter;
        }

        if(mf == null)
        {
            Debug.LogError($"[Buoyancy] No MeshFilter found on {gameObject.name}!", gameObject);
            enabled = false;
            return;
        }
        meshData = mf.mesh;
        CreateSamplePoints();
        if(cachedWaterSurfaceStatic == null && autoFindWater)
        {
            cachedWaterSurfaceStatic = FindFirstObjectByType<WaterSurface>();
        }
        if(waterSurface == null)
        {
            waterSurface = cachedWaterSurfaceStatic;
        }
        cachedWaterSurface = waterSurface;
        if(cachedWaterSurface != null)
        {
            waterPlaneHeight = cachedWaterSurface.transform.position.y;
        }
    }

    void FixedUpdate()
    {
        if(rb == null || meshData == null || samplePoints == null)
        return;
        CalculateBuoyancy();
        ApplyWaterDamping();
    }

    void CreateSamplePoints()
    {
        Vector3[] meshVertices = meshData.vertices;
        int stepLength = Mathf.Max(1, meshVertices.Length / PointsCount);        
        System.Collections.Generic.List<Vector3> points = new System.Collections.Generic.List<Vector3>();       
        for(int i = 0; i < meshVertices.Length; i += stepLength)
        {
            points.Add(meshVertices[i]);
        }
        while(points.Count < PointsCount && points.Count < meshVertices.Length)
        {
            int randomIndex = Random.Range(0, meshVertices.Length);
            if(!points.Contains(meshVertices[randomIndex]))
                points.Add(meshVertices[randomIndex]);
        }
        samplePoints = points.ToArray();
    }

    float GetWaterSurfaceHeightAtPosition(Vector3 worldPosition)
    {
        if(cachedWaterSurface == null)
        return waterPlaneHeight;
        WaterSearchParameters searchParams = new WaterSearchParameters()
        {
            startPositionWS = worldPosition,
            targetPositionWS = worldPosition
        };
        WaterSearchResult searchResult;
        if(cachedWaterSurface.ProjectPointOnWaterSurface(searchParams, out searchResult))
        {
            return searchResult.projectedPositionWS.y;
        }
        return cachedWaterSurface.transform.position.y;
    }

    void CalculateBuoyancy()
    {
        Vector3 totalBuoyantForce = Vector3.zero;
        Vector3 centerOfBuoyancy = Vector3.zero;
        int pointsUnderwater = 0;
        Transform meshTransform = useOwnMeshFilter ? transform : customMeshFilter.transform;
        foreach(Vector3 localPoint in samplePoints)
        {
            Vector3 worldPoint = meshTransform.TransformPoint(localPoint);
            float waterHeight = GetWaterSurfaceHeightAtPosition(worldPoint);
            if (worldPoint.y < waterHeight)
            {
                pointsUnderwater++;
                centerOfBuoyancy += worldPoint;
                float depth = waterHeight - worldPoint.y;
                float pointForce = waterDensity * gravityAccel * (depth * depth) / samplePoints.Length;
                Vector3 buoyantForce = Vector3.up * pointForce;
                rb.AddForceAtPosition(buoyantForce, worldPoint, ForceMode.Force);
                totalBuoyantForce += buoyantForce;
            }
        }

        if(pointsUnderwater > 0)
        {
            centerOfBuoyancy /= pointsUnderwater;
            lastCenterOfBuoyancy = centerOfBuoyancy;
            lastDisplacedVolume = pointsUnderwater / (float)samplePoints.Length;
            lastWaterHeight = GetWaterSurfaceHeightAtPosition(transform.position);
            Vector3 rbCenterOfMass = rb.transform.TransformPoint(rb.centerOfMass);
            Vector3 leverArm = centerOfBuoyancy - rbCenterOfMass;
            Vector3 torque = Vector3.Cross(leverArm, totalBuoyantForce);
            rb.AddTorque(torque, ForceMode.Force);
        }
        else
        {
            lastDisplacedVolume = 0f;
        }
    }

    void ApplyWaterDamping()
    {
        int pointsUnderwater = 0;
        Transform meshTransform = useOwnMeshFilter ? transform : customMeshFilter.transform;
        foreach(Vector3 localPoint in samplePoints)
        {
            Vector3 worldPoint = meshTransform.TransformPoint(localPoint);
            float waterHeight = GetWaterSurfaceHeightAtPosition(worldPoint);

            if(worldPoint.y < waterHeight)
            {
                pointsUnderwater++;
            }
        }
        if(pointsUnderwater > 0)
        {
            float submergedFraction = pointsUnderwater / (float)samplePoints.Length;
            rb.linearVelocity *= Mathf.Lerp(1f, velocityDamping, submergedFraction);
            rb.angularVelocity *= Mathf.Lerp(1f, angularVelocityDamping, submergedFraction);
            Vector3 dragForce = -rb.linearVelocity * linearDragCoefficient * rb.mass * submergedFraction;
            rb.AddForce(dragForce, ForceMode.Force);
            Vector3 angularDragTorque = -rb.angularVelocity * angularDragCoefficient * rb.mass * submergedFraction;
            rb.AddTorque(angularDragTorque, ForceMode.Force);
        }
    }
}