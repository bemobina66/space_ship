using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public sealed class GamePresentation : MonoBehaviour {
    static readonly Color Ink = new Color(.13f,.025f,.035f,.96f), Red = new Color(.92f,.18f,.18f), Amber = new Color(1f,.72f,.28f), White = new Color(.93f,.97f,1f), Muted = new Color(.84f,.68f,.68f);
    [SerializeField] Font font;
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject menu, hud, pauseScreen, gameOverScreen, victoryScreen;
    [SerializeField] Text score, lives, stage, missiles, power, food, bossLabel, banner;
    [SerializeField] Image foodFill, bossFill;
    [SerializeField] Image[] lifeIcons = new Image[3];
    [SerializeField] Button newGameButton, continueButton;
    GameWorld world;
    int previousState = -1;

#if UNITY_EDITOR
    public static void AuthorScene(bool isLevel) {
        var host = new GameObject("Presentation").AddComponent<GamePresentation>();
        host.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var events = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var root = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(host.transform, false);
        host.canvas = root.GetComponent<Canvas>();
        host.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        host.canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0;
        if (isLevel) {
            host.BuildHud();
            host.pauseScreen = host.BuildOverlay(1);
            host.gameOverScreen = host.BuildOverlay(2);
            host.victoryScreen = host.BuildOverlay(3);
        } else host.BuildMenu();
    }
#endif
    void Start() {
        world = FindFirstObjectByType<GameWorld>();
        if (menu) {
            continueButton.interactable = SaveSystem.Read() != null;
            Select(newGameButton);
        }
        if (hud) { GameManager.I.Changed += RefreshHud; RefreshHud(); }
    }
    void Click() { GameManager.I.Sound("click"); }
    public void OnNewGame() { Click(); GameManager.I.NewGame(); }
    public void OnContinue() { Click(); GameManager.I.Continue(); }
    public void OnQuit() { Click(); Application.Quit(); }
    public void OnResume() { Click(); GameManager.I.SetPause(false); }
    public void OnRetryStage() { Click(); GameManager.I.gameOver = false; GameManager.I.LoadLevel(); }
    public void OnMainMenu() { Click(); GameManager.I.Menu(); }
    public void SetMusic(float value) { GameManager.I.music = value; GameManager.I.musicSource.volume = value; PlayerPrefs.SetFloat("music", value); }
    public void SetEffects(float value) { GameManager.I.effects = value; PlayerPrefs.SetFloat("effects", value); }
    void Select(Button button) { if (EventSystem.current && button) EventSystem.current.SetSelectedGameObject(button.gameObject); }
#if UNITY_EDITOR
    RectTransform Box(Transform parent, string name, float x, float y, float w, float h) { var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false); var r=g.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.sizeDelta=new Vector2(w,h); r.anchoredPosition=new Vector2(x,y); return r; }
    Image Panel(Transform parent, string name, float x,float y,float w,float h,Color color) { var r=Box(parent,name,x,y,w,h); var i=r.gameObject.AddComponent<Image>(); i.color=color; return i; }
    Text Label(Transform parent,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft,FontStyle style=FontStyle.Normal) { var r=Box(parent,value,x,y,w,h); var t=r.gameObject.AddComponent<Text>(); t.font=font; t.text=value; t.fontSize=size; t.color=color; t.alignment=align; t.fontStyle=style; t.resizeTextForBestFit=true; t.resizeTextMinSize=Mathf.Max(10,size-5); t.resizeTextMaxSize=size; t.raycastTarget=false; return t; }
    Button ButtonAt(Transform parent,string caption,float x,float y,System.Action action,bool enabled=true) { var img=Panel(parent,caption,x,y,300,52,enabled?new Color(.39f,.08f,.10f):new Color(.18f,.08f,.09f)); var button=img.gameObject.AddComponent<Button>(); button.interactable=enabled; var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.35f,1.35f,1.35f); colors.selectedColor=new Color(1.35f,1.35f,1.35f); colors.pressedColor=new Color(1f,.7f,.7f); colors.disabledColor=new Color(.65f,.65f,.65f); button.colors=colors;
        UnityEngine.Events.UnityAction callback;
        switch (caption) {
            case "NEW GAME": callback = OnNewGame; break;
            case "CONTINUE": callback = OnContinue; break;
            case "QUIT": callback = OnQuit; break;
            case "RESUME": callback = OnResume; break;
            case "RETRY STAGE": callback = OnRetryStage; break;
            default: callback = OnMainMenu; break;
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, callback);
        Label(img.transform,caption,0,0,280,48,18,White,TextAnchor.MiddleCenter,FontStyle.Bold); return button; }
    void BuildMenu() {
        menu=new GameObject("Main menu",typeof(RectTransform)); menu.transform.SetParent(canvas.transform,false); var r=menu.GetComponent<RectTransform>(); r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        Panel(menu.transform,"Menu shade",0,0,1800,1100,new Color(.07f,.01f,.015f,.70f));
        Panel(menu.transform,"Accent",-495,225,8,102,Amber);
        Label(menu.transform,"CHICKEN",-235,248,480,68,60,White,TextAnchor.MiddleLeft,FontStyle.Bold);
        Label(menu.transform,"SHOOTER",-235,187,480,68,60,Red,TextAnchor.MiddleLeft,FontStyle.Bold);
        Label(menu.transform,"Save the galaxy, one wave at a time.",-220,123,510,34,20,White);
        newGameButton=ButtonAt(menu.transform,"NEW GAME",-330,42,()=>GameManager.I.NewGame());
        continueButton=ButtonAt(menu.transform,"CONTINUE",-330,-22,()=>GameManager.I.Continue());
        ButtonAt(menu.transform,"QUIT",-330,-86,Application.Quit);
        Label(menu.transform,"FLIGHT CONTROLS",-330,-194,300,25,14,Amber,TextAnchor.MiddleLeft,FontStyle.Bold);
        Label(menu.transform,"MOVE  WASD / ARROWS\nFIRE  SPACE     MISSILE  X",-330,-235,300,58,16,White);
    }
    void BuildHud() {
        hud=new GameObject("Flight HUD",typeof(RectTransform));hud.transform.SetParent(canvas.transform,false);
        var r=hud.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        var strip=Panel(hud.transform,"Status strip",0,0,1216,66,Ink); strip.rectTransform.anchorMin=strip.rectTransform.anchorMax=new Vector2(.5f,1); strip.rectTransform.anchoredPosition=new Vector2(0,-45);
        score=Label(strip.transform,"",-490,2,190,48,21,White,TextAnchor.MiddleLeft,FontStyle.Bold);
        lives=Label(strip.transform,"LIVES",-315,2,75,48,15,Muted,TextAnchor.MiddleLeft,FontStyle.Bold);
        var ship=Resources.Load<Sprite>("Art/ship"); if(!ship){var tex=Resources.Load<Texture2D>("Art/ship");if(tex)ship=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100);}
        for(int i=0;i<3;i++){lifeIcons[i]=Panel(strip.transform,"Life",-250+i*32,2,25,30,White);lifeIcons[i].sprite=ship;lifeIcons[i].preserveAspect=true;}
        stage=Label(strip.transform,"",-65,2,190,48,15,White,TextAnchor.MiddleLeft,FontStyle.Bold);
        missiles=Label(strip.transform,"",145,2,180,48,18,White,TextAnchor.MiddleLeft,FontStyle.Bold);
        power=Label(strip.transform,"",315,2,135,48,18,White,TextAnchor.MiddleLeft,FontStyle.Bold);
        food=Label(strip.transform,"",465,12,155,22,13,Muted,TextAnchor.MiddleLeft,FontStyle.Bold);
        Panel(strip.transform,"Food track",470,-13,150,8,new Color(.29f,.12f,.13f)); foodFill=Panel(strip.transform,"Food fill",395,-13,150,8,Amber); foodFill.rectTransform.pivot=new Vector2(0,.5f);
        var bossTrack=Panel(hud.transform,"Boss health",0,0,520,14,new Color(.18f,.06f,.09f,.94f));bossTrack.rectTransform.anchorMin=bossTrack.rectTransform.anchorMax=new Vector2(.5f,1);bossTrack.rectTransform.anchoredPosition=new Vector2(0,-103);
        bossFill=Panel(bossTrack.transform,"Boss fill",-260,0,520,14,new Color(1,.35f,.25f));bossFill.rectTransform.pivot=new Vector2(0,.5f);
        bossLabel=Label(bossTrack.transform,"COMMANDER",0,-19,520,20,12,White,TextAnchor.MiddleCenter,FontStyle.Bold);
        banner=Label(hud.transform,"",0,0,700,80,36,White,TextAnchor.MiddleCenter,FontStyle.Bold);
    }
