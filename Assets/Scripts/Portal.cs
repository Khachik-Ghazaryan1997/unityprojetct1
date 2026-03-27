using UnityEngine;
using UnityEngine.Rendering;

public class Portal : MonoBehaviour
{
    [Header("Portal Components")]
    public MeshRenderer portalPlane;
    public Camera portalCamera;
    public Camera player;

    [Header("Portal Link")]
    public Portal linkedPortal;

    [Header("Settings")]
    public Vector2 planeSize = new Vector2(2f, 4f);
    public Vector2Int renderResolution = new Vector2Int(1024, 1024);

    private RenderTexture renderTexture;

    /// Called when the game starts. Initializes the render texture so the
    /// portal plane displays a live feed from the portal camera.
    void Start()
    {
        SetupRenderTexture();
    }

    /// Releases the render texture when the portal is destroyed to avoid memory leaks.
    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    /// Links this portal to another portal bidirectionally.
    /// Calling portalA.LinkTo(portalB) sets both portalA.linkedPortal = portalB
    /// and portalB.linkedPortal = portalA, so you only need to call it once.
    public void LinkTo(Portal other)
    {
        linkedPortal = other;
        if (other != null && other.linkedPortal != this)
            other.linkedPortal = this;
    }

    /// Creates a RenderTexture, assigns it to the portal camera, and applies
    /// it to the portal plane using an Unlit material so the live camera feed
    /// displays without lighting interference.
    void SetupRenderTexture()
    {
        if (portalCamera == null || portalPlane == null)
            return;

        if (renderTexture != null)
            renderTexture.Release();

        float aspect = planeSize.x / planeSize.y;
        int rtWidth = renderResolution.x;
        int rtHeight = Mathf.RoundToInt(rtWidth / aspect);
        renderTexture = new RenderTexture(rtWidth, rtHeight, 24);
        portalCamera.targetTexture = renderTexture;
        portalCamera.aspect = aspect;
        portalCamera.enabled = true;

        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
            unlitShader = Shader.Find("Unlit/Texture");

        if (unlitShader != null)
        {
            var mat = new Material(unlitShader);
            mat.SetTexture("_BaseMap", renderTexture);
            portalPlane.material = mat;
        }
        else
        {
            portalPlane.material.SetTexture("_BaseMap", renderTexture);
        }
    }

    /// Positions the portal camera at the linked portal each frame,
    /// facing outward along the linked portal's plane normal.
    void LateUpdate()
    {
        if (linkedPortal == null || portalCamera == null)
            return;

        
        Vector3 offset = player.transform.position - transform.position;
        Quaternion relativeRotation = linkedPortal.transform.rotation * Quaternion.Inverse(transform.rotation);
        offset = -(relativeRotation * offset);

        portalCamera.transform.position = linkedPortal.transform.position + offset;

        Vector3 direction = linkedPortal.transform.position - portalCamera.transform.position;

        //portalCamera.transform.rotation = Quaternion.LookRotation(direction);



        Matrix4x4 worldToCameraSpace = portalCamera.transform.worldToLocalMatrix;

        Vector3 portalInCameraSpace = worldToCameraSpace.MultiplyPoint3x4(linkedPortal.transform.position);

        Quaternion rotationOfPortal = Quaternion.FromToRotation(Vector3.forward, portalInCameraSpace); 

        Matrix4x4 T = Matrix4x4.Translate(-portalInCameraSpace);

        Matrix4x4 R = Matrix4x4.Rotate(rotationOfPortal);

        Matrix4x4 S = Matrix4x4.identity;

        Vector3 cameraNewLocation = R *T * portalCamera.transform.position;

        Debug.Log("Camera New Location: " + cameraNewLocation);
        S[0,2] = -cameraNewLocation.x/cameraNewLocation.z;
        S[1,2] = -cameraNewLocation.y/cameraNewLocation.z;

        Matrix4x4 toOrigin = Matrix4x4.Translate(new Vector3(0f, 0f, -cameraNewLocation.z));
        Matrix4x4 flipZ = Matrix4x4.Scale(new Vector3(1f, 1f, -1f));

        portalCamera.worldToCameraMatrix = flipZ * toOrigin *T * worldToCameraSpace;
        
        
        float near = Mathf.Max(-cameraNewLocation.z, 0.01f); 
        float far = portalCamera.farClipPlane;
        float aspect = planeSize.x / planeSize.y;
        float halfHeight = planeSize.y * 0.5f; 
        float halfWidth = halfHeight * aspect; 

        portalCamera.projectionMatrix = Matrix4x4.Frustum(
            -halfWidth, halfWidth,
            -halfHeight, halfHeight,
            near, far
        );
        
    
    }

    /// Called automatically by Unity whenever a serialized field is changed
    /// in the Inspector. Keeps the portal plane scale in sync with planeSize.
    void OnValidate()
    {
        if (portalPlane != null)
            portalPlane.transform.localScale = new Vector3(planeSize.x, planeSize.y, 1f);

        if (portalCamera != null && planeSize.y > 0f)
            portalCamera.aspect = planeSize.x / planeSize.y;
    }

    /// Called automatically by Unity when the component is first added to a
    /// GameObject, or when the user selects "Reset" from the Inspector context menu.
    /// Triggers SetupPortal() to create the child objects.
    void Reset()
    {
        SetupPortal();
    }

    /// Creates the child PortalPlane (a Quad with a white URP material) and
    /// PortalCamera if they don't already exist. Can be re-run from the Inspector
    /// by right-clicking the component header and selecting "Setup Portal".
    [ContextMenu("Setup Portal")]
    public void SetupPortal()
    {
        if (portalPlane == null)
        {
            var planeObj = new GameObject("PortalPlane");
            planeObj.transform.SetParent(transform, false);
            var meshFilter = planeObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            portalPlane = planeObj.AddComponent<MeshRenderer>();
            var defaultMat = GraphicsSettings.defaultRenderPipeline?.defaultMaterial;
            if (defaultMat != null)
                portalPlane.sharedMaterial = new Material(defaultMat);
            planeObj.transform.localScale = new Vector3(planeSize.x, planeSize.y, 1f);
        }

        if (portalCamera == null)
        {
            var camObj = new GameObject("PortalCamera");
            camObj.transform.SetParent(transform, false);
            camObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            portalCamera = camObj.AddComponent<Camera>();
            portalCamera.enabled = false;
        }
    }
}
