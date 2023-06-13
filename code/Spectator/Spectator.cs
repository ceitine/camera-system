namespace TestGame;

public partial class Spectator : Entity
{
	/// <summary>
	/// To all of the Spectators.
	/// </summary>
	public static To To => To.Multiple( Game.Clients.Where( cl => cl.Pawn is Spectator ) );

	/// <summary>
	/// The Spectator's mouse ray from the view of current Camera.
	/// </summary>
	[ClientInput] public Ray MouseRay { get; set; }

	/// <summary>
	/// The current Camera that the Spectator is watching.
	/// </summary>
	public CCTV CCTV
	{
		get => cctv;
		set
		{
			if ( !cctv?.Capturer?.Paused ?? false )
				cctv.Capturer.Toggle();

			cctv = value;
			if ( cctv?.Capturer?.Paused ?? false )
				cctv.Capturer.Toggle();
		}
	}
	[Net, Local] private CCTV cctv { get; set; }

	public override void BuildInput()
	{
		if ( CCTV == null )
			return;

		MouseRay = SpectatorView.Ray;
	}

	public override void ClientSpawn()
	{
		if ( Game.LocalClient != Client )
			return;

		_ = new SpectatorView();
		Camera.Main.FindOrCreateHook<CameraOverlay>();
	}

	public override void Simulate( IClient cl )
	{
		// Set Camera to first one if it hasn't already been set.
		if ( CCTV == null )
		{
			CCTV = CCTV.All.FirstOrDefault();
			return;
		}

		// Unpause current Camera.
		if ( CCTV.Capturer != null && CCTV.Capturer.Paused )
			CCTV?.Capturer.Toggle();

		// Move to the next Camera.
		var direction = Input.Pressed( "use" )
			? 1
			: Input.Pressed( "menu" )
				? -1
				: 0;

		if ( direction != 0 )
		{
			var index = CCTV.IndexOf( CCTV );
			var next = index + direction >= CCTV.All.Count
				? CCTV.All.FirstOrDefault()
				: index + direction < 0  
					? CCTV.All.LastOrDefault()
					: CCTV.All[index + direction];

			CCTV = next;
		}
		
		// Toggle Camera nightmode.
		if ( Input.Pressed( "score" ) )
			CCTV.Nightmode = !CCTV.Nightmode;
	}

	public override void FrameSimulate( IClient cl )
	{
		// Position Camera based on current CCTV.
		if ( CCTV == null )
			return;

		Camera.Position = CCTV.Position;
		Camera.Rotation = CCTV.Rotation;

		Camera.FieldOfView = 0f;

		Camera.FirstPersonViewer = CCTV;
	}

	/// <summary>
	/// Swap the Camera to any of the existing ones.
	/// </summary>
	/// <param name="ident"></param>
	[ConCmd.Server]
	public static void SwapCamera( int ident )
	{
		if ( ConsoleSystem.Caller.Pawn is not Spectator spectator )
			return;

		var cctv = Entity.All
			.OfType<CCTV>()
			.FirstOrDefault( e => e.NetworkIdent == ident );

		if ( cctv == null )
			return;

		spectator.CCTV = cctv;
	}

	/// <summary>
	/// Send sound request.
	/// </summary>
	/// <param name="ident"></param>
	/// <param name="type"></param>
	/// <param name="input"></param>
	[ConCmd.Server]
	public static void SoundRequest( int ident, CCTV.SoundType type, string input )
	{
		var cl = ConsoleSystem.Caller;
		if ( cl.Pawn is not Spectator spectator )
			return;

		if ( input == string.Empty )
			return;

		var cctv = ident == 0 
			? spectator.CCTV
			: Entity.All
				.OfType<CCTV>()
				.FirstOrDefault( e => e.NetworkIdent == ident );

		if ( cctv == null )
			return;

		cctv.QueueSound( type, input );

		if ( type == CCTV.SoundType.TTS )
			Chat.Send( Spectator.To, Chat.MessageType.TTS, $"{cl.SteamId}", $"{cl.Name}", $"{cctv.Title}", $"{input}" );
	}
	/*[Event( "render" )]
	private void RenderCameras()
	{
		if ( CCTV == null || Game.LocalPawn != this || (Game.LocalClient.Components.Get<DevCamera>( true )?.Enabled ?? false) )
			return;

		var size = CCTV.Capturer.RenderTarget.Size;
		var rect = new Rect( Screen.Size / 2f - size / 2f, size );
		var mat = Material.UI.Basic;

		var attributes = new RenderAttributes();
		attributes.Set( "Texture", CCTV.Capturer.RenderTarget );

		Graphics.DrawQuad( rect, mat, Color.White, attributes );
	}*/
}
