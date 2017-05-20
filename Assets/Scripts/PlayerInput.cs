using UnityEngine;
using System.Collections;

public class PlayerInput : MonoBehaviour {

	Rigidbody myRigidbody;
	Animator myAnimator;
	ThirdPersonCamera myCam;

	public GameObject cube;

	public enum WallJumps
	{
		none,
		sideWall,
		vertical,
		wallSwap,
		wallLeap
	}

	[Header("Speed Variables")]
	public float walkSpeed = 6.0f;
	public float sprintSpeed = 36.0f;
	public float runSpeed = 24.0f;
	public float airMoveSpeed = 0;
	public float walkSpeedTime = 3;
	public float speedSmoothTime = 0.1f;
	public float directionSpeed = 3.0f;

	[Header("Jump Variables")]
	public float timeToJumpApex = 0.5f;
	public float jumpHeight = 6.0f;

	[Header("Wallrun Variables")]
	public float wallRunMaxDist = 10.0f;
	public float wallRunMaxHeight = 4.0f;
	public float wallRunSlowSpeed = 2.0f;

	[Header("Special Ability Variables")]
	public float teleportDist = 100.0f;
	public float abilityCoolDown = 5;
	public float abilityTimer = 0;
	public float rollTimer = 0;

	[Header("Smoothing Variables")]
	public float groundTurnSmoothTime = 0.1f;
	public float airTurnSmoothTime = 1.0f;
	public float jumpTurnSmoothTime = 2.0f;
	public float wallJumpAngle = 0;

	// private floats
	private float currentSpeed;
	private float speed;
	private float direction;
	private float gravity;
	private float distToGround;
	private float onWallRotation;
	private float wallSide;
	private float walkSpeedCounter;
	private float jumpForce;
	private float wallRunForce;

	private float turnSmoothVelocity;
	private float speedSmoothVelocity;

	private float groundCheckDist = 8;
	private float wallJumpDelay = 0;
	// Bools
	private bool grounded = false;
	private bool isNotMoving = true;
		// Jumping
	private bool jumping = false;
	private bool wallJump = false;

	private bool isRunning = false;

	private bool isFalling = false;
	private bool isLanding = false;
		// Wall running
	private bool isWallRunning = false;
	private bool canWallRun = false;
	private bool verticalWallRun = false;
	private bool wallRunReleased = true;
		// Special
	private bool hasTeleported = false;
	private bool teleportVisual = false;
	private bool rbPressed = false;
	private bool groundRoll = false;

	public Vector3 movementSpeeds;

	private Vector3 myForward;
	private Vector3 wallJumpForce;
	// Rays
	private Vector3 rayDir;
	private Vector3 wallNormal;
	private Vector3 rayRight;
	private Vector3 rayLeft;
	private Vector3 rayFront;
	private Vector3 myUp;
	private Vector3 myRight;
	// Inputs
	private Vector3 input;
	private Vector3 inputDir;

	private Transform cameraT;
	private Transform myTransform;
	
	private LayerMask ignoreMask;
	
	public WallJumps wallJumpType;

	RaycastHit hit;
	Vector3 indicatorPos;


	// Use this for initialization
	void Start () {
		myRigidbody = GetComponentInChildren<Rigidbody> ();
		myTransform = GetComponent<Transform> ();
		myAnimator = GetComponentInChildren<Animator> ();

		distToGround = 0;// GetComponent<CapsuleCollider> ().height / 8;
		ignoreMask = 1 << 9;

		cameraT = Camera.main.transform;
		myCam = Camera.main.GetComponent<ThirdPersonCamera> ();
		walkSpeedCounter = walkSpeedTime;

		myForward = myTransform.forward;
		myUp = myTransform.up;
		myRight = myTransform.right;
		CalculateGravity ();
		CalaculateWallRunForce ();
	}

