using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lod
{
    public class PlayerMovement : MonoBehaviour
    {
        public float mouseSensitivity = 2.0f;
        private Transform pTransform;
        private Vector2 mouseLook;
        
        void Start()
        {
            pTransform = transform;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            pTransform.position += Input.GetAxis("Vertical") * transform.forward + Input.GetAxis("Horizontal") * transform.right;
            mouseLook.x += mouseSensitivity * Input.GetAxis("Mouse X");
            mouseLook.y -= mouseSensitivity * Input.GetAxis("Mouse Y");

            transform.rotation = Quaternion.Euler(mouseLook.y, mouseLook.x, 0);
        }
    }
}
