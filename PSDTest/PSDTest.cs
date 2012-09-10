using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PSDTest
{
	public class PsdTest : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private Texture2D _texture;

		public PsdTest()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);
			_texture = Content.Load<Texture2D>("TestPSD");
		}

		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		
		protected override void Update(GameTime gameTime)
		{
		
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
				Exit();

			// TODO: Add your update logic here

			base.Update(gameTime);
		}

		
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			_spriteBatch.Begin();

			_spriteBatch.Draw(_texture, Vector2.Zero, Color.White);

			_spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}
