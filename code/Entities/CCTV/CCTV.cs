namespace TestGame;

public partial class CCTV : ModelEntity
{
	/// <summary>
	/// Enum for types of sound that you can play from the Camera.
	/// </summary>
	public enum SoundType
	{
		TTS,
		Effect
	}

	/// <summary>
	/// List of all the CCTV Cameras.
	/// </summary>
	public new static IReadOnlyList<CCTV> All => all;
	private static List<CCTV> all { get; set; } = new();

	/// <summary>
	/// The title that this Camera is displayed with.
	/// </summary>
	[Net] public string Title { get; set; }

	/// <summary>
	/// The Camera's view rotation.
	/// </summary>
	[Net] public Rotation ViewRotation { get; set; }

	/// <summary>
	/// Determines if the Camera is using nightvision or not.
	/// </summary>
	[Net] public bool Nightmode { get; set; } = false;

	/// <summary>
	/// The SceneCapture used for this Camera's rendering.
	/// </summary>
	public SceneCapture Capturer { get; private set; }

	/// <summary>
	/// Index of the camera.
	/// </summary>
	/// <param name="cctv"></param>
	/// <returns></returns>
	public static int IndexOf( CCTV cctv )
		=> all.IndexOf( cctv );

	private Sound? sound;
	private SoundStream stream;
	private List<(SoundType type, string input)> queue = new();
	private TimeUntil? ttsEnd;

	public override void Spawn()
	{
		SetModel( "models/sbox_props/cctv_globe/cctv_globe.vmdl" );

		Rotation = Rotation.From( 180, 0, 0 );
		Transmit = TransmitType.Always;

		all.Add( this );
	}

	public override void ClientSpawn()
	{
		all.Add( this );
	}

	public override void OnNewModel( Model model )
	{
		if ( Game.IsServer )
			return;

		// Stop old capturer.
		Capturer?.Stop();
		Capturer = null;

		// Initialize new SceneCamera.
		var camera = new SceneCamera()
		{
			World = Game.SceneWorld,
			Position = Position,
			Rotation = ViewRotation,
			FieldOfView = 70f,
			ZFar = 7500f
		};

		// Add all automatic renderhooks to our SceneCamera.
		var renderHooks = TypeLibrary.GetTypesWithAttribute<SceneCamera.AutomaticRenderHookAttribute>();
		foreach ( var tuple in renderHooks )
		{
			var hook = tuple.Type.Create<RenderHook>();
			if ( hook is null )
				continue;

			camera.AddHook( hook );
		}

		// Begin Capturing frames.
		Capturer = SceneCapture.Begin( camera, 24, new Vector2( 1280, 720 ) );
		if ( !Capturer.Paused )
			Capturer.Toggle();

		// Avoid drawing self.
		SceneObject.Tags.Add( NetworkIdent.ToString() );
		camera.ExcludeTags.Add( NetworkIdent.ToString() );
	}

	protected override void OnDestroy()
	{
		Capturer?.Stop();
		Capturer = null;
	}

	/// <summary>
	/// Plays a specific type of sound from the Camera, only called from server.
	/// </summary>
	public void QueueSound( SoundType type, string input )
		=> queue.Add( (type, input) );

	[ClientRpc]
	private async void PlayTTS( short[] samples )
	{
		if ( Game.IsServer )
			return;

		Sound.FromEntity( "tts_begin", this )
			.SetVolume( 0.5f );

		await GameTask.Delay( 1250 );

		stream?.Delete();

		stream = new SoundStream( SAMLibrary.SAMPLE_RATE );
		stream.WriteData( samples );
		stream.Play( this );
	}

	[GameEvent.Tick]
	private void Tick()
	{
		// Handle Camera sound effects and TTS.
		if ( Game.IsServer )
		{
			// Current item in queue.
			var current = queue.FirstOrDefault();
			if ( current == default )
				return;

			// Move to next in queue.
			if ( (ttsEnd == null && (sound == null || !sound.Value.IsPlaying)) || (ttsEnd ?? false) )
			{
				var type = current.type;
				var input = current.input;

				ttsEnd = null;

				// Switch through the sound types.
				switch ( type )
				{
					case SoundType.TTS:
						var samples = SAMLibrary.Generate( input );
						if ( samples == null )
							break;

						ttsEnd = samples.Length / 22050 + 2.5f;
						PlayTTS( To.Everyone, samples );

						break;


					case SoundType.Effect:
						sound = Sound.FromEntity( input, this )
							.SetVolume( 0.5f );

						break;
				}

				// Remove item from queue.
				queue.Remove( current );
			}

			return;
		}

		// Do nightmode coloring.
		Capturer.Camera.AmbientLightColor = Nightmode ? new Color( 0.1f, 0.1f, 0.1f ) : Color.Black;
		Capturer.Camera.EnablePostProcessing = !Nightmode;
	}
}