	// Update is called once per frame
	void Update () {
		float DT = Time.deltaTime;
		grounded = IsGrounded ();

		// Update rays
		rayRight = myTransform.right;
		rayLeft = -myTransform.right;
		rayFront = myTransform.forward;

		// Movement input
		input = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical"));
		inputDir = input.normalized;

		float inputLength = inputDir.magnitude;
		float inputLen = input.magnitude;

		isRunning = Input.GetKey (KeyCode.LeftShift);

		// Set target move speed
		float moveSpeed = (inputLen > 0.4f) ? runSpeed : walkSpeed;
		float targetSpeed = ((isRunning) ? sprintSpeed : moveSpeed) * inputLength;

		if (!grounded && !isWallRunning)
		{
			targetSpeed = airMoveSpeed;
		}
		else if (grounded || isWallRunning)
		{
			airMoveSpeed = targetSpeed;
		}

		if (abilityTimer > 0)
		{
			abilityTimer -= DT;
			hasTeleported = false;
			teleportVisual = false;
		}

		if (isRunning)
		{
			walkSpeedCounter = 0;
		}

		currentSpeed = Mathf.SmoothDamp (currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);


		Rotate (DT);

		Inputs ();

		// Forward movement while wall running and jumping
		if (isWallRunning)
		{
			currentSpeed = ((isRunning) ? sprintSpeed : runSpeed);
		}

		if (wallJumpType == WallJumps.wallLeap)
		{
			movementSpeeds.z = jumpForce * 2;
		}
		else
		{
			movementSpeeds.z = currentSpeed;
		}

		if (groundRoll && rollTimer > 0)
		{
			rollTimer -= DT;
			movementSpeeds.z = runSpeed;
			if (rollTimer <= 0)
			{
				rollTimer = 0;
				groundRoll = false;
			}
		}

		if (hasTeleported)
		{
			myTransform.position += myForward * teleportDist;
		}

		GravityCals (DT);

		StickToWorldSpace (this.transform, myCam.transform, ref direction, ref speed);

		SetAnimatorValues ();

		if (isLanding && movementSpeeds.y < -35)
		{
			groundRoll = true;
			rollTimer = 0.8235296f;
		}

		// Debug Rays
		Debug.DrawRay (wallNormal, rayDir * 5, Color.red);
		Debug.DrawRay (myTransform.position, rayRight * 3, Color.blue);
		Debug.DrawRay (myTransform.position, rayLeft * 3, Color.green);
		Debug.DrawRay (myTransform.position, rayFront * 2, Color.black);
	}

	void FixedUpdate()
	{
		if (wallJumpType == WallJumps.vertical)
		{
			movementSpeeds = new Vector3 (movementSpeeds.x, movementSpeeds.y, 0);
		}
		myRigidbody.velocity = myForward * movementSpeeds.z + myUp * movementSpeeds.y + myRight * movementSpeeds.x + wallJumpForce;
	}

	/// <summary>
	/// Determines teleport distance
	/// </summary>
	void DistCheck()
	{
		teleportDist = 100;
		Debug.DrawRay (myTransform.position + Vector3.up * 4, myForward * 100, Color.red);
		Vector3 teleportSpot = myTransform.position + (myForward * teleportDist);
		if (Physics.Raycast (myTransform.position + Vector3.up * 4, myForward, out hit, teleportDist, ignoreMask))
		{
			teleportDist = hit.distance;
			indicatorPos = hit.point;
			Debug.DrawRay (hit.point, hit.normal * teleportDist);
		}
		else
		{
			indicatorPos = teleportSpot + Vector3.up * 4;
		}

		cube.transform.position = indicatorPos;
	}

