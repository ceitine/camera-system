global using Sandbox;
global using Sandbox.UI.Construct;
global using Sandbox.Component;
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;

namespace TestGame;

public partial class TestGame : GameManager
{
	/// <summary>
	/// Distance that the voice fades out at.
	/// </summary>
	public const float VOICE_DISTANCE = 2500f;

	/// <summary>
	/// Static reference to current GameManager instance.
	/// </summary>
	public static TestGame Instance { get; private set; }

	/// <summary>
	/// List of all the handlers.
	/// </summary>
	public static IReadOnlyList<Spectator> Spectators => (List<Spectator>)Instance.spectators;
	[Net] private IList<Spectator> spectators { get; set; } = new List<Spectator>();

	public TestGame()
	{
		Instance = this;
		
		if ( Game.IsClient )
			return;

		new CCTV()
		{
			Position = new Vector3( -1689.03f, -1129.78f, 293.78f ),
			ViewRotation = Rotation.From( 22.51f, 136.11f, 0.00f ),
			Title = "Lobby 1",
		};

		new CCTV()
		{
			Position = new Vector3( -1683.66f, -246.05f, 285.27f ),
			ViewRotation = Rotation.From( 23.41f, -141.45f, 0.00f ),
			Title = "Lobby 2",
		};

		new CCTV()
		{
			Position = new Vector3( -1958.45f, 225.97f, 308.50f ),
			ViewRotation = Rotation.From( 27.00f, -146.54f, 0.00f ),
			Title = "Piano room",
		};

		new CCTV()
		{
			Position = new Vector3( -2806.97f, -795.05f, 284.56f ),
			ViewRotation = Rotation.From( 31.55f, -49.49f, -0.00f ),
			Title = "Staircase",
		};

		new CCTV()
		{
			Position = new Vector3( -3439.48f, -324.18f, 318.61f ),
			ViewRotation = Rotation.From( 25.10f, -1.20f, 0.00f ),
			Title = "Dining room",
		};
	}

	public override bool CanHearPlayerVoice( IClient source, IClient receiver )
	{
		var from = source.Pawn;
		var to = receiver.Pawn;
		if ( from == null || !from.IsValid || to == null || !to.IsValid )
			return false;

		return from.Position.Distance( to.Position ) < VOICE_DISTANCE;
	}

	public override void OnVoicePlayed( IClient cl )
	{
		cl.Voice.WantsStereo = true;

		var speaker = cl.Pawn;
		if ( speaker == null || !speaker.IsValid )
			return;
		
		DebugOverlay.Text( "Speaking", speaker.Position );
	}

	public override void ClientJoined( IClient cl )
	{
		base.ClientJoined( cl );

		if ( Entity.All.OfType<Spectator>().Count() <= 0 )
		{
			var spectator = new Spectator();
			cl.TagList = "host";
			cl.Pawn = new Spectator();

			return;
		}

		// Create a pawn for this client to play with
		var pawn = new Player();
		cl.Pawn = pawn;

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}
	}

	public override void RenderHud()
	{
		base.RenderHud();
		Event.Run( "render" );
	}
}
