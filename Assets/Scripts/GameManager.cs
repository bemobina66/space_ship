using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public SaveData data = new SaveData();
    public event Action Changed;
    public LevelDataSO[] levels;
    public WeaponDataSO weapon;
    public float music = .65f, effects = .8f;
    public bool paused, gameOver, victory;
    public AudioSource musicSource, effectsSource;
    bool sessionStarted;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        if (levels == null || levels.Length == 0) levels = Resources.LoadAll<LevelDataSO>("Data/Levels");
        Array.Sort(levels, (a, b) => a.levelIndex.CompareTo(b.levelIndex));
        if (!weapon) weapon = Resources.Load<WeaponDataSO>("Data/Weapon");
        music = PlayerPrefs.GetFloat("music", .65f);
        effects = PlayerPrefs.GetFloat("effects", .8f);
        if (!musicSource) musicSource = GetComponent<AudioSource>();
        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        if (!effectsSource) effectsSource = gameObject.AddComponent<AudioSource>();
        SceneManager.sceneLoaded += Loaded;
    }

    void OnDestroy() { SceneManager.sceneLoaded -= Loaded; if (I == this) I = null; }
    void Loaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1;
        paused = false;
        gameOver = false;
        if (scene.name.StartsWith("Level"))
        {
            if (!sessionStarted && int.TryParse(scene.name.Substring(5), out int directLevel) && directLevel >= 1 && directLevel <= 3)
            {
                data = new SaveData { level = directLevel };
                sessionStarted = true;
            }
            var cfg = levels.Length >= data.level ? levels[data.level - 1] : null;
            string clipName = cfg && !string.IsNullOrEmpty(cfg.musicClip) ? cfg.musicClip : "music" + data.level;
            var clip = Resources.Load<AudioClip>("Audio/" + clipName);
            if (clip) { musicSource.clip = clip; musicSource.volume = music; musicSource.Play(); }
        }
        else musicSource.Stop();
        Notify();
    }

    public void Notify() => Changed?.Invoke();
    public void NewGame() { sessionStarted = true; data = new SaveData(); victory = false; SaveSystem.Write(data); LoadLevel(); }
    public void Continue()
    {
        var saved = SaveSystem.Read();
        if (saved == null) return;
        sessionStarted = true; data = saved; victory = false; LoadLevel();
    }
    public void LoadLevel()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(levels.Length >= data.level ? levels[data.level - 1].sceneName : "Level" + data.level);
    }
    public void Menu() { Time.timeScale = 1; paused = false; SceneManager.LoadScene("MainMenu"); }
    public void SetPause(bool value) { paused = value; Time.timeScale = value ? 0 : 1; Notify(); }

    public void Damage()
    {
        if (gameOver) return;
        data.lives--;
        data.weapon = Mathf.Max(1, data.weapon - 1);
        if (data.lives <= 0)
        {
            SaveSystem.ResetForDeath(data);
            SaveSystem.Write(data);
            gameOver = true;
            Time.timeScale = 0;
        }
        Notify();
    }

    public void NextRound()
    {
        int max = levels.Length >= data.level && levels[data.level - 1].waves != null
            ? levels[data.level - 1].waves.Length : new[] { 5, 7, 10 }[data.level - 1];
        if (data.round < max)
        {
            data.round++;
            SaveSystem.Write(data);
        }
        else if (data.level < 3)
        {
            var next = levels.Length >= data.level ? levels[data.level - 1].nextLevel : null;
            data.level++;
            data.round = 1;
            data.entryScore = data.score;
            data.entryMissiles = data.missiles;
            data.entryWeapon = data.weapon;
            data.entryFood = data.food;
            SaveSystem.Write(data);
            if (next) SceneManager.LoadScene(next.sceneName); else LoadLevel();
        }
        else
        {
            SaveSystem.Delete();
            victory = true;
            Time.timeScale = 0;
        }
        Notify();
    }

    public void Sound(string name)
    {
        if (!clips.TryGetValue(name, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/" + name);
            clips[name] = clip;
        }
        if (clip) effectsSource.PlayOneShot(clip, effects);
    }
}
