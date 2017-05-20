using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	public Transform target;
	public GameObject myTarget;

	public Vector2 pitchMinMax = new Vector2(1, 85);
	public Vector3 offset = new Vector3(0, 3, 6);

	public bool lockCursor = false;
	public bool moveBehindTarget = false;

	public float rotationSmoothTime = 0.12f;
	public float mouseSensitivity = 5;
	public float controllerSensitivity = 10;
	public float distFromTarget = 10;

	float pitch = 30;
	public float yaw;

	Vector3 rotationSmoothVelocity;
	Vector3 currentRotation;
	public Vector3 behindRotation;

	private Transform myTransform;
	private Vector3 targetPos;
	private Vector3 positionSmoothVelocity;
	public float camSmoothDampTime = 0.1f;

	// Use this for initialization
	void Start () {
		myTransform = GetComponent<Transform>();
		if (lockCursor) {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		behindRotation.x = pitch;
	}

	// Update is called once per frame
	void LateUpdate () {
		if (moveBehindTarget)
		{
			MoveBehind ();
		}
		else
		{
			ControlCamera ();
		}
		//myTransform.LookAt (target);
	}

	void MoveBehind()
	{
		targetPos = target.position + Vector3.up * offset.y - target.forward * offset.z;

		myTransform.position = Vector3.SmoothDamp (myTransform.position, targetPos, ref positionSmoothVelocity, camSmoothDampTime);
		//myTransform.position = Vector3.Lerp (myTransform.position, targetPos, Time.deltaTime * rotationSmoothTime);

		myTransform.LookAt (myTarget.transform.position);

		//currentRotation = Vector3.SmoothDamp (currentRotation, behindRotation, ref rotationSmoothVelocity, rotationSmoothTime);
	}

	void ControlCamera()
	{
		Vector2 cameraInput = new Vector2 (Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y"));
		Vector2 contJoyInput = new Vector2 (Input.GetAxis ("Mouse X_Joy"), Input.GetAxis ("Mouse Y_Joy"));

		if (Input.GetAxis ("Fire1") != 0)
		{
			yaw += cameraInput.x * mouseSensitivity;
			pitch -= cameraInput.y * mouseSensitivity;
			pitch = Mathf.Clamp (pitch, pitchMinMax.x, pitchMinMax.y);
		}
		else if (contJoyInput != Vector2.zero)
		{
			yaw += contJoyInput.x * controllerSensitivity;
			pitch -= contJoyInput.y * controllerSensitivity;
			pitch = Mathf.Clamp (pitch, pitchMinMax.x, pitchMinMax.y);
		}

		if (!moveBehindTarget)
		{
			currentRotation = Vector3.SmoothDamp (currentRotation, new Vector3 (pitch, yaw, 0), ref rotationSmoothVelocity, rotationSmoothTime);
		}
		else
		{
			currentRotation = Vector3.SmoothDamp (currentRotation, new Vector3 (pitch, myTarget.transform.localEulerAngles.y, 0), ref rotationSmoothVelocity, rotationSmoothTime);
		}
		//myTransform.eulerAngles = currentRotation;
		myTransform.LookAt (target.position);
		myTransform.position = target.position - (myTransform.forward * distFromTarget);
	}
}
