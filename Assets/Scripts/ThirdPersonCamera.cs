using UnityEngine;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour {
	[SerializeField]
	public float distanceAway;
	[SerializeField]
	private float distanceUp;
	[SerializeField]
	private float smooth;

	private Transform followXForm;
	private Transform camParentRig;
	private Vector3 targetPosition;
	private Vector3 offset = new Vector3 (0f, 1.5f, 0f);

	private Vector3 lookDir;
	private Vector3 characterOffset;

	private Vector3 velocityCamSmooth= Vector3.zero;
	private float camSmoothDampTime = 0.2f;

	private Vector2 cameraInput;
	private Vector2 contJoyInput;
	private float mouseLeftClick;

	private float rotateSpeed = 0.5f;

	private PlayerInput player;

	public float pitch = 24.197f;
	public float yaw = 0.0f;
	public float rotationSmoothTime = 0.12f;
	Vector3 rotationSmoothVelocity;
	Vector3 currentRotation;
	public float mouseSensitivity = 5;
	public float controllerSensitivity = 10;
	public Vector2 pitchMinMax = new Vector2(1, 85);

	private Vector3 baseCamPos = Vector3.zero;

	private Vector3 myUp;
	private Vector3 myForward;

	public float resetTimer = 1.5f;
	private float resetCooldown = 0;
	private Vector3 targetRotation;

	RaycastHit hit;
	Vector3 ray;

	Transform camTargetPos;

	private LayerMask layerMask;

	public Transform lockOnTargetPos;
	public bool hasLockOnTarget;
	public Vector3 lockOnTargetDir;
	public Transform lookAtTarget;

	// Use this for initialization
	void Awake () {
		followXForm = GameObject.FindGameObjectWithTag ("Player").transform;
		camParentRig = GameObject.Find ("camParentRig").transform;
		player = GameObject.FindGameObjectWithTag ("Player").GetComponentInParent <PlayerInput> ();
		camTargetPos = GameObject.Find ("CameraTargetPos").transform;
		layerMask = 1 << 9;
		lookAtTarget = followXForm;
	}
	
	// Update is called once per frame
	void LateUpdate () {

		cameraInput = new Vector2 (Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y"));
		contJoyInput = new Vector2 (Input.GetAxis ("Mouse X_Joy"), Input.GetAxis ("Mouse Y_Joy"));
		mouseLeftClick = Input.GetAxis ("Fire1");

		characterOffset = followXForm.position + offset;

		//targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;
		lookDir = characterOffset - transform.position;
		lookDir.y = 0;
		lookDir.Normalize ();

		if (Input.GetAxisRaw ("WheelClick") != 0 && resetCooldown <= 0)
		{
			resetCooldown = resetTimer;
		}

		SetParent ();
		SetCamera ();
		TestForWall();

		transform.LookAt (lookAtTarget);
	}

	private void smoothPosition (Vector3 fromPos, Vector3 toPos)
	{
		transform.position = Vector3.SmoothDamp (fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
	}

	void ResetCameraRotations()
	{
		followXForm.rotation = player.transform.rotation;
	}

	void SetCamera()
	{
		/*if (resetCooldown > 0)
		{
			resetCooldown -= Time.deltaTime;
			ResetCamera ();
		}
		else if (mouseLeftClick != 0 || contJoyInput != Vector2.zero)
		{*/
		if (hasLockOnTarget)
		{
			FocusOnTarget ();
		}
		else
		{
			UserControl ();
			currentRotation = Vector3.SmoothDamp (currentRotation, targetRotation, ref rotationSmoothVelocity, rotationSmoothTime);
			transform.rotation = Quaternion.Euler (currentRotation);
		}
		Vector3 offsetPos = (Vector3.up * distanceUp) + (-transform.forward * distanceAway);
		transform.position = camParentRig.position + offsetPos;
		/*}
		else if (!player.isNotMoving)
		{
			//BehindTarget ();
		}*/
	}

	void FocusOnTarget()
	{
		lookAtTarget = lockOnTargetPos;
		camParentRig.LookAt (lookAtTarget);
		camParentRig.eulerAngles = new Vector3 (0, camParentRig.eulerAngles.y, 0);
		if (mouseLeftClick != 0 || contJoyInput != Vector2.zero)
		{
			if (mouseLeftClick != 0)
			{
				pitch -= cameraInput.y * mouseSensitivity;
			}
			else if (contJoyInput != Vector2.zero)
			{
				pitch -= contJoyInput.y * controllerSensitivity;
			}
			pitch = Mathf.Clamp (pitch, pitchMinMax.x, pitchMinMax.y);
		}
		targetRotation = new Vector3 (pitch, 0, 0);
	}

	void UserControl()
	{
		lookAtTarget = followXForm;
		if (mouseLeftClick != 0 || contJoyInput != Vector2.zero)
		{
			if (mouseLeftClick != 0)
			{
				yaw += cameraInput.x * mouseSensitivity;
				pitch -= cameraInput.y * mouseSensitivity;
			}
			else if (contJoyInput != Vector2.zero)
			{
				yaw += contJoyInput.x * controllerSensitivity;
				pitch -= contJoyInput.y * controllerSensitivity;
			}
			pitch = Mathf.Clamp (pitch, pitchMinMax.x, pitchMinMax.y);
		}
		targetRotation = new Vector3 (pitch, yaw, 0);
	}

	void BehindTarget()
	{
		targetRotation = camParentRig.rotation.eulerAngles;
	}

	void ResetCamera()
	{
		targetRotation = camParentRig.eulerAngles;
		if (targetRotation.y > 180)
		{
			targetRotation.y -= 360;
		}
		if (targetRotation.y < -180)
		{
			targetRotation.y += 360;
		}
		pitch = targetRotation.x;
		yaw = targetRotation.y;
		if (yaw > 360)
		{
			yaw -= 360;
		}
	}

	void SetParent()
	{
		if (camParentRig != null)
		{		
			camParentRig.rotation = player.transform.rotation;
			camParentRig.position = player.transform.position; //Vector3.SmoothDamp (camParentRig.position, player.transform.position, ref velocityCamSmooth, smooth);
		}
	}

	void TestForWall()
	{
		if (Physics.Linecast (followXForm.position, transform.position, out hit, layerMask))
		{
			transform.position = new Vector3 (hit.point.x, transform.position.y, hit.point.z);
		}
	}
}
