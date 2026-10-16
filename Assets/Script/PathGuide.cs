using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Draws a glowing path on the ground from the player to the current task target,
/// plus a tall beacon at the destination. Uses the NavMesh to follow walkable routes.
/// TaskListManager adds and controls this automatically, so you don't have to set anything up.
/// </summary>
public class PathGuide : MonoBehaviour
{
    [Header("Look")]
    public Color lineColor = new Color(1f, 0.85f, 0.2f, 1f);
    public float lineWidth = 0.25f;
    public bool dashed = true;
    [Tooltip("Length of one dash + gap, in meters.")]
    public float dashLength = 0.6f;
    public float heightOffset = 0.1f;
    [Tooltip("Optional. If empty, a simple material is created automatically.")]
    public Material lineMaterial;

    [Header("Behaviour")]
    public string playerTag = "Player";
    public float updateInterval = 0.1f;
    [Tooltip("The path line hides when the player is this close to the target.")]
    public float hideLineWithin = 2f;
    public bool showBeacon = true;
    public float beaconHeight = 6f;

    private Transform target;
    private Transform player;
    private LineRenderer line;
    private LineRenderer beacon;
    private NavMeshPath path;
    private readonly Vector3[] corners = new Vector3[64];
    private float timer;
    private bool warnedFallback;

    private void Awake()
    {
        path = new NavMeshPath();

        line = CreateLine("PathLine", true, dashed, lineColor, lineWidth);
        beacon = CreateLine("PathBeacon", false, false,
            new Color(lineColor.r, lineColor.g, lineColor.b, 0.5f), 0.35f);

        line.enabled = false;
        beacon.enabled = false;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        timer = 0f;

        if (target == null)
        {
            line.enabled = false;
            beacon.enabled = false;
            return;
        }

        if (showBeacon)
        {
            Vector3 ground = GroundPoint(target.position);
            beacon.positionCount = 2;
            beacon.SetPosition(0, ground);
            beacon.SetPosition(1, ground + Vector3.up * beaconHeight);
            beacon.enabled = true;
        }
    }

    private void Update()
    {
        if (target == null) return;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag(playerTag);
            if (p == null) return;
            player = p.transform;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = updateInterval;
            Recalculate();
        }
        else if (line.enabled && line.positionCount > 0)
        {
            // Keep the start of the line glued to the player between recalculations
            Vector3 start = line.GetPosition(0);
            line.SetPosition(0, new Vector3(player.position.x, start.y, player.position.z));
        }
    }

    private void Recalculate()
    {
        Vector3 flat = target.position - player.position;
        flat.y = 0f;
        if (flat.magnitude < hideLineWithin)
        {
            line.enabled = false;
            return;
        }

        Vector3 from = GroundPoint(player.position);
        Vector3 to = GroundPoint(target.position);

        if (NavMesh.SamplePosition(from, out NavMeshHit a, 4f, NavMesh.AllAreas)) from = a.position;
        if (NavMesh.SamplePosition(to, out NavMeshHit b, 4f, NavMesh.AllAreas)) to = b.position;

        int count = 0;
        if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) &&
            path.status != NavMeshPathStatus.PathInvalid)
        {
            count = path.GetCornersNonAlloc(corners);
        }

        if (count < 2)
        {
            // No NavMesh route: fall back to a straight line so the player still gets a direction
            corners[0] = from;
            corners[1] = to;
            count = 2;

            if (!warnedFallback)
            {
                Debug.LogWarning("PathGuide: no NavMesh path found, drawing a straight line. " +
                                 "Bake a NavMesh on your ground/floors for a proper route.");
                warnedFallback = true;
            }
        }

        line.positionCount = count;
        for (int i = 0; i < count; i++)
            line.SetPosition(i, corners[i] + Vector3.up * heightOffset);

        line.enabled = true;
    }

    private Vector3 GroundPoint(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 6f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.point;
        return p;
    }

    private LineRenderer CreateLine(string objName, bool flatOnGround, bool dash, Color color, float width)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(transform, false);

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.widthMultiplier = width;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        if (flatOnGround)
        {
            // Lie flat on the floor instead of facing the camera
            lr.alignment = LineAlignment.TransformZ;
            obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        if (lineMaterial != null)
        {
            lr.sharedMaterial = lineMaterial;
            lr.startColor = color;
            lr.endColor = color;
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            if (shader == null)
            {
                Debug.LogError("PathGuide: no usable shader found. Assign a Line Material in the Inspector.");
            }
            else
            {
                Material mat = new Material(shader);
                mat.color = color;
                lr.startColor = Color.white;
                lr.endColor = Color.white;

                if (dash)
                {
                    Texture2D tex = new Texture2D(2, 1, TextureFormat.RGBA32, false);
                    tex.SetPixels(new[] { Color.white, new Color(1f, 1f, 1f, 0f) });
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.filterMode = FilterMode.Point;
                    tex.Apply();

                    mat.mainTexture = tex;
                    mat.mainTextureScale = new Vector2(1f / Mathf.Max(0.05f, dashLength), 1f);
                    lr.textureMode = LineTextureMode.Tile;
                }

                lr.sharedMaterial = mat;
            }
        }

        return lr;
    }
}