using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public sealed class PresentationEffects : MonoBehaviour {
    static PresentationEffects instance;
    [SerializeField] ParticleSystem sparks;
    [SerializeField] Shockwave[] shockwaves;
    GameWorld world;
    float exhaustTimer;

#if UNITY_EDITOR
    public static void AuthorScene() {
        var host = new GameObject("Space atmosphere").AddComponent<PresentationEffects>();
        var dot = AssetSprite("Dot", false);
        var ring = AssetSprite("Ring", true);
        var emitter = new GameObject("Pooled particles", typeof(ParticleSystem));
        emitter.transform.SetParent(host.transform, false);
        host.sparks = emitter.GetComponent<ParticleSystem>();
        var main = host.sparks.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;
        var emission = host.sparks.emission;
        emission.enabled = false;
        var renderer = emitter.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 12;
        const string materialPath = "Assets/Resources/Presentation/Particle.mat";
        var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material) {
            material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = dot.texture;
            UnityEditor.AssetDatabase.CreateAsset(material, materialPath);
        }
        renderer.sharedMaterial = material;
        host.shockwaves = new Shockwave[4];
        for (int i = 0; i < host.shockwaves.Length; i++) {
            var wave = SceneSprite(host.transform, "Missile shockwave", Vector3.zero, Color.clear, 11, ring);
            host.shockwaves[i] = wave.AddComponent<Shockwave>();
            wave.SetActive(false);
        }
        for (int layer = 0; layer < 3; layer++) for (int i = 0; i < 24; i++) {
            Color color = Color.Lerp(new Color(.3f,.65f,1f,.32f), new Color(.8f,.95f,1f,.8f), layer / 2f);
            var star = SceneSprite(host.transform, "Star", new Vector3(Random.Range(-12f,12f), Random.Range(-6f,6f), 0), color, -9 + layer, dot);
            star.transform.localScale = Vector3.one * (.025f + layer * .018f);
            star.AddComponent<StarDrift>().speed = .12f + layer * .18f;
        }
        var camera = Camera.main;
        if (camera) camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        UnityEditor.EditorUtility.SetDirty(host);
    }
    static Sprite AssetSprite(string name, bool outline) {
        string path = "Assets/Resources/Presentation/" + name + ".asset";
        var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing) return existing;
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name + " texture", filterMode = FilterMode.Bilinear };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
            float d = Vector2.Distance(new Vector2(x,y), new Vector2(31.5f,31.5f)) / 31.5f;
            float alpha = outline ? Mathf.Clamp01(1 - Mathf.Abs(d - .83f) * 18) : Mathf.Clamp01((1 - d) * 2);
            pixels[y * size + x] = new Color(1,1,1,alpha);
        }
        texture.SetPixels(pixels); texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(.5f,.5f), size);
        sprite.name = name;
        UnityEditor.AssetDatabase.CreateAsset(sprite, path);
        UnityEditor.AssetDatabase.AddObjectToAsset(texture, sprite);
        UnityEditor.EditorUtility.SetDirty(sprite);
        return sprite;
    }
    static GameObject SceneSprite(Transform parent, string name, Vector3 position, Color color, int order, Sprite sprite) {
        var item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        var sr = item.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
        return item;
    }
#endif
    void Awake() { instance = this; }
    void Start() { world = FindFirstObjectByType<GameWorld>(); }
    void Update() {
        if (!world) world = FindFirstObjectByType<GameWorld>();
        if (!world || !world.player || !world.player.gameObject.activeSelf || Time.timeScale <= 0) return;
        exhaustTimer -= Time.deltaTime;
        if (exhaustTimer > 0) return;
        exhaustTimer = .04f;
        Vector3 position = world.player.transform.position + new Vector3(Random.Range(-.13f, .13f), -.43f, 0);
        Emit(position, new Color(.3f, .9f, 1f, .8f), new Vector2(Random.Range(-.15f, .15f), Random.Range(-1.7f, -.7f)), .32f, .12f);
    }
    public static void Burst(Vector3 position, string kind) {
        if (!instance) return;
        int count = kind == "missile" ? 30 : kind == "feather" ? 14 : kind == "egg" ? 12 : 6;
        Color color = kind == "egg" ? new Color(1, .82f, .46f) : kind == "feather" ? new Color(1, .95f, .82f) : kind == "missile" ? new Color(.35f, .9f, 1) : new Color(1, .65f, .3f);
        for (int i = 0; i < count; i++) {
            float angle = i * Mathf.PI * 2 / count + Random.Range(-.15f, .15f);
            float speed = Random.Range(.8f, kind == "missile" ? 5f : 2.8f);
            instance.Emit(position, color, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed, Random.Range(.25f, .6f), kind == "missile" ? .18f : .1f);
        }
        if (kind == "missile") foreach (var wave in instance.shockwaves) if (!wave.gameObject.activeSelf) { wave.Launch(position); break; }
    }
    void Emit(Vector3 position, Color color, Vector2 velocity, float life, float size) {
        var particle = new ParticleSystem.EmitParams { position = position, velocity = velocity, startColor = color, startLifetime = life, startSize = size };
        sparks.Emit(particle, 1);
    }
    void OnDestroy() { if (instance == this) instance = null; }
}
