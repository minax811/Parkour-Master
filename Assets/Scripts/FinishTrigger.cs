using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    public LevelManager levelManager;

    void OnTriggerEnter(Collider other)
    {
        if (levelManager == null) return;

        if (other.CompareTag("Player"))
            levelManager.Finish();
    }
}