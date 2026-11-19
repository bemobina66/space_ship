using UnityEngine;

public sealed class StarDrift : MonoBehaviour {
    public float speed;
    void Update() {
        transform.position += Vector3.down * (speed * Time.deltaTime);
        if (transform.position.y < -6) transform.position += Vector3.up * 12;
    }
}
