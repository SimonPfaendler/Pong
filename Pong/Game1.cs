using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Pong;

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
        float ballSpeedIncrease = 1.15f;

        Texture2D PongTexture;
        Vector2 PongPosition;
        Vector2 PongSize;
        float PongRotation;

        Texture2D Pong2Texture;
        Vector2 Pong2Position;
        Vector2 Pong2Size;
        float Pong2Rotation;

        // Visual Polish
        ParticleSystem particleSystem;
        List<Vector2> ballTrail;
        float trailTimer = 0f;
        float shakeDuration = 0f;
        float shakeMagnitude = 0f;
        Vector2 shakeOffset = Vector2.Zero;
        Color skyColor = new Color(200, 230, 255); // Brighter sky background

        float prevPongY;
        float prevPong2Y;
        float currentPongVelocityY;
        float currentPong2VelocityY;

        int scorePlayer1 = 0;
        int scorePlayer2 = 0;
        bool gameOver = false;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        SpriteFont scoreFont;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            ballPosition = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            ballSpeed = 350f; // Geschwindigkeit in Pixel/Sekunde
            ballSize = new Vector2(30, 30);

            ballTrail = new List<Vector2>();

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
            ballSpeed = 350f; // Reset der Ballgeschwindigkeit
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

            particleSystem = new ParticleSystem(ballTexture); // Recycle ball texture for particles
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Store previous positions
            prevPongY = PongPosition.Y;
            prevPong2Y = Pong2Position.Y;

            // --- Visual Updates ---
            particleSystem.Update(dt);

            // Screen Shake
            if (shakeDuration > 0)
            {
                shakeDuration -= dt;
                shakeOffset = new Vector2((float)(rnd.NextDouble() * 2 - 1), (float)(rnd.NextDouble() * 2 - 1)) * shakeMagnitude;
            }
            else
            {
                shakeOffset = Vector2.Zero;
            }

            // Ball Trail
            trailTimer += dt;
            if (trailTimer > 0.016f) // Record ~60fps
            {
                trailTimer = 0;
                ballTrail.Add(ballPosition);
                if (ballTrail.Count > 20) ballTrail.RemoveAt(0);
            }
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Check for Reset
            if (gameOver)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    scorePlayer1 = 0;
                    scorePlayer2 = 0;
                    gameOver = false;
                    ResetBall(rnd.Next(0, 2) == 0);
                }
                return; // Stop update if game is over
            }

            // Check Win Condition
            if (scorePlayer1 >= 5 || scorePlayer2 >= 5)
            {
                gameOver = true;
            }



            // Moved dt to top of Update

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
                
                // Wall Impact Effect
                particleSystem.Emit(ballPosition, 10, Color.DarkSlateBlue, 150f);
                shakeDuration = 0.1f;
                shakeMagnitude = 3f;
            }
            else if (ballRect.Bottom >= screenH && ballVelocity.Y > 0)
            {
                ballVelocity.Y = -ballVelocity.Y;
                ballPosition.Y = screenH - ballRadius - 1;
                
                // Wall Impact Effect
                particleSystem.Emit(ballPosition, 10, Color.DarkSlateBlue, 150f);
                shakeDuration = 0.1f;
                shakeMagnitude = 3f;
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
                        // CAP SPEED
                        if (ballSpeed > 1500f) ballSpeed = 1500f;

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

                    // Impact FX
                    particleSystem.Emit(ballPosition, 20, Color.DarkTurquoise, 300f);
                    shakeDuration = 0.2f;
                    shakeMagnitude = 10f;
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
                        // CAP SPEED
                        if (ballSpeed > 1500f) ballSpeed = 1500f;
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

                    // Impact FX
                    particleSystem.Emit(ballPosition, 20, Color.DeepPink, 300f);
                    shakeDuration = 0.2f;
                    shakeMagnitude = 10f;
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
                scorePlayer2 += 1;
                ResetBall(true);
            }
            else if (ballRect.Right >= screenW)
            {
                scorePlayer1 += 1;
                ResetBall(false);
            }

            // --- Player 1 (Mouse Control) ---
            var mouse = Mouse.GetState();
            // Mouse controls Left Paddle
            PongPosition.Y = mouse.Y;
            PongPosition.Y = MathHelper.Clamp(PongPosition.Y, PongSize.Y / 2f, _graphics.PreferredBackBufferHeight - PongSize.Y / 2f);

            // --- Player 2 (AI Control) ---
            float aiSpeed = 220f; // Slightly slower than base ball speed (300) to make it beatable
            
            // Basic tracking
            if (ballPosition.Y < Pong2Position.Y - 10)
            {
                Pong2Position.Y -= aiSpeed * dt;
            }
            else if (ballPosition.Y > Pong2Position.Y + 10)
            {
                Pong2Position.Y += aiSpeed * dt;
            }
            Pong2Position.Y = MathHelper.Clamp(Pong2Position.Y, Pong2Size.Y / 2f, _graphics.PreferredBackBufferHeight - Pong2Size.Y / 2f);

            base.Update(gameTime);
            
            // Calculate Velocity for Tweening (pixels per second)
            if (dt > 0)
            {
                currentPongVelocityY = (PongPosition.Y - prevPongY) / dt;
                currentPong2VelocityY = (Pong2Position.Y - prevPong2Y) / dt;
            }
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(skyColor);

            // TODO: Add your drawing code here#
            var transform = Matrix.CreateTranslation(new Vector3(shakeOffset, 0));
            _spriteBatch.Begin(transformMatrix: transform);
            
            var scale = new Vector2(ballSize.X / ballTexture.Width, ballSize.Y / ballTexture.Height);
            Vector2 ballOrigin = new Vector2(ballTexture.Width / 2, ballTexture.Height / 2);

            // Draw Trail
            for (int i = 0; i < ballTrail.Count; i++)
            {
                float alpha = (float)i / ballTrail.Count;
                float trailScale = 1.0f - (1.0f - alpha) * 0.5f; // Gets smaller
                _spriteBatch.Draw(ballTexture, ballTrail[i], null, Color.DarkSlateBlue * alpha * 0.5f, 0f, 
                    ballOrigin, scale * trailScale, SpriteEffects.None, 0f);
            }

            _spriteBatch.Draw(ballTexture, ballPosition, null, Color.DarkSlateBlue, 0f,
                ballOrigin, scale, SpriteEffects.None, 0f);
            
            // Draw Particles
            particleSystem.Draw(_spriteBatch);

            var textureSize = new Vector2(PongTexture.Width, PongTexture.Height);
            var origin = textureSize / 2f;
            var scale1 = new Vector2(PongSize.X / textureSize.X, PongSize.Y / textureSize.Y);
            var scale2 = new Vector2(Pong2Size.X / textureSize.X, Pong2Size.Y / textureSize.Y);

            // Apply Tweening (Squash & Stretch + Tilt)
            // Player 1
            float stretch1 = 1.0f + Math.Abs(currentPongVelocityY) * 0.0005f;
            stretch1 = MathHelper.Clamp(stretch1, 1.0f, 1.6f); // Max 60% stretch
            Vector2 drawScale1 = scale1;
            drawScale1.X /= stretch1;
            drawScale1.Y *= stretch1;

            // Player 2
            float stretch2 = 1.0f + Math.Abs(currentPong2VelocityY) * 0.0005f;
            stretch2 = MathHelper.Clamp(stretch2, 1.0f, 1.6f);
            Vector2 drawScale2 = scale2;
            drawScale2.X /= stretch2;
            drawScale2.Y *= stretch2;

            // Paddle 1
            _spriteBatch.Draw(PongTexture, PongPosition, null, Color.DarkTurquoise, PongRotation, origin, drawScale1, SpriteEffects.None, 0f);

            // Paddle 2
            _spriteBatch.Draw(PongTexture, Pong2Position, null, Color.DeepPink, Pong2Rotation, origin, drawScale2, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(scoreFont, $"{scorePlayer2}", new Vector2(20, 20), Color.DarkSlateBlue);
            _spriteBatch.DrawString(scoreFont, $"{scorePlayer1}", new Vector2(_graphics.PreferredBackBufferWidth - 60, 20), Color.DarkSlateBlue);

            if (gameOver)
            {
                string msg = "Game Over!";
                string subMsg = "Press [Space] to Restart";

                // Center the text
                Vector2 msgSize = scoreFont.MeasureString(msg);
                Vector2 subMsgSize = scoreFont.MeasureString(subMsg);
                Vector2 screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);

                _spriteBatch.DrawString(scoreFont, msg, screenCenter - msgSize / 2 - new Vector2(0, 30), Color.Red);
                _spriteBatch.DrawString(scoreFont, subMsg, screenCenter - subMsgSize / 2 + new Vector2(0, 10), Color.Yellow);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}