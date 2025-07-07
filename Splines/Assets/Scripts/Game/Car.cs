using UnityEngine;

public class Car : MonoBehaviour
{
    Rigidbody rb;

    [SerializeField] float horsePower = 100;
    [SerializeField] float steerStrength = 100;

    bool forwardInput = false;
    enum Steering { none, left, right }
    Steering steerDirection = Steering.none;

    void Awake()
    {
        if (!TryGetComponent(out rb)) Debug.LogError(name + " could not find Rigidbody component.", this);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            forwardInput = true;
        }
        else forwardInput = false;

        if (Input.GetKey(KeyCode.A)) steerDirection = Steering.left;
        else if (Input.GetKey(KeyCode.D)) steerDirection = Steering.right;
        else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D)) steerDirection = Steering.none;
        else steerDirection = Steering.none;
    }

    void FixedUpdate()
    {
        if (forwardInput) rb.AddForce(horsePower * Time.fixedDeltaTime * transform.forward, ForceMode.Impulse);

        if (steerDirection != Steering.none)
        {
            Vector3 direction = steerDirection == Steering.left ? -transform.up : transform.up;
            rb.AddTorque(steerStrength * Time.fixedDeltaTime * direction);
        }
    }

    public void ResetVelocity()
    {
        rb.linearVelocity = new();
        rb.angularVelocity = new();
    }
}
