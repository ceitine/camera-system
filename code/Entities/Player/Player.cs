namespace TestGame;

public partial class Player : AnimatedEntity
{
	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();

		//
		// Use a watermelon model
		//
		SetModel( "models/sbox_props/watermelon/watermelon.vmdl_c" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -8, -8, 0 ), new Vector3( 8, 8, 16 ) );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	// An example BuildInput method within a player's Pawn class.
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		var look = Input.AnalogLook;

		var viewAngles = ViewAngles;
		viewAngles += look;
		ViewAngles = viewAngles.Normal;
	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		Rotation = Rotation.FromYaw( ViewAngles.yaw );

		// build movement from the input values
		var movement = InputDirection.Normal;

		// rotate it to the direction we're facing
		Velocity = ViewAngles.ToRotation() * movement;

		// apply some speed to it
		Velocity *= Input.Down( "run" ) ? 1000 : 200;

		// apply it to our position using MoveHelper, which handles collision
		// detection and sliding across surfaces for us
		var helper = new MoveHelper( Position, Velocity );
		helper.Trace = helper.Trace
			.Size( 16 )
			.IncludeClientside()
			.Ignore( this );

		if ( helper.TryMove( Time.Delta ) > 0 )
			Position = helper.Position;
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		Rotation = Rotation.FromYaw( ViewAngles.yaw );

		Camera.Position = Position;
		Camera.Rotation = ViewAngles.WithPitch( ViewAngles.pitch.Clamp( -89, 89 ) ).ToRotation();

		// Set field of view to whatever the user chose in options
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 70f );

		// Set the first person viewer to this, so it won't render our model
		Camera.FirstPersonViewer = this;
	}
}
