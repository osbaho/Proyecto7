using UnityEngine;

public class WheelRotation : MonoBehaviour
{
    public Transform[] wheels;
    public float rotationSpeed = 300f;

    void Update()
    {
        float moveInput = Input.GetAxis("Vertical");

        foreach (Transform wheel in wheels)
        {
            wheel.Rotate(Vector3.right * moveInput * rotationSpeed * Time.deltaTime);
        }
    }
}
