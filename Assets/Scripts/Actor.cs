using UnityEngine;

public enum BossPhase { Fan, AimedVolley, RapidFire }

public class Actor : MonoBehaviour
{
    public Kind kind;
    public float hp, maxHp, timer, age, fireInterval = 3, enemySpeed = 3, entryCurve = 1.1f;
    public Vector2 velocity, target, origin;
    public bool diving, enraged;
    public BossPhase phase;
    public SpriteRenderer sr;
    public BoxCollider2D col;
    public GameWorld world;
    [System.NonSerialized] public Actor prefabOrigin;
    Material defaultMaterial;
    static Material flashMaterial;
    float flashTime;

    void Awake() => EnsureComponents();

    void EnsureComponents()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = gameObject.AddComponent<SpriteRenderer>();
        if (!defaultMaterial) defaultMaterial = sr.sharedMaterial;
        if (!col) col = GetComponent<BoxCollider2D>();
        if (!col) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        var body = GetComponent<Rigidbody2D>();
        if (!body) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        body.gravityScale = 0;
    }

    public void Setup(GameWorld owner, Kind type, Vector2 position, Vector2 size, string sprite, Color tint, bool preservePresentation = false)
    {
        EnsureComponents();
        world = owner;
        kind = type;
        transform.position = position;
        if (!preservePresentation) transform.rotation = Quaternion.identity;
        if (!preservePresentation)
        {
            sr.sprite = owner.Sprite(sprite);
            var bounds = sr.sprite.bounds;
            float xScale = size.x / Mathf.Max(.01f, bounds.size.x);
            float yScale = size.y / Mathf.Max(.01f, bounds.size.y);
            float uniform = Mathf.Min(xScale, yScale);
            transform.localScale = type == Kind.Bullet ? new Vector3(xScale, yScale, 1) : new Vector3(uniform, uniform, 1);
            col.offset = bounds.center;
            col.size = bounds.size * .72f;
            sr.sortingOrder = type == Kind.Player ? 8 : type == Kind.Bullet ? 7 : 5;
        }
        sr.sharedMaterial = defaultMaterial;
        flashTime = 0;
        if (!preservePresentation) sr.color = tint;
        sr.enabled = true;
        hp = maxHp = timer = age = 0;
        fireInterval = 3;
        enemySpeed = 3;
        entryCurve = 1.1f;
        velocity = target = origin = Vector2.zero;
        diving = enraged = false;
        phase = BossPhase.Fan;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (world == null || GameManager.I.paused || GameManager.I.gameOver || GameManager.I.victory) return;
        float dt = Time.deltaTime;
        if (flashTime > 0)
        {
            flashTime -= dt;
            if (flashTime <= 0) sr.sharedMaterial = defaultMaterial;
        }
        age += dt;
        timer -= dt;
        if (kind == Kind.Player) { world.UpdatePlayer(this, dt); return; }
        if (kind == Kind.Bullet || kind == Kind.Egg || kind == Kind.Food || kind == Kind.Gift || kind == Kind.Asteroid)
        {
            transform.Translate(velocity * dt, Space.World);
            Vector3 p = transform.position;
            float xLimit = world.HalfWidth + 1;
            float yLimit = world.HalfHeight + 1;
            if ((p.y < -yLimit && velocity.y < 0) || (p.y > yLimit && velocity.y > 0)
                || (p.x < -xLimit && velocity.x < 0) || (p.x > xLimit && velocity.x > 0)) world.Release(this);
            return;
        }
        if (kind == Kind.Chicken) UpdateChicken(dt);
        else if (kind == Kind.Boss) UpdateBoss();
    }

    void UpdateChicken(float dt)
    {
        if (age < 1.5f)
        {
            float t = age / 1.5f;
            Vector2 control = (origin + target) * .5f + new Vector2(entryCurve, Mathf.Abs(entryCurve) * .55f);
            transform.position = (1 - t) * (1 - t) * origin + 2 * (1 - t) * t * control + t * t * target;
        }
        else if (diving)
        {
            transform.Translate(velocity * dt, Space.World);
            if (transform.position.y < -world.HalfHeight - 1 || Mathf.Abs(transform.position.x) > world.HalfWidth + 1) world.Release(this);
        }
        else
        {
            transform.position = target + Vector2.right * Mathf.Sin(Time.time * 1.1f + target.y) * .35f;
            if (timer > 0) return;
            if (Random.value < .16f + world.level * .06f)
            {
                diving = true;
                velocity = ((Vector2)world.player.transform.position - (Vector2)transform.position).normalized * enemySpeed;
                if (velocity.y > -.5f) velocity.y = -enemySpeed * .5f;
            }
            else
            {
                world.SpawnEgg(transform.position, Vector2.down * (2 + world.level * .35f));
                timer = Random.Range(.7f, 1.3f) * fireInterval;
                GameManager.I.Sound("chicken");
            }
        }
    }

    void UpdateBoss()
    {
        float edge = Mathf.Max(0, world.HalfWidth - sr.bounds.extents.x);
        transform.position = new Vector2(Mathf.Clamp(origin.x + Mathf.Sin(Time.time * .7f) * 2.4f, -edge, edge), origin.y);
        if (!enraged && hp / maxHp < .3f)
        {
            enraged = true;
            world.Banner("ENRAGED", 1.5f);
            world.SpawnHelpers();
        }
        if (timer > 0) return;
        switch (phase)
        {
            case BossPhase.Fan:
                for (int i = -2; i <= 2; i++) world.SpawnEgg(transform.position, new Vector2(i * .7f, -2.8f));
                phase = BossPhase.AimedVolley;
                break;
            case BossPhase.AimedVolley:
                for (int i = -1; i <= 1; i++)
                    world.SpawnEgg((Vector2)transform.position + Vector2.right * i * .5f,
                        ((Vector2)world.player.transform.position - (Vector2)transform.position).normalized * 3.3f + Vector2.right * i * .4f);
                phase = BossPhase.RapidFire;
                break;
            case BossPhase.RapidFire:
                world.SpawnEgg(transform.position, Vector2.down * 4.3f);
                if (enraged) world.SpawnHelpers();
                phase = BossPhase.Fan;
                break;
        }
        float cadence = Mathf.Max(.75f, 1.7f - (world.level - 1) * .22f);
        timer = enraged ? cadence * .58f : cadence;
    }

    public void Flash()
    {
        if (!flashMaterial)
        {
            var shader = Resources.Load<Shader>("WhiteFlash");
            if (shader) flashMaterial = new Material(shader);
        }
        if (!flashMaterial) return;
        sr.sharedMaterial = flashMaterial;
        flashTime = .12f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var otherActor = other.GetComponent<Actor>();
        if (!otherActor || !otherActor.gameObject.activeSelf) return;
        if (kind == Kind.Bullet && (otherActor.kind == Kind.Chicken || otherActor.kind == Kind.Boss || otherActor.kind == Kind.Asteroid || otherActor.kind == Kind.Egg))
        {
            float damage = hp;
            world.Release(this);
            world.Hit(otherActor, damage);
        }
        else if (kind == Kind.Player)
        {
            if (otherActor.kind == Kind.Egg || otherActor.kind == Kind.Chicken || otherActor.kind == Kind.Asteroid || otherActor.kind == Kind.Boss) world.PlayerHit(otherActor);
            else if (otherActor.kind == Kind.Food || otherActor.kind == Kind.Gift) world.Collect(otherActor);
        }
    }
}
