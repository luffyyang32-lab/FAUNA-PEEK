using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;

    private float mouseX, mouseY;//获取鼠标移动值

    public float mouseSensitiviy;//鼠标灵敏度

    public float xRotation;


    private void Update()
    {
        mouseX = Input.GetAxis("Mouse X") * mouseSensitiviy * Time.deltaTime;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitiviy * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -70f, 70f);


        player.Rotate(Vector3.up * mouseX);
        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

    }
}