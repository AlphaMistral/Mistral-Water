using UnityEngine;

namespace LowPolyWaterv2Demo {
    public class DemoFreeFlyCamera : MonoBehaviour {

        public bool lockCursor = false;
        public float cameraSensitivity = 4;
        public float normalMoveSpeed = 10;
        public float smoothTime = 10f;

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        void Start() {
            if (lockCursor) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void Update() {
            var dt = Mathf.Clamp(Time.deltaTime, 0f, 0.03f);

            rotationX += Input.GetAxis("Mouse X") * cameraSensitivity;
            rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity;
            rotationY = Mathf.Clamp(rotationY, -90, 90);
            if (rotationX > 360) rotationX -= 360f;

            var locRot = Quaternion.AngleAxis(rotationX, Vector3.up);
            locRot *= Quaternion.AngleAxis(rotationY, Vector3.left);

            transform.localRotation = Quaternion.Slerp(transform.localRotation,
                locRot, smoothTime * dt);

            float speed = normalMoveSpeed * dt;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed *= 4;

            var deltaVertical = transform.forward;
            var deltaHorizontal = transform.right;

            if (Input.GetKey(KeyCode.Space)) { transform.position += Vector3.up * speed; }
            if (Input.GetKey(KeyCode.E)) { transform.position -= Vector3.up * speed; }

            transform.position += deltaVertical * speed * Input.GetAxis("Vertical");
            transform.position += deltaHorizontal * speed * Input.GetAxis("Horizontal");
        }
    }
}
