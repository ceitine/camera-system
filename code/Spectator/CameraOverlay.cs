namespace TestGame;

public class CameraOverlay : RenderHook
{
	/// <summary>
	/// The camera this RenderHook is tied to.
	/// </summary>
	public CCTV Camera { get; set; }

	public override void OnStage( SceneCamera target, Stage stage )
	{
		if ( stage != Stage.AfterUI )
			return;

		var mat = Material.FromShader( "shaders/camera.shader" );
		var attributes = new RenderAttributes();

		if ( Game.LocalPawn is not Spectator pawn )
			return;

		// Grab frame texture.
		Graphics.GrabFrameTexture( "ColorTexture", attributes );

		// Overlay shader on top of current frame.
		Graphics.Blit( mat, attributes );
	}
}
