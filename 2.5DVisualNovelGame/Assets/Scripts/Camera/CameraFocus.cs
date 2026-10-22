using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// Moves the game camera toward a point over a set time (e.g. to show Stella coming in),
// then back to the player. The room's camera bounds still apply.
// Created automatically the first time it's used: CameraFocus.Instance.MoveTo(point, 2f);
public class CameraFocus : MonoBehaviour
{
    private static CameraFocus instance;

    private CinemachineCamera cam;
    private Transform followBefore;   // what the camera normally follows (the player)
    private Transform proxy;
    private Coroutine moving;

    public static CameraFocus Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("Camera Focus").AddComponent<CameraFocus>();
            return instance;
        }
    }

    private void Awake()
    {
        cam = FindFirstObjectByType<CinemachineCamera>();
        proxy = new GameObject("Camera Focus Point").transform;
        proxy.SetParent(transform);
    }

    // Glide the camera to this point in 'seconds' (it stays there until MoveBack).
    public void MoveTo(Transform point, float seconds)
    {
        if (point == null || cam == null)
            return;

        if (cam.Follow != proxy)
        {
            followBefore = cam.Follow;
            proxy.position = followBefore != null ? followBefore.position : point.position;
            cam.Follow = proxy;
        }

        Restart(Glide(() => point.position, seconds, false));
    }

    // Glide back to the player in 'seconds', then follow them normally again.
    public void MoveBack(float seconds)
    {
        if (cam == null || followBefore == null || cam.Follow != proxy)
            return;

        Restart(Glide(() => followBefore.position, seconds, true));
    }

    private void Restart(IEnumerator routine)
    {
        if (moving != null)
            StopCoroutine(moving);
        moving = StartCoroutine(routine);
    }

    private IEnumerator Glide(System.Func<Vector3> target, float seconds, bool backToPlayer)
    {
        Vector3 start = proxy.position;
        float t = 0f;
        while (t < 1f)
        {
            t += seconds > 0f ? Time.deltaTime / seconds : 1f;
            proxy.position = Vector3.Lerp(start, target(), Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        if (backToPlayer)
            cam.Follow = followBefore;
        moving = null;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
