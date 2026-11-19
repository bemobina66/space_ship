using UnityEngine;

public sealed class Shockwave : MonoBehaviour {
    float age;
    SpriteRenderer sprite;
    public void Launch(Vector3 position) {
        transform.position = position;
        age = 0;
        gameObject.SetActive(true);
        if (!sprite) sprite = GetComponent<SpriteRenderer>();
    }
    void Update() {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / .55f);
        transform.localScale = Vector3.one * Mathf.Lerp(.4f, 10f, t);
        sprite.color = new Color(.35f, .9f, 1f, (1 - t) * .85f);
        if (t >= 1) gameObject.SetActive(false);
    }
}
