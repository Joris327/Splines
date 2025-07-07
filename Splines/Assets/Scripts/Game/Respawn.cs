using UnityEngine;

public class Respawn : MonoBehaviour
{
    [SerializeField] Car car;

    void Update()
    {
        if (car && Input.GetKeyDown(KeyCode.R))
        {
            car.ResetVelocity();
            car.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }
    }
}
