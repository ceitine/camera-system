namespace TestGame;

public class SceneCapture
{
	private static List<SceneCapture> scenes = new();

	/// <summary>
	/// The RenderTarget texture of the capture.
	/// </summary>
	public Texture RenderTarget { get; set; }

	/// <summary>
	/// The target framerate of updating the texture.
	/// </summary>
	public int TargetFramerate { get; set; }

	/// <summary>
	/// The camera used for capturing the scene.
	/// </summary>
	public SceneCamera Camera { get; set; }

	/// <summary>
	/// Is the SceneCapture paused or not?
	/// </summary>
	public bool Paused => paused;

	protected SceneCapture() { }

	private bool captureOnce;
	private bool shouldStop = false;
	private bool paused = false;
	private TimeSince lastCaptured;

	/// <summary>
	/// Begin capturing a scene.
	/// </summary>
	/// <param name="camera"></param>
	/// <param name="framerate"></param>
	/// <param name="size"></param>
	/// <param name="captureOnce"></param>
	/// <returns></returns>
	public static SceneCapture Begin( SceneCamera camera, int framerate = 24, Vector2? size = null, bool captureOnce = false )
	{
		var sceneCapture = new SceneCapture()
		{
			Camera = camera,
			TargetFramerate = framerate,
			captureOnce = captureOnce,
			lastCaptured = framerate
		};

		sceneCapture.RenderTarget = Texture.CreateRenderTarget()
			.WithSize( size ?? 256 )
			.WithFormat( ImageFormat.Default )
			.WithScreenFormat()
			.WithScreenMultiSample()
			.Create();

		scenes.Add( sceneCapture );

		return sceneCapture;
	}

	/// <summary>
	/// Stop the rendering of this SceneCapture.
	/// </summary>
	public void Stop()
	{
		shouldStop = true;
	}

	/// <summary>
	/// Toggles the pause of this SceneCapture.
	/// </summary>
	public void Toggle()
	{
		paused = !paused;
	}

	[Event( "render" )]
	private static void RenderScenes()
	{
		for ( int i = 0; i < scenes.Count; i++ )
		{
			var scene = scenes[i];
			if ( scene == null )
				continue;

			if ( scene.shouldStop )
			{
				scenes.Remove( scene );
				continue;
			}

			if ( scene.lastCaptured < 1f / scene.TargetFramerate || scene.paused )
				continue;

			Graphics.RenderToTexture( scene.Camera, scene.RenderTarget );
			scene.lastCaptured = 0f;

			if ( scene.captureOnce )
				scenes.Remove( scene );
		}
	}
}