	/// <summary>
	/// Input controller
	/// </summary>
	void Inputs()
	{
		isLanding = false;
		isFalling = false;

		if (input == Vector3.zero)
		{
			isNotMoving = true;
		}
		else
		{
			isNotMoving = false;
		}

		if (Input.GetAxisRaw ("RightBumper") != 0 && !rbPressed)
		{
			myCam.hasLockOnTarget = !myCam.hasLockOnTarget;
			rbPressed = true;
		}
		else if (Input.GetAxisRaw ("RightBumper") == 0)
		{
			rbPressed = false;
		}

		if (grounded)
		{
			wallJumpForce = Vector3.zero;
			wallJumpAngle = 0;
			wallJumpType = WallJumps.none;
			movementSpeeds.x = 0;
			wallJump = false;
			if (Input.GetAxis ("AutoMove") == 0)
			{
				isWallRunning = false;
				verticalWallRun = false;
				wallRunReleased = true;
			}

			if (Input.GetAxis ("AutoMove") != 0 && !isWallRunning && canWallRun && wallRunReleased && input != Vector3.zero)
			{
				isWallRunning = true;
				movementSpeeds.y = wallRunForce;
			}
			else if (Input.GetAxis ("Jump") != 0)
			{
				jumping = true;
				Invoke ("JumpDelay", 0.2f);
			}
			else if (!jumping)
			{
				movementSpeeds.y = 0;
				isWallRunning = false;
			}
		}
		else if (Input.GetAxis ("AutoMove") != 0 && canWallRun)
		{
			wallJumpAngle = 0;
			isWallRunning = true;
			wallRunReleased = false;
			if (Input.GetAxis ("Jump") != 0 && !wallJump)
			{
				GetTypeOfJump ();
			}
			else if (wallJumpDelay <= 0)
			{
				wallJump = false;
				wallJumpType = WallJumps.none;
			}
		}
		else
		{
			FallingOrLanding ();
		}

		if ((Input.GetAxisRaw ("LTrigger") != 0 || Input.GetKeyDown (KeyCode.E)) && abilityTimer <= 0)
		{
			teleportVisual = true;
			DistCheck ();
		}

		if (teleportVisual && Input.GetAxisRaw ("LTrigger") == 0)
		{
			hasTeleported = true;
			abilityTimer = abilityCoolDown;
		}
	}

	/// <summary>
	/// Sets the animator values.
	/// </summary>
	void SetAnimatorValues()
	{
		myAnimator.SetFloat ("Speed", currentSpeed);
		myAnimator.SetFloat ("WallRunSide", wallSide);
		myAnimator.SetFloat ("FallSpeed", movementSpeeds.y);
		myAnimator.SetBool ("Falling", isFalling);
		myAnimator.SetBool ("Landing", isLanding);
		myAnimator.SetBool ("WallRunning", isWallRunning);
		myAnimator.SetBool ("Grounded", grounded);

		if (jumping || wallJump)
		{
			myAnimator.SetBool ("Jump", true);
		}
		else
		{
			myAnimator.SetBool ("Jump", false);
		}
	}

	/// <summary>
	/// Determine the type of jump.
	/// </summary>
	void GetTypeOfJump()
	{
		// Determine against camera angle
		wallJump = true;
		if (input.z < -0.5)
		{
			wallJumpType = WallJumps.wallLeap;
		}
		else if (Mathf.Abs (input.x) > 0.5f)
		{
			wallJumpType = WallJumps.wallSwap;
		}
		else
		{
			wallJumpType = WallJumps.sideWall;
		}
			
		Invoke ("WallJump", 0.2f);
		wallJumpDelay = 0.25f;
	}

	/// <summary>
	/// Controls rotataion for wallrunning.
	/// </summary>
	/// <param name="DT">D.</param>
	void Rotate(float DT)
	{
		// Set facing direction
		if (wallJump)
		{
			wallJumpDelay -= DT;
			// If wanting air control when jumping
			float targetRotation = Mathf.Atan2 (inputDir.x, inputDir.z) * Mathf.Rad2Deg + onWallRotation + cameraT.eulerAngles.y;
			float turnSmoothTime = jumpTurnSmoothTime;

			if (wallJumpType == WallJumps.sideWall || wallJumpType == WallJumps.wallLeap)
			{
				myTransform.eulerAngles = Vector3.up * onWallRotation;
				movementSpeeds.x = 0;
			}
			else if (wallJumpType == WallJumps.vertical)
			{
				myTransform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle (myTransform.eulerAngles.y, onWallRotation, ref turnSmoothVelocity, groundTurnSmoothTime);
			}
		}
		else if (isWallRunning)
		{
			GetSide (wallJumpAngle);
			myTransform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle (myTransform.eulerAngles.y, onWallRotation, ref turnSmoothVelocity, groundTurnSmoothTime);
		}
		else if (inputDir != Vector3.zero)
		{
			float joyDir = Mathf.Atan2 (inputDir.x, inputDir.z) * Mathf.Rad2Deg;
			float targetRotation = joyDir + cameraT.eulerAngles.y;
			float turnSmoothTime = groundTurnSmoothTime;

			if (!grounded)
			{
				turnSmoothTime = airTurnSmoothTime; 
			}
			myTransform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle (myTransform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);

			if (walkSpeedCounter > 0)
			{
				walkSpeedCounter -= DT;
			}
		}
		else
		{
			walkSpeedCounter = walkSpeedTime;
		}
	}

