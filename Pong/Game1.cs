using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pong
{
    public class Game1 : Game
    {
        Texture2D ballTexture;
        Vector2 ballPosition;
        float ballSpeed;
        Vector2 ballSize;
        Vector2 ballVelocity;
        Random rnd = new Random();
        float ballSpeedIncrease = 1.05f;

        Texture2D PongTexture;
        Vector2 PongPosition;
        Vector2 PongSize;
        float PongRotation;

        Texture2D Pong2Texture;
        Vector2 Pong2Position;
        Vector2 Pong2Size;
        float Pong2Rotation;

        int scorePlayer1 = 0;
        int scorePlayer2 = 0;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        SpriteFont scoreFont;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            ballPosition = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            ballSpeed = 300f; // Geschwindigkeit in Pixel/Sekunde
            ballSize = new Vector2(30, 30);

            // initiale Ballrichtung zufällig
            ResetBall(rnd.Next(0, 2) == 0);

            // Paddle-Größen so setzen, dass sie hochkant (vertikal) sind:
            // Breite klein, Höhe groß
            PongSize = new Vector2(35, 150);   // linkes Paddle (Breite=35, Höhe=150)
            Pong2Size = new Vector2(35, 150);  // rechtes Paddle

            // Positionen: x nah am Rand, y in der Bildschirmmitte
            PongPosition = new Vector2(50, _graphics.PreferredBackBufferHeight / 2f); // links
            Pong2Position = new Vector2(_graphics.PreferredBackBufferWidth - 50, _graphics.PreferredBackBufferHeight / 2f); // rechts

            PongRotation = 0f;
            Pong2Rotation = 0f;
            base.Initialize();
        }

        void ResetBall(bool toRight)
            
        {
            ballSpeed = 300f; // Reset der Ballgeschwindigkeit
            ballPosition = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);

            // Winkel zwischen -25 und 25 Grad
            float angleDeg = (float)(rnd.NextDouble() * 50.0 - 25.0);
            if (!toRight) angleDeg += 180f;
            float angleRad = MathHelper.ToRadians(angleDeg);

            ballVelocity = new Vector2((float)Math.Cos(angleRad), (float)Math.Sin(angleRad));
            ballVelocity.Normalize();
            ballVelocity *= ballSpeed;
        }

        // --- REPLACE existing CircleIntersectsAABB with this improved version ---
        // Returns true if circle intersects AABB; also returns closest point, contact normal and penetration depth.
        bool CircleAABBOverlap(Vector2 circleCenter, float radius, Vector2 rectCenter, Vector2 rectSize,
                               out Vector2 closest, out Vector2 normal, out float penetration)
        {
            float left = rectCenter.X - rectSize.X / 2f;
            float top = rectCenter.Y - rectSize.Y / 2f;
            float right = left + rectSize.X;
            float bottom = top + rectSize.Y;

            float cx = MathHelper.Clamp(circleCenter.X, left, right);
            float cy = MathHelper.Clamp(circleCenter.Y, top, bottom);
            closest = new Vector2(cx, cy);

            Vector2 diff = circleCenter - closest;
            float distSq = diff.LengthSquared();

            // No intersection
            if (distSq > radius * radius)
            {
                normal = Vector2.Zero;
                penetration = 0f;
                return false;
            }

            float dist = (float)Math.Sqrt(distSq);
            // If center is exactly on closest (circle center inside rect), choose a fallback normal
            if (dist > 1e-6f)
                normal = diff / dist; // outward normal from rect to circle center
            else
            {
                // Circle center is inside the rectangle -- pick the shortest escape direction
                // compute distances to each side and choose smallest
                float dl = Math.Abs(circleCenter.X - left);
                float dr = Math.Abs(right - circleCenter.X);
                float dt = Math.Abs(circleCenter.Y - top);
                float db = Math.Abs(bottom - circleCenter.Y);

                float min = Math.Min(Math.Min(dl, dr), Math.Min(dt, db));
                if (min == dl) normal = new Vector2(-1f, 0f);
                else if (min == dr) normal = new Vector2(1f, 0f);
                else if (min == dt) normal = new Vector2(0f, -1f);
                else normal = new Vector2(0f, 1f);
                dist = 0f;
            }

            penetration = radius - dist;
            return true;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            ballTexture = Content.Load<Texture2D>("ball");
            PongTexture = Content.Load<Texture2D>("pngegg");
            Pong2Texture = Content.Load<Texture2D>("pngegg");
            scoreFont = Content.Load<SpriteFont>("Arial");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Ballbewegung
            ballPosition += ballVelocity * dt;

            // Ball als Kreis
            float ballRadius = ballSize.X / 2f;
            var ballRect = new Rectangle(
                (int)(ballPosition.X - ballRadius),
                (int)(ballPosition.Y - ballRadius),
                (int)ballSize.X,
                (int)ballSize.Y);

            int screenW = _graphics.PreferredBackBufferWidth;
            int screenH = _graphics.PreferredBackBufferHeight;

            // Wandkollision oben/unten
            if (ballRect.Top <= 0 && ballVelocity.Y < 0)
            {
                ballVelocity.Y = -ballVelocity.Y;
                ballPosition.Y = ballRadius + 1;
            }
            else if (ballRect.Bottom >= screenH && ballVelocity.Y > 0)
            {
                ballVelocity.Y = -ballVelocity.Y;
                ballPosition.Y = screenH - ballRadius - 1;
            }

            // Skala der Paddles (wie beim Zeichnen)
            float scale1X = PongSize.X / PongTexture.Width;
            float scale1Y = PongSize.Y / PongTexture.Height;
            float scale2X = Pong2Size.X / Pong2Texture.Width;
            float scale2Y = Pong2Size.Y / Pong2Texture.Height;

            Vector2 paddle1SizePixels = new Vector2(PongTexture.Width * scale1X, PongTexture.Height * scale1Y);
            Vector2 paddle2SizePixels = new Vector2(Pong2Texture.Width * scale2X, Pong2Texture.Height * scale2Y);

            // Linkes Paddle - Kreis-vs-AABB (verbessert)
            if (CircleAABBOverlap(ballPosition, ballRadius, PongPosition, paddle1SizePixels,
                                  out Vector2 closest1, out Vector2 normal1, out float penetration1))
            {
                // Nur reagieren, wenn Ball tatsächlich in die Kontaktfläche hinein bewegt (vermeidet Doppel-Handling)
                if (Vector2.Dot(ballVelocity, normal1) < 0f)
                {
                    // Wenn Kontaktnormal größtenteils horizontal ist => Seiten-Treffer (klassisches Pong-Verhalten)
                    if (Math.Abs(normal1.X) > Math.Abs(normal1.Y) * 0.75f)
                    {
                        // Treffpunkt (-1..1) entlang Paddle
                        float hit = (ballPosition.Y - PongPosition.Y) / (paddle1SizePixels.Y / 2f);
                        hit = MathHelper.Clamp(hit, -1f, 1f);

                        Vector2 dir = new Vector2(1f, hit * 0.9f);
                        dir.Normalize();
                        ballVelocity = dir * ballSpeed;

                        // Ballgeschwindigkeit erhöhen
                        ballSpeed *= ballSpeedIncrease;

                    }
                    else
                    {
                        // Corner / Top/Bottom -> Reflexion an Kontaktnormal
                        ballVelocity = Vector2.Reflect(ballVelocity, normal1);
                        if (ballVelocity != Vector2.Zero)
                        {
                            ballVelocity.Normalize();
                            ballVelocity *= ballSpeed;
                        }
                    }

                    // Ball aus dem Paddle heraus schieben, damit er nicht "klebt"
                    ballPosition += normal1 * (penetration1 + 0.5f);
                }
            }

            // Rechtes Paddle - Kreis-vs-AABB (verbessert)
            if (CircleAABBOverlap(ballPosition, ballRadius, Pong2Position, paddle2SizePixels,
                                  out Vector2 closest2, out Vector2 normal2, out float penetration2))
            {
                if (Vector2.Dot(ballVelocity, normal2) < 0f)
                {
                    if (Math.Abs(normal2.X) > Math.Abs(normal2.Y) * 0.75f)
                    {
                        float hit = (ballPosition.Y - Pong2Position.Y) / (paddle2SizePixels.Y / 2f);
                        hit = MathHelper.Clamp(hit, -1f, 1f);

                        Vector2 dir = new Vector2(-1f, hit * 0.9f);
                        dir.Normalize();
                        ballVelocity = dir * ballSpeed;

                        // Ballgeschwindigkeit erhöhen
                        ballSpeed *= ballSpeedIncrease;
                    }
                    else
                    {
                        ballVelocity = Vector2.Reflect(ballVelocity, normal2);
                        if (ballVelocity != Vector2.Zero)
                        {
                            ballVelocity.Normalize();
                            ballVelocity *= ballSpeed;
                        }
                    }

                    ballPosition += normal2 * (penetration2 + 0.5f);
                }
            }

            // Recompute ballRect nach möglichen Positionskorrekturen (wichtig für Goal-Check)
            ballRect = new Rectangle(
                (int)(ballPosition.X - ballRadius),
                (int)(ballPosition.Y - ballRadius),
                (int)ballSize.X,
                (int)ballSize.Y);

            // Goal links oder rechts -> ResetBall
            if (ballRect.Left <= 0)
            {
                scorePlayer1 += 1;
                ResetBall(true);
            }
            else if (ballRect.Right >= screenW)
            {
                scorePlayer2 += 1;
                ResetBall(false);
            }

            // Maussteuerung für die Paddles mit gedrückter linker Maustaste
            var mouse = Mouse.GetState();
            int halfWidth = _graphics.PreferredBackBufferWidth / 2;
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (mouse.X < halfWidth)
                {
                    PongPosition.Y = mouse.Y;
                }
                else
                {
                    Pong2Position.Y = mouse.Y;
                }
            }
            PongPosition.Y = MathHelper.Clamp(PongPosition.Y, PongSize.Y / 2f, _graphics.PreferredBackBufferHeight - PongSize.Y / 2f);
            Pong2Position.Y = MathHelper.Clamp(Pong2Position.Y, Pong2Size.Y / 2f, _graphics.PreferredBackBufferHeight - Pong2Size.Y / 2f);

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here#
            _spriteBatch.Begin();
            var scale = new Vector2(ballSize.X / ballTexture.Width, ballSize.Y / ballTexture.Height);
            _spriteBatch.Draw(ballTexture, ballPosition, null, Color.White, 0f,
                new Vector2(ballTexture.Width / 2, ballTexture.Height / 2), scale, SpriteEffects.None, 0f);

            var textureSize = new Vector2(PongTexture.Width, PongTexture.Height);
            var origin = textureSize / 2f;
            var scale1 = new Vector2(PongSize.X / textureSize.X, PongSize.Y / textureSize.Y);
            var scale2 = new Vector2(Pong2Size.X / textureSize.X, Pong2Size.Y / textureSize.Y);

            // Paddle 1
            _spriteBatch.Draw(PongTexture, PongPosition, null, Color.White, PongRotation, origin, scale1, SpriteEffects.None, 0f);

            // Paddle 2
            _spriteBatch.Draw(PongTexture, Pong2Position, null, Color.White, Pong2Rotation, origin, scale2, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(scoreFont, $"{scorePlayer2}", new Vector2(20, 20), Color.White);
            _spriteBatch.DrawString(scoreFont, $"{scorePlayer1}", new Vector2(_graphics.PreferredBackBufferWidth - 60, 20), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}