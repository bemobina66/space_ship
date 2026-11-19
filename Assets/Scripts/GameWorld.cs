using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameWorld : MonoBehaviour
{
    public int level;
    public Camera sceneCamera;
    public Actor player;
    public SpriteRenderer background;
    public LevelDataSO config;
    public Transform actorRoot, poolRoot, leftEntry, rightEntry;
    public SceneWaveLayout[] layouts;
    public Actor Boss => boss;
    public string CurrentBanner => banner;
    public float BannerTime => bannerTime;
    public float HalfWidth => cam.orthographicSize * cam.aspect;
    public float HalfHeight => cam.orthographicSize;

    readonly List<Actor> actors = new List<Actor>();
    readonly HashSet<Actor> authoredActors = new HashSet<Actor>();
    readonly Dictionary<Actor, Vector2> formationTargets = new Dictionary<Actor, Vector2>();
    readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    InputAction move, fire, missile, pause;
    float shot, invulnerable, between, bannerTime, shake;
    string banner = "";
    bool advancing;
    Camera cam;
    Actor boss;

    void Start()
    {
        cam = sceneCamera;
        if (!cam) { Debug.LogError("GameWorld requires an authored camera.", this); enabled = false; return; }
        CreateActions();
        if (poolRoot)
        {
            foreach (var seed in poolRoot.GetComponentsInChildren<Actor>(true))
            {
                seed.prefabOrigin = seed.kind == Kind.Bullet && GameManager.I.weapon ? GameManager.I.weapon.projectilePrefab : null;
                seed.gameObject.SetActive(false);
                actors.Add(seed);
            }
        }
        if (!config) return;
        level = config.levelIndex;
        if (!player || layouts == null || layouts.Length == 0)
        {
            Debug.LogError("GameWorld requires an authored player and round layouts.", this);
            enabled = false;
            return;
        }
        foreach (var layout in layouts)
        {
            if (!layout) continue;
            if (layout.actors != null)
                foreach (var actor in layout.actors)
                    if (actor)
                    {
                        formationTargets[actor] = actor.transform.position;
                        authoredActors.Add(actor);
                        actors.Add(actor);
                        actor.gameObject.SetActive(false);
                    }
            if (layout.boss)
            {
                authoredActors.Add(layout.boss);
                actors.Add(layout.boss);
                layout.boss.gameObject.SetActive(false);
            }
            layout.gameObject.SetActive(false);
        }
        player.Setup(this, Kind.Player, player.transform.position, Vector2.one * .9f, "ship", Color.white, true);
        StartWave();
    }

    void CreateActions()
    {
        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        fire = new InputAction("Fire", binding: "<Keyboard>/space");
        missile = new InputAction("Missile", binding: "<Keyboard>/x");
        pause = new InputAction("Pause", binding: "<Keyboard>/escape");
        move.Enable(); fire.Enable(); missile.Enable(); pause.Enable();
    }
    void OnDestroy() { move?.Dispose(); fire?.Dispose(); missile?.Dispose(); pause?.Dispose(); }

    public Sprite Sprite(string name)
    {
        if (sprites.TryGetValue(name, out var sprite)) return sprite;
        sprite = Resources.Load<Sprite>("Art/" + name);
        if (!sprite)
        {
            var texture = Resources.Load<Texture2D>("Art/" + name);
            if (texture) sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
        }
        if (!sprite)
        {
            var texture = new Texture2D(32, 32);
            var colors = new Color[1024];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                colors[y * 32 + x] = (x - 16) * (x - 16) + (y - 16) * (y - 16) < 200 ? Color.white : Color.clear;
            texture.SetPixels(colors); texture.Apply();
            sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 32);
        }
        sprites[name] = sprite;
        return sprite;
    }

    Actor Spawn(Kind kind, Vector2 position, Vector2 size, string sprite, Color tint, Actor prefab = null)
    {
        Actor actor = actors.Find(a => !authoredActors.Contains(a) && !a.gameObject.activeSelf && a.prefabOrigin == prefab);
        if (!actor)
        {
            actor = prefab ? Instantiate(prefab) : new GameObject("Pooled Actor").AddComponent<Actor>();
            actor.prefabOrigin = prefab;
            if (poolRoot) actor.transform.SetParent(poolRoot, true);
            actors.Add(actor);
        }
        actor.Setup(this, kind, position, size, sprite, tint);
        return actor;
    }
    public void Release(Actor actor) { if (actor && actor != player) actor.gameObject.SetActive(false); }

    void StartWave()
    {
        advancing = false;
        boss = null;
        int index = GameManager.I.data.round - 1;
        if (index < 0 || index >= layouts.Length || !layouts[index])
        {
            Debug.LogError("Missing authored layout for round " + (index + 1), this);
            enabled = false;
            return;
        }
        foreach (var item in layouts) if (item) item.gameObject.SetActive(false);
        var layout = layouts[index];
        layout.gameObject.SetActive(true);
        WaveDataSO wave = layout.wave;
        if (!wave && config.waves != null && index < config.waves.Length) wave = config.waves[index];
        if (!wave) { Debug.LogError("Missing wave data on " + layout.name, layout); enabled = false; return; }
        Banner(wave.boss ? "BOSS APPROACHING" : "ROUND " + (index + 1), 2.2f);
        if (wave.boss)
        {
            boss = layout.boss;
            if (!boss) { Debug.LogError("Boss layout needs an authored boss", layout); enabled = false; return; }
            boss.gameObject.SetActive(true);
            boss.Setup(this, Kind.Boss, boss.transform.position, Vector2.one * 2.5f, config.bossSprite, Color.white, true);
            boss.origin = boss.transform.position;
            boss.hp = boss.maxHp = 35 + level * 25;
            boss.timer = 2;
            return;
        }
        if (layout.actors == null) return;
        for (int i = 0; i < layout.actors.Length; i++)
        {
            var a = layout.actors[i];
            if (!a) continue;
            Vector2 target = formationTargets[a];
            a.gameObject.SetActive(true);
            if (wave.asteroids)
            {
                Vector2 origin = target + Vector2.up * (HalfHeight + 1);
                a.Setup(this, Kind.Asteroid, origin, Vector2.one * .7f, "asteroid", Color.white, true);
                a.velocity = new Vector2(Random.Range(-.3f, .3f), -Random.Range(1f, 2f));
                a.hp = 2;
            }
            else
            {
                Transform anchor = i % 2 == 0 ? leftEntry : rightEntry;
                Vector2 origin = anchor ? (Vector2)anchor.position : new Vector2(i % 2 == 0 ? -HalfWidth - 1 : HalfWidth + 1, 5);
                origin.y += i * .12f;
                a.Setup(this, Kind.Chicken, origin, Vector2.one * .8f, config.enemySprite, Color.white, true);
                a.origin = origin;
                a.target = new Vector2(Mathf.Clamp(target.x, -HalfWidth + .5f, HalfWidth - .5f), target.y);
                a.hp = wave.health;
                a.fireInterval = wave.fireInterval;
                a.enemySpeed = wave.enemySpeed;
                a.entryCurve = wave.entryCurve * (i % 2 == 0 ? 1 : -1);
                a.timer = Random.Range(2f, 5f);
            }
        }
    }

    public void SpawnHelpers()
    {
        string enemy = config && !string.IsNullOrEmpty(config.enemySprite) ? config.enemySprite : "chicken" + level;
        for (int i = 0; i < 2; i++)
        {
            var a = Spawn(Kind.Chicken, new Vector2(i == 0 ? -5 : 5, 5), Vector2.one * .65f, enemy, Color.white);
            a.origin = a.transform.position;
            float helperX = Mathf.Min(3, Mathf.Max(.2f, HalfWidth - .5f));
            a.target = new Vector2(i == 0 ? -helperX : helperX, 2);
            a.hp = 2; a.timer = 2;
        }
    }
    public void SpawnEgg(Vector2 position, Vector2 velocity)
    {
        var egg = Spawn(Kind.Egg, position, Vector2.one * .32f, "egg", Color.white);
        egg.velocity = velocity;
    }

    public void UpdatePlayer(Actor actor, float dt)
    {
        var v = move.ReadValue<Vector2>();
        float half = cam.orthographicSize * cam.aspect;
        var pos = actor.transform.position;
        pos.x = Mathf.Clamp(pos.x + v.x * 6 * dt, -half + .45f, half - .45f);
        pos.y = Mathf.Clamp(pos.y + v.y * 6 * dt, -5.05f, 5.05f);
        actor.transform.position = pos;
        invulnerable = Mathf.Max(0, invulnerable - dt);
        actor.sr.enabled = invulnerable <= 0 || Mathf.FloorToInt(invulnerable * 12) % 2 == 0;
        shot -= dt;
        if (!fire.IsPressed() || shot > 0) return;
        shot = GameManager.I.weapon ? GameManager.I.weapon.interval : .18f;
        int power = GameManager.I.data.weapon;
        var patterns = GameManager.I.weapon ? GameManager.I.weapon.patterns : null;
        WeaponPattern pattern = patterns != null && patterns.Length >= power ? patterns[power - 1] : (WeaponPattern)(power - 1);
        if (pattern == WeaponPattern.Single) Bullet(pos, 0, 0);
        else if (pattern == WeaponPattern.Double) { Bullet(pos, -.2f, 0); Bullet(pos, .2f, 0); }
        else { float spread = GameManager.I.weapon ? GameManager.I.weapon.spreadAngle : .18f; Bullet(pos, 0, 0); Bullet(pos, -.2f, -spread); Bullet(pos, .2f, spread); }
        GameManager.I.Sound("shoot");
    }
    void Bullet(Vector2 position, float offset, float angle)
    {
        var a = Spawn(Kind.Bullet, position + new Vector2(offset, .5f), new Vector2(.13f, .4f), GameManager.I.weapon ? GameManager.I.weapon.projectileSprite : "laser", Color.red, GameManager.I.weapon ? GameManager.I.weapon.projectilePrefab : null);
        float speed = GameManager.I.weapon ? GameManager.I.weapon.speed : 10;
        a.velocity = new Vector2(angle * speed, speed);
        a.hp = GameManager.I.weapon ? GameManager.I.weapon.damage : 1;
    }

    public void Hit(Actor actor, float damage)
    {
        if (!actor.gameObject.activeSelf) return;
        actor.hp -= damage;
        if (actor.kind == Kind.Egg) actor.hp = 0;
        if (actor.hp > 0)
        {
            actor.Flash();
            PresentationEffects.Burst(actor.transform.position, "hit");
            GameManager.I.Sound("hit");
            GameManager.I.Notify();
            return;
        }
        if (actor == boss) boss = null;
        var position = actor.transform.position;
        var kind = actor.kind;
        Release(actor);
        PresentationEffects.Burst(position, kind == Kind.Egg ? "egg" : "feather");
        if (kind != Kind.Chicken && kind != Kind.Boss && kind != Kind.Asteroid) return;
        GameManager.I.data.score += kind == Kind.Boss ? 2000 : kind == Kind.Asteroid ? 50 : 100;
        GameManager.I.Notify();
        GameManager.I.Sound("explosion");
        if (kind == Kind.Chicken && Random.value < .7f)
        {
            bool gift = Random.value < .16f;
            var drop = Spawn(gift ? Kind.Gift : Kind.Food, position, Vector2.one * .42f, gift ? "gift" : "food", Color.white);
            drop.velocity = Vector2.down * 1.2f;
        }
    }

    public void PlayerHit(Actor actor)
    {
        if (invulnerable > 0) return;
        invulnerable = 2;
        shake = .25f;
        if (actor.kind == Kind.Egg) Release(actor);
        PresentationEffects.Burst(player.transform.position, "hit");
        GameManager.I.Sound("hit");
        GameManager.I.Damage();
    }

    public void Collect(Actor actor)
    {
        if (actor.kind == Kind.Food)
        {
            GameManager.I.data.score += 25;
            GameManager.I.data.food++;
            if (GameManager.I.data.food >= 10)
            {
                GameManager.I.data.food = 0;
                GameManager.I.data.missiles++;
                Banner("MISSILE EARNED", 1.2f);
            }
        }
        else GameManager.I.data.weapon = Mathf.Min(3, GameManager.I.data.weapon + 1);
        Release(actor);
        GameManager.I.Sound("pickup");
        GameManager.I.Notify();
    }

    void Missile()
    {
        if (GameManager.I.data.missiles <= 0) return;
        GameManager.I.data.missiles--;
        foreach (var a in actors.ToArray())
        {
            if (!a.gameObject.activeInHierarchy) continue;
            if (a.kind == Kind.Chicken || a.kind == Kind.Egg) Hit(a, 999);
            else if (a.kind == Kind.Boss) Hit(a, a.maxHp * .18f);
        }
        PresentationEffects.Burst(player.transform.position, "missile");
        GameManager.I.Notify();
        GameManager.I.Sound("missile");
        shake = .5f;
        Banner("SCREEN CLEAR", 1);
    }

    public void Banner(string text, float duration) { banner = text; bannerTime = duration; }
    void Update()
    {
        if (level == 0) return;
        var gm = GameManager.I;
        if (pause.WasPressedThisFrame() && !gm.gameOver && !gm.victory) gm.SetPause(!gm.paused);
        if (gm.paused || gm.gameOver || gm.victory) return;
        if (missile.WasPressedThisFrame()) Missile();
        bannerTime -= Time.deltaTime;
        if (shake > 0)
        {
            shake -= Time.deltaTime;
            cam.transform.position = new Vector3(Random.Range(-.1f, .1f), Random.Range(-.1f, .1f), -10);
        }
        else cam.transform.position = new Vector3(0, 0, -10);
        if (!advancing && !actors.Exists(a => a.gameObject.activeInHierarchy && (a.kind == Kind.Chicken || a.kind == Kind.Boss || a.kind == Kind.Asteroid)))
        {
            advancing = true;
            between = 2;
            bool finalRound = config && config.waves != null && gm.data.round == config.waves.Length;
            Banner(finalRound ? "STAGE CLEAR" : "ROUND CLEAR", 2);
        }
        if (!advancing) return;
        between -= Time.deltaTime;
        if (between > 0) return;
        int previousLevel = gm.data.level;
        gm.NextRound();
        if (!gm.victory && gm.data.level == previousLevel) StartWave();
    }
}
