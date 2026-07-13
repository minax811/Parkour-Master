using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement player;
    public CharacterController controller;
    public Transform spawnPoint;

    [Header("Death")]
    public float killY = -5f;

    private float timer;
    private bool timerRunning;
    private bool finished;
    private float bestTime = -1f;

    void Start()
    {
        Respawn();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            Respawn();

        if (!finished)
        {
            if (!timerRunning && HasMoveInput())
                timerRunning = true;

            if (timerRunning)
                timer += Time.deltaTime;
        }

        if (player != null && player.transform.position.y < killY)
            Respawn();
    }

    bool HasMoveInput()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f
            || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
    }

    public void Respawn()
    {
        if (player == null || spawnPoint == null) return;

        controller.enabled = false;
        player.transform.position = spawnPoint.position;
        player.transform.rotation = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f);
        controller.enabled = true;

        player.ResetMovement();

        timer = 0f;
        timerRunning = false;
        finished = false;
    }

    public void Finish()
    {
        if (finished) return;

        finished = true;
        timerRunning = false;

        if (bestTime < 0f || timer < bestTime)
            bestTime = timer;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 34;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(30, 25, 400, 50), FormatTime(timer), style);

        GUIStyle small = new GUIStyle();
        small.fontSize = 20;
        small.normal.textColor = new Color(1f, 1f, 1f, 0.75f);

        if (bestTime >= 0f)
            GUI.Label(new Rect(30, 70, 400, 40), "Best  " + FormatTime(bestTime), small);

        GUI.Label(new Rect(30, Screen.height - 40, 400, 40), "R  restart", small);

        if (finished)
        {
            GUIStyle big = new GUIStyle();
            big.fontSize = 60;
            big.normal.textColor = new Color(0.9f, 0.2f, 0.2f);
            big.fontStyle = FontStyle.Bold;
            big.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0, Screen.height / 2 - 80, Screen.width, 80), "FINISH", big);

            GUIStyle mid = new GUIStyle();
            mid.fontSize = 32;
            mid.normal.textColor = Color.white;
            mid.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0, Screen.height / 2, Screen.width, 50), FormatTime(timer), mid);
        }
    }

    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        float seconds = t % 60f;
        return string.Format("{0:0}:{1:00.00}", minutes, seconds);
    }
}