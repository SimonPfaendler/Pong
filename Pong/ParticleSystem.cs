using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pong
{
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Size;
        public float LifeTime; // in seconds
        public float MaxLifeTime;
    }

    public class ParticleSystem
    {
        private Random _random;
        private List<Particle> _particles;
        private Texture2D _texture;

        public ParticleSystem(Texture2D texture)
        {
            _texture = texture;
            _particles = new List<Particle>();
            _random = new Random();
        }

        public void Emit(Vector2 position, int count, Color color, float speed = 100f)
        {
            for (int i = 0; i < count; i++)
            {
                var angle = _random.NextDouble() * Math.PI * 2;
                var velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                var randomSpeed = speed * (float)(0.5 + _random.NextDouble());

                _particles.Add(new Particle
                {
                    Position = position,
                    Velocity = velocity * randomSpeed,
                    Color = color,
                    Size = (float)(5 + _random.NextDouble() * 10),
                    LifeTime = 0.5f + (float)_random.NextDouble() * 0.5f,
                    MaxLifeTime = 1.0f
                });
            }
        }

        public void Update(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.LifeTime -= dt;
                p.Position += p.Velocity * dt;
                p.Size *= 0.95f; // Shrink over time

                if (p.LifeTime <= 0)
                {
                    _particles.RemoveAt(i);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var p in _particles)
            {
                // Fade out
                float alpha = p.LifeTime / p.MaxLifeTime;
                Color drawColor = p.Color * alpha;
                
                // Draw centered
                Vector2 origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
                float scale = p.Size / _texture.Width;

                spriteBatch.Draw(_texture, p.Position, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
