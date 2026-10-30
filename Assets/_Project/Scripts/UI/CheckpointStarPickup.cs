using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Owns one route star's contact shape, idle motion and one-shot collection effect.</summary>
public sealed class CheckpointStarPickup : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private Transform waypoint;
    private Mesh ownedMesh;
    private Material ownedMaterial;
    private MeshCollider contactShape;
    private Light glow;
    private float hoverHeight;
    private float bobHeight;
    private float idleRotationSpeed;
    private float collectDuration;
    private float collectRise;
    private float collectRotationSpeed;
    private float elapsed;
    private Vector3 collectPosition;
    private Color initialColor;
    private Color initialEmission;
    private bool initialized;

    public bool IsCollected { get; private set; }

    public void Initialize(Transform target, Mesh mesh, Material material,
        float hover, float bob, float idleSpin, float duration, float rise, float collectSpin)
    {
        if (initialized) return;
        waypoint = target;
        ownedMesh = mesh;
        ownedMaterial = new Material(material);
        initialColor = ownedMaterial.GetColor(ColorId);
        initialEmission = ownedMaterial.GetColor(EmissionId);
        hoverHeight = hover;
        bobHeight = bob;
        idleRotationSpeed = idleSpin;
        collectDuration = Mathf.Max(0.05f, duration);
        collectRise = Mathf.Max(0f, rise);
        collectRotationSpeed = collectSpin;

        gameObject.AddComponent<MeshFilter>().sharedMesh = ownedMesh;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = ownedMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // A trigger hull follows the actual star mesh and its current scale/rotation.
        // Query explicitly against the player so AI, ragdolls and other triggers cannot collect it.
        contactShape = gameObject.AddComponent<MeshCollider>();
        contactShape.sharedMesh = ownedMesh;
        contactShape.convex = true;
        contactShape.isTrigger = true;

        GameObject lightObject = new GameObject("Star Light", typeof(Light));
        lightObject.transform.SetParent(transform, false);
        glow = lightObject.GetComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.5f, 0.06f);
        glow.range = 3.5f;
        glow.intensity = 1.4f;
        glow.shadows = LightShadows.None;
        initialized = true;
        UpdateIdlePose(0f);
    }

    public bool IsTouching(CharacterController player)
    {
        if (!initialized || IsCollected || !isActiveAndEnabled ||
            player == null || !player.enabled || !player.gameObject.activeInHierarchy) return false;

        return Physics.ComputePenetration(contactShape, transform.position, transform.rotation,
            player, player.transform.position, player.transform.rotation, out _, out _);
    }

    public void Collect()
    {
        if (!initialized || IsCollected) return;
        IsCollected = true;
        contactShape.enabled = false;
        collectPosition = transform.position;
        elapsed = 0f;
        gameObject.name = "Collected Checkpoint Star";
    }

    private void Update()
    {
        if (!initialized) return;
        if (!IsCollected)
        {
            UpdateIdlePose(Time.deltaTime);
            return;
        }

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / collectDuration);
        float easedProgress = 1f - (1f - progress) * (1f - progress);
        transform.position = collectPosition + Vector3.up * (collectRise * easedProgress);
        transform.Rotate(Vector3.up, collectRotationSpeed * Time.deltaTime, Space.World);
        Color color = initialColor;
        color.a *= 1f - progress;
        ownedMaterial.SetColor(ColorId, color);
        ownedMaterial.SetColor(EmissionId, initialEmission * (1f - progress));
        glow.intensity = 1.4f * (1f - progress);
        if (progress >= 1f) Destroy(gameObject);
    }

    private void UpdateIdlePose(float deltaTime)
    {
        if (waypoint == null) return;
        float hover = hoverHeight + Mathf.Sin(Time.time * 3f) * bobHeight;
        transform.position = waypoint.position + Vector3.up * hover;
        transform.Rotate(Vector3.up, idleRotationSpeed * deltaTime, Space.World);
    }

    private void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
        if (ownedMesh != null) Destroy(ownedMesh);
    }
}