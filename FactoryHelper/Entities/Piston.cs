﻿using Celeste;
using Celeste.Mod.Entities;
using FactoryHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace FactoryHelper.Entities
{
    [CustomEntity(
        "FactoryHelper/PistonUp = LoadUp",
        "FactoryHelper/PistonDown = LoadDown",
        "FactoryHelper/PistonLeft = LoadLeft",
        "FactoryHelper/PistonRight = LoadRight")]
    class Piston : Entity
    {
        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) => new Piston(data, offset, Directions.Up);
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) => new Piston(data, offset, Directions.Down);
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) => new Piston(data, offset, Directions.Left);
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) => new Piston(data, offset, Directions.Right);

        public static ParticleType P_Steam = new ParticleType(ParticleTypes.Steam)
        {
            SpeedMin = 16.0f,
            SpeedMax = 32.0f,
            DirectionRange = (float)Math.PI / 3,
            Size = 1.5f,
            SizeRange = 0.5f
        };

        public FactoryActivatorComponent Activator { get; }

        public float MoveTime { get; } = 0.2f;

        public float PauseTime { get; } = 0.4f;

        public float InitialDelay { get; private set; } = 0f;

        public float Percent { get; private set; } = 0f;

        public float PauseTimer { get; private set; }

        public bool MovingForward { get; private set; } = false;

        public bool Heated { get; private set; }


        private static readonly Random _rnd = new Random();
        private const int _bodyVariantCount = 4;
        private PistonPart _head;
        private PistonPart _base;
        private Solid _body;
        private SoundSource _sfx;
        private Image[] _bodyImages;
        private Vector2 _startPos;
        private Vector2 _endPos;
        private Vector2 _basePos;
        private int _bodyPartCount;
        private Directions _direction = Directions.Up;

        private float _rotationModifier {
            get
            {
                switch (_direction)
                {
                    default:
                        return 0f;
                    case Directions.Down:
                        return (float)Math.PI;
                    case Directions.Left:
                        return (float)Math.PI / -2;
                    case Directions.Right:
                        return (float)Math.PI / 2;
                }
            }
        }

        private int _headMinimumPositionModifier
        {
            get
            {
                switch (_direction)
                {
                    default:
                    case Directions.Up:
                    case Directions.Left:
                        return -8;
                    case Directions.Right:
                    case Directions.Down:
                        return 8;
                }
            }
        }

        private int _headPositionModifier
        {
            get
            {
                switch (_direction)
                {
                    default:
                    case Directions.Left:
                    case Directions.Up:
                        return 8;
                    case Directions.Right:
                    case Directions.Down:
                        return -8;
                }
            }
        }

        private int _grabPositionModifier
        {
            get
            {
                switch (_direction)
                {
                    default:
                    case Directions.Up:
                    case Directions.Left:
                        return 8;
                    case Directions.Right:
                    case Directions.Down:
                        return 8;
                }
            }
        }

        private int _bodyPositionModifier
        {
            get
            {
                switch (_direction)
                {
                    default:
                    case Directions.Up:
                    case Directions.Left:
                        return 0;
                    case Directions.Down:
                        return (int)Math.Abs(_basePos.Y - _head.Y) - 16;
                    case Directions.Right:
                        return (int)Math.Abs(_basePos.X - _head.X) - 16;
                }
            }
        }

        public Piston(EntityData data, Vector2 offset, Directions direction) 
            : this(data.Position + offset,
                   data.Nodes[0] + offset,
                   data.Nodes[1] + offset,
                   direction,
                   data.Attr("activationId", ""),
                   data.Float("moveTime", 0.4f),
                   data.Float("pauseTime", 0.2f),
                   data.Float("initialDelay", 0f),
                   data.Bool("startActive", true),
                   data.Bool("heated", false))
        {
        }

        public Piston(Vector2 position, Vector2 start, Vector2 end, Directions direction, string activationId, float moveTime, float pauseTime, float initialDelay, bool startActive, bool heated)
        {
            Heated = heated;
            MoveTime = moveTime;
            PauseTime = pauseTime;
            InitialDelay = initialDelay;

            Add(Activator = new FactoryActivatorComponent());
            Activator.StartOn = startActive;
            Activator.ActivationId = activationId == string.Empty ? null : activationId;
            Activator.OnStartOn = OnStartOn;

            _direction = direction;
            
            double length = 0;

            _basePos = position;

            if (_direction == Directions.Up || _direction == Directions.Down)
            {
                _startPos.X = _basePos.X;
                _endPos.X = _basePos.X;

                if (_direction == Directions.Up)
                {
                    _startPos.Y = Math.Min(start.Y, _basePos.Y + _headMinimumPositionModifier);
                    _endPos.Y = Math.Min(end.Y, _basePos.Y + _headMinimumPositionModifier);
                } else
                {
                    _startPos.Y = Math.Max(start.Y, _basePos.Y + _headMinimumPositionModifier);
                    _endPos.Y = Math.Max(end.Y, _basePos.Y + _headMinimumPositionModifier);
                }

                length = Math.Max(Math.Abs(_basePos.Y - _endPos.Y), Math.Abs(_basePos.Y - _startPos.Y));

                _base = new PistonPart(_basePos + new Vector2(2, 0), 12, 8, "objects/FactoryHelper/piston/base00");
                _head = new PistonPart(_startPos, 16, 8, "objects/FactoryHelper/piston/head00");
                _body = new Solid(new Vector2(_basePos.X + 3, Math.Min(_startPos.Y, _basePos.Y) + 8), 10, 0, false);
            }
            else if (_direction == Directions.Left || _direction == Directions.Right)
            {
                _startPos.Y = _basePos.Y;
                _endPos.Y = _basePos.Y;

                if (_direction == Directions.Left)
                {
                    _startPos.X = Math.Min(start.X, _basePos.X + _headMinimumPositionModifier);
                    _endPos.X = Math.Min(end.X, _basePos.X + _headMinimumPositionModifier);
                }
                else
                {
                    _startPos.X = Math.Max(start.X, _basePos.X + _headMinimumPositionModifier);
                    _endPos.X = Math.Max(end.X, _basePos.X + _headMinimumPositionModifier);
                }

                length = Math.Max(Math.Abs(_basePos.X - _endPos.X), Math.Abs(_basePos.X - _startPos.X));

                _base = new PistonPart(_basePos + new Vector2(0, 2), 8, 12, "objects/FactoryHelper/piston/base00");
                _head = new PistonPart(_startPos, 8, 16, "objects/FactoryHelper/piston/head00");
                _body = new Solid(new Vector2(Math.Min(_startPos.X, _basePos.X) + 8, _basePos.Y + 3), Math.Abs(_base.X - _head.X) - 8, 10, false);
            }

            _base.Image.Rotation += _rotationModifier;
            _head.Image.Rotation += _rotationModifier;

            if (Heated)
            {
                _base.Image.Color = new Color(1.0f, 0.8f, 0.8f);
                _head.Image.Color = new Color(1.0f, 0.8f, 0.8f);
            }

            _bodyPartCount = (int)Math.Ceiling(length / 8);
            _bodyImages = new Image[_bodyPartCount];

            for (int i = 0; i < _bodyPartCount; i++)
            {
                Vector2 bodyPosition;
                if (_direction == Directions.Up || _direction == Directions.Down)
                {
                    bodyPosition = new Vector2(-3, i * (_basePos.Y - (_head.Y + _headPositionModifier)) / (_bodyPartCount) + _bodyPositionModifier);
                }
                else
                {
                    bodyPosition = new Vector2(i * (_basePos.X - (_head.X + _headPositionModifier)) / (_bodyPartCount) + _bodyPositionModifier, -3);
                }
                string bodyImage = $"objects/FactoryHelper/piston/body0{_rnd.Next(_bodyVariantCount)}";

                _body.Add(_bodyImages[i] = new Image(GFX.Game[bodyImage])
                {
                    Position = bodyPosition
                });
                _bodyImages[i].CenterOrigin();
                _bodyImages[i].Rotation += _rotationModifier;
                _bodyImages[i].Position = _direction == Directions.Down || _direction == Directions.Up ? new Vector2(8, 4) : new Vector2(4, 8);
                if (Heated)
                {
                    _bodyImages[i].Color = new Color(1.0f, 0.5f, 0.5f);
                }
            }

            _base.Depth = -9010;
            _head.Depth = -9010;
            _body.Depth = -9000;
            _body.AllowStaticMovers = false;
            _head.DisableLightsInside = false;
            _body.DisableLightsInside = false;
            _base.DisableLightsInside = false;
            _base.Add(_sfx = new SoundSource());

            _base.Add(new LightOcclude(0.2f));
            _head.Add(new LightOcclude(0.2f));
            _body.Add(new LightOcclude(0.2f));
        }

        private void OnStartOn()
        {
            if (InitialDelay > ((MoveTime + PauseTime) * 2))
            {
                InitialDelay = InitialDelay % ((MoveTime + PauseTime) * 2);
            }
            if (InitialDelay >= PauseTime + MoveTime)
            {
                InitialDelay -= PauseTime + MoveTime;
                MovingForward = !MovingForward;
            }
            if (InitialDelay >= MoveTime)
            {
                InitialDelay -= MoveTime;
            }
            else
            {
                Percent = 1-(InitialDelay / MoveTime);
                InitialDelay = 0;
            }
            Console.WriteLine($"InitialDelay: {InitialDelay}");
            Console.WriteLine($"Percent: {Percent}");
            Console.WriteLine($"MovingForward: {MovingForward}");
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            UpdatePosition();
        }

        public override void Update()
        {
            base.Update();
            if (Activator.IsOn)
            {

                if (InitialDelay > 0f)
                {
                    InitialDelay -= Engine.DeltaTime;
                }
                else if (PauseTimer > 0f)
                {
                    PauseTimer -= Engine.DeltaTime;
                }
                else if (Percent < 1f)
                {
                    Percent = Calc.Approach(Percent, 1f, Engine.DeltaTime / MoveTime);
                    UpdatePosition();
                    if (!_sfx.Playing)
                    {
                        _sfx.Play("event:/env/local/09_core/conveyor_idle");
                    }
                }
                else
                {
                    PauseTimer = PauseTime;
                    Percent = 0f;
                    MovingForward = !MovingForward;
                }
            } else if (_sfx.Playing)
            {
                _sfx.Stop();
            }
            if (Heated)
            {
                if (_body.HasPlayerRider())
                {
                    Player player = _body.GetPlayerRider();
                    if (player != null)
                    {
                        Vector2 dir;
                        if (player.Bottom <= _body.Top)
                        {
                            dir = -Vector2.UnitY;
                            SceneAs<Level>().ParticlesFG.Emit(P_Steam, 10, player.BottomCenter, Vector2.UnitX * 4f, direction: dir.Angle());
                        }
                        else if (player.Right <= _body.Left)
                        {
                            dir = -Vector2.UnitX;
                            SceneAs<Level>().ParticlesFG.Emit(P_Steam, 10, player.CenterRight, Vector2.UnitY * 4f, direction: dir.Angle());
                        }
                        else if (player.Left >= _body.Right)
                        {
                            dir = Vector2.UnitX;
                            SceneAs<Level>().ParticlesFG.Emit(P_Steam, 10, player.CenterLeft, Vector2.UnitY * 4f, direction: dir.Angle());
                        }
                        else
                        {
                            dir = Vector2.Zero;
                        }
                        _body.GetPlayerRider().Die(dir);
                    }
                }
            }
            if (_direction == Directions.Down)
            {
                DisplacePlayerOnTop(_head);
            }
            if (_direction == Directions.Down || _direction == Directions.Up)
            {
                DisplacePlayerOnTop(_base);
            }
        }

        private void DisplacePlayerOnTop(Solid element)
        {
            if (element.HasPlayerOnTop())
            {
                Player player = element.GetPlayerOnTop();
                if (player != null)
                {
                    if (player.Right - element.Left < element.Right - player.Left)
                    {
                        player.Right = element.Left;
                    }
                    else
                    {
                        player.Left = element.Right;
                    }
                    player.Y += 1f;
                }
            }
        }

        private void UpdatePosition()
        {
            Vector2 posDisplacement;
            var start = MovingForward ? _startPos : _endPos;
            var end = MovingForward ? _endPos : _startPos;
            _head.MoveTo(Vector2.Lerp(end, start, Ease.SineIn(Percent)));
            switch (_direction)
            {
                default:
                case Directions.Up:
                case Directions.Down:
                    posDisplacement = new Vector2(8, 4);
                    float heightBefore = _body.Collider.Height;
                    float heightAfter = Math.Abs(_base.Y - _head.Y) - 8;
                    
                    _body.Y = Math.Min(_head.Y, _base.Y) + 8;
                    _body.Collider.Height = heightAfter;

                    if (_body.HasPlayerClimbing())
                    {
                        Player player = _body.GetPlayerClimbing();
                        if (player != null)
                        {
                            float newY = _base.Y + _grabPositionModifier + (player.Y - (_base.Y + _grabPositionModifier)) * heightAfter / heightBefore;
                            player.LiftSpeed = new Vector2(player.LiftSpeed.X, (newY - player.Y) / Engine.DeltaTime);
                            player.MoveV(newY - player.Y);
                        }
                    }

                    for (int i = 0; i < _bodyPartCount; i++)
                    {
                        _bodyImages[i].Position = new Vector2(-3, i * (_basePos.Y - (_head.Y + _headPositionModifier)) / (_bodyPartCount) + _bodyPositionModifier) + posDisplacement;
                    }
                    break;
                case Directions.Left:
                case Directions.Right:
                    posDisplacement = new Vector2(4, 8);
                    float widthBefore = _body.Collider.Width;
                    float widthAfter = Math.Abs(_base.X - _head.X) - 8;

                    _body.X = Math.Min(_head.X, _base.X) + 8;
                    _body.Collider.Width = widthAfter;

                    if (_body.HasPlayerOnTop())
                    {
                        Player player = _body.GetPlayerOnTop();
                        if (player != null)
                        {
                            float newX = _base.X + _grabPositionModifier + (player.X - (_base.X + _grabPositionModifier)) * widthAfter / widthBefore;
                            player.LiftSpeed = new Vector2((newX - player.X) / Engine.DeltaTime, player.LiftSpeed.Y);
                            player.MoveH(newX - player.X);
                        }
                    }

                    for (int i = 0; i < _bodyPartCount; i++)
                    {
                        _bodyImages[i].Position = new Vector2(i * (_basePos.X - (_head.X + _headPositionModifier)) / (_bodyPartCount) + _bodyPositionModifier, -3) + posDisplacement;
                    }
                    break;
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            scene.Add(_head);
            scene.Add(_base);
            scene.Add(_body);
            Activator.Added(scene);
        }

        public override void Removed(Scene scene)
        {
            scene.Remove(_head);
            scene.Remove(_base);
            scene.Remove(_body);
            base.Removed(scene);
        }

        public enum Directions
        {
            Up,
            Down,
            Left,
            Right
        }

        private class PistonPart : Solid
        {
            public Image Image;

            public PistonPart(Vector2 position, float width, float height, bool safe) : base(position, width, height, safe)
            {
            }

            public PistonPart(Vector2 position, float width, float height, string sprite) : base(position, width, height, false)
            {
                base.Add(Image = new Image(GFX.Game[sprite]));
                Image.Active = true;
                Image.CenterOrigin();
                Image.Position = new Vector2(Width / 2, Height / 2);
            }
        }
    }
}