#endif
    void OnDestroy() {
        if (GameManager.I) GameManager.I.Changed -= RefreshHud;
    }
    void RefreshHud() {
        var gm = GameManager.I;
        if (!gm || !hud) return;
        var d = gm.data;
        score.text = "SCORE  " + d.score.ToString("D7");
        for (int i = 0; i < lifeIcons.Length; i++)
            lifeIcons[i].color = i < d.lives ? White : new Color(.42f, .25f, .25f, .5f);
        stage.text = "STAGE " + d.level + "   ROUND " + d.round;
        missiles.text = "MISSILES  " + d.missiles;
        power.text = "POWER  " + d.weapon;
        food.text = "FOOD  " + d.food + " / 10";
        foodFill.rectTransform.sizeDelta = new Vector2(150 * Mathf.Clamp01(d.food / 10f), 8);
        int state = gm.gameOver ? 2 : gm.victory ? 3 : gm.paused ? 1 : 0;
        if (state != previousState) {
            previousState = state;
            pauseScreen.SetActive(state == 1);
            gameOverScreen.SetActive(state == 2);
            victoryScreen.SetActive(state == 3);
            var active = state == 1 ? pauseScreen : state == 2 ? gameOverScreen : state == 3 ? victoryScreen : null;
            if (active) Select(active.GetComponentInChildren<Button>());
        }
    }
    void Update() {
        if (!hud) return;
        if (!world) world = FindFirstObjectByType<GameWorld>();
        var boss = world ? world.Boss : null;
        bool show = boss && boss.gameObject.activeSelf;
        bossFill.transform.parent.gameObject.SetActive(show);
        if (show) bossFill.rectTransform.sizeDelta = new Vector2(520 * Mathf.Clamp01(boss.hp / Mathf.Max(1, boss.maxHp)), 14);
        banner.text = world && world.BannerTime > 0 ? world.CurrentBanner : "";
    }
#if UNITY_EDITOR
    GameObject BuildOverlay(int state) {
        var overlay = new GameObject(state == 1 ? "Pause screen" : state == 2 ? "Game over screen" : "Victory screen", typeof(RectTransform));
        overlay.transform.SetParent(canvas.transform, false);
        Panel(overlay.transform, "Shade", 0, 0, 1800, 1100, new Color(.04f,.005f,.01f,.88f));
        string title = state == 1 ? "PAUSED" : state == 2 ? "MISSION FAILED" : "GALAXY SAVED";
        string sub = state == 1 ? "Take a breath, pilot." : state == 2 ? "Your stage checkpoint is ready." : "The invasion is over. Excellent flying.";
        Label(overlay.transform, title, 0, 125, 700, 76, 42, White, TextAnchor.MiddleCenter, FontStyle.Bold);
        Label(overlay.transform, sub, 0, 65, 600, 35, 18, Muted, TextAnchor.MiddleCenter);
        ButtonAt(overlay.transform, state == 1 ? "RESUME" : state == 2 ? "RETRY STAGE" : "NEW GAME", 0, -8, null);
        ButtonAt(overlay.transform, "MAIN MENU", 0, -72, null);
        overlay.SetActive(false);
        return overlay;
    }
#endif
}
