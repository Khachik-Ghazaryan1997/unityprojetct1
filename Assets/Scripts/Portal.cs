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

    /// Positions the portal camera behind the linked portal each frame so
    /// that looking through this portal feels like looking through a window
    /// into the linked portal's scene. Uses off-axis projection (asymmetric
    /// frustum) to produce correct parallax as the player moves around.
    void LateUpdate()
    {
        if (linkedPortal == null || portalCamera == null || player == null)
            return;

        // === Step 1: Mirror the player's position to the linked portal ===
        // Get the player's position in this portal's local space.
        Vector3 playerLocal = transform.InverseTransformPoint(player.transform.position);

        // Flip X and Z (180° Y-axis rotation) so that entering one portal
        // corresponds to exiting the other while facing the opposite direction.
        Vector3 flipped = new Vector3(-playerLocal.x, playerLocal.y, -playerLocal.z);

        // Place the camera at that mirrored position relative to the linked portal.
        portalCamera.transform.position = linkedPortal.transform.TransformPoint(flipped);

        // === Step 2: Orient the camera perpendicular to the linked portal ===
        // The camera always looks straight through the linked portal surface,
        // just like your eye direction doesn't change a real window's image.
        portalCamera.transform.rotation = Quaternion.LookRotation(
            linkedPortal.transform.forward,
            linkedPortal.transform.up
        );

        // === Step 3: Build an off-center (asymmetric) frustum ===
        // This is the key to the "window" effect. The near plane sits exactly
        // on the linked portal's surface, and the frustum edges match the
        // portal rectangle's edges as seen from the camera position.

        // Portal centre in camera-local space.
        Vector3 centerLocal = portalCamera.transform.InverseTransformPoint(
            linkedPortal.transform.position
        );

        // Near = distance from camera to the portal plane along the camera's
        // forward axis (local Z). The camera is behind the portal, so this is
        // always positive.
        float near = Mathf.Max(centerLocal.z, 0.01f);
        float far  = player.farClipPlane;

        // Half-extents of the portal rectangle.
        float hw = linkedPortal.planeSize.x * 0.5f;
        float hh = linkedPortal.planeSize.y * 0.5f;

        // centerLocal.x / .y give the lateral offset of the portal centre
        // from the camera's optical axis.  Shift the frustum accordingly.
        float left   = centerLocal.x - hw;
        float right  = centerLocal.x + hw;
        float bottom = centerLocal.y - hh;
        float top    = centerLocal.y + hh;

        portalCamera.projectionMatrix = Matrix4x4.Frustum(
            left, right, bottom, top, near, far
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