	/// <summary>
	/// Changes effect of gravity depending on current action
	/// </summary>
	/// <param name="DT">Delta time</param>
	void GravityCals(float DT)
	{
		movementSpeeds.y += gravity * DT;

		myForward = myTransform.forward;
		myUp = myTransform.up;
		myRight = myTransform.right;

		if (isWallRunning || verticalWallRun)
		{
			if (verticalWallRun)
			{
				myForward = myTransform.up;
				myUp = -myTransform.forward;
				movementSpeeds.y = 0;
			}
			else if (movementSpeeds.y < 0)
			{
				movementSpeeds.y /= wallRunSlowSpeed;
			}
		}

		if (movementSpeeds.y < -100)
		{
			movementSpeeds.y = -100;
		}
		if (wallJumpType == WallJumps.vertical)
		{
			wallJumpForce.y = movementSpeeds.y;
			myForward = rayDir;
		}
		if (movementSpeeds.y < -40 && airMoveSpeed > 0)
		{
			airMoveSpeed -= DT * 6;
			if(airMoveSpeed < 0)
			{
				airMoveSpeed = 0;
			}
		}
	}

	/// <summary>
	/// Calculates gravity.
	/// </summary>
	void CalculateGravity()
	{
		gravity = -(2 * jumpHeight / Mathf.Pow (timeToJumpApex, 2));
		jumpForce = Mathf.Abs (gravity) * timeToJumpApex;
	}


	/// <summary>
	/// Determines whether this instance is grounded.
	/// </summary>
	/// <returns><c>true</c> if this instance is grounded; otherwise, <c>false</c>.</returns>
	bool IsGrounded()
	{
		return Physics.Raycast (myTransform.position + Vector3.up * 0.1f, -Vector3.up, 0.2f);
	}

	/// <summary>
	/// Check if instance is falling or landing.
	/// </summary>
	void FallingOrLanding ()
	{
		isWallRunning = false;
		jumping = false;
		verticalWallRun = false;

		if (currentSpeed > 0.1f)
		{
			groundCheckDist = 5.2f;
		}
		else
		{
			groundCheckDist = 8;
		}

		if (Physics.Raycast (myTransform.position + Vector3.up * 0.1f, -Vector3.up, groundCheckDist))
		{
			isLanding = true;
		}
		else if (movementSpeeds.y < 0)
		{
			isFalling = true;
		}
	}

	/// <summary>
	/// Applies jump force to instance.
	/// </summary>
	void JumpDelay()
	{
		movementSpeeds.y = jumpForce;
	}

	/// <summary>
	/// Applies correct jumping actions base on jump type
	/// </summary>
	void WallJump()
	{
		isWallRunning = false;
		if (wallSide == 0)
		{
			wallJumpAngle = 180;
			wallJumpForce = rayDir * jumpForce;
			movementSpeeds.y = jumpForce;
			wallJumpType = WallJumps.vertical;
			onWallRotation = myTransform.eulerAngles.y - 180;
		}
		else
		{
			if (wallJumpType == WallJumps.wallLeap)
			{
				wallJumpAngle = 90;
				movementSpeeds.y = jumpForce / 1.5f;
				movementSpeeds.z = jumpForce * 2;
			}
			else if (wallJumpType == WallJumps.wallSwap)
			{
				movementSpeeds.y = jumpForce / 2.0f;
				movementSpeeds.z = runSpeed;
				movementSpeeds.x = (jumpForce * wallSide) * 2;
			}
			else if (wallJumpType == WallJumps.sideWall)
			{
				wallJumpAngle = 25;
				movementSpeeds.y = jumpForce;
				movementSpeeds.z = jumpForce * 1.5f;
				movementSpeeds.x = jumpForce * wallSide;
			}
			GetSide (wallJumpAngle);
		}
	}

	/// <summary>
	/// Calaculates the wall run force.
	/// </summary>
	void CalaculateWallRunForce()
	{
		wallRunForce = Mathf.Sqrt (-2 * gravity * wallRunMaxHeight);
	}

	void OnCollisionEnter(Collision other)
	{
		movementSpeeds.x = 0;
		if (other.gameObject.tag == "WallRun")
		{
			canWallRun = true;
		}
	}

	void OnCollisionExit (Collision other)
	{
		if (other.gameObject.tag == "WallRun")
		{
			canWallRun = false;
		}
	}

	/// <summary>
	/// Gets direction of wall to rotate to.
	/// </summary>
	/// <param name="rotateAmount">Rotate amount.</param>
	void GetSide(float rotateAmount)
	{
		RaycastHit hitRight;
		RaycastHit hitLeft;
		RaycastHit hitFront;

		Physics.Raycast (myTransform.position, rayLeft, out hitRight, 3.0f);
		Physics.Raycast (myTransform.position, rayRight, out hitLeft, 3.0f);
		Physics.Raycast (myTransform.position, rayFront, out hitFront, 2.0f);

		if (hitFront.normal != Vector3.zero && hitFront.transform.tag == "WallRun")
		{
			RotateAlongWall (hitFront, rotateAmount);
		}
		else if (hitRight.normal != Vector3.zero && wallJumpType != WallJumps.vertical)
		{
			RotateAlongWall (hitRight, rotateAmount);
		}
		else if (hitLeft.normal != Vector3.zero && wallJumpType != WallJumps.vertical)
		{
			RotateAlongWall (hitLeft, rotateAmount);
		}
	}

	/// <summary>
	/// Rotates gameobject to correct orientation
	/// </summary>
	/// <param name="wallDirNorm">Wall direction normal.</param>
	/// <param name="rotateAmount">Rotate amount.</param>
	void RotateAlongWall(RaycastHit wallDirNorm, float rotateAmount)
	{
		rayDir = wallDirNorm.normal;

		wallNormal = Vector3.Cross (myTransform.forward, rayDir);

		float angle = Vector3.Angle (rayDir, myTransform.forward);

		float yRot = angle - 90;
		isWallRunning = true;
		if (angle > 145 || wallJumpType == WallJumps.vertical)
		{
			wallSide = 0;
			verticalWallRun = true;
			onWallRotation = Quaternion.LookRotation (-wallDirNorm.normal).eulerAngles.y + rotateAmount;
		}
		else
		{
			wallSide = 1;
			if (wallNormal.y < 0)
			{
				yRot *= -1;
				wallSide = -1;
			}
			onWallRotation = myTransform.localEulerAngles.y + yRot + (rotateAmount * wallSide);
		}

		wallNormal = wallDirNorm.point;
	}

	/// <summary>
	/// Converts stick to world space.
	/// </summary>
	/// <param name="root">Root.</param>
	/// <param name="camera">Camera.</param>
	/// <param name="directionOut">Direction out.</param>
	/// <param name="speedOut">Speed out.</param>
	public void StickToWorldSpace(Transform root, Transform camera, ref float directionOut, ref float speedOut)
	{
		Vector3 rootDirection = root.forward;
		Vector3 stickDirection = new Vector3 (input.x, 0, input.z);

		speedOut = stickDirection.sqrMagnitude;

		Vector3 CameraDirection = camera.forward;
		CameraDirection.y = 0;
		Quaternion referentialShift = Quaternion.FromToRotation (Vector3.forward, CameraDirection);

		Vector3 moveDirection = referentialShift * stickDirection;
		Vector3 axisSign = Vector3.Cross (moveDirection, rootDirection);

		float angleRootToMove = Vector3.Angle (rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);

		angleRootToMove /= 180f;
		directionOut = angleRootToMove * directionSpeed;
	}
}
