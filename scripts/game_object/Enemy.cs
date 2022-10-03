using System.Collections.Generic;
using System.Linq;
using Game.Effect;
using Game.Manager;
using Godot;
using GodotUtilities;

namespace Game.GameObject
{
    public class Enemy : Node2D
    {
        [Node]
        private ResourcePreloader resourcePreloader;

        private GameBoard gameBoard;
        private int health = 2;
        private bool isInvulnerable = true;
        private Vector2 lastDirection;

        private enum AttackType
        {
            Line,
            Wall
        }

        private AttackType[] attackTypes = new AttackType[] {
            AttackType.Line,
            AttackType.Line,
            AttackType.Wall
        };

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.AddToGroup();
                this.WireNodes();
            }
        }

        public override void _Ready()
        {
            gameBoard = this.GetAncestor<GameBoard>();
            gameBoard.TurnManager.Connect(nameof(TurnManager.PlayerTurnStarted), this, nameof(OnPlayerTurnStarted));
            gameBoard.TurnManager.Connect(nameof(TurnManager.EnemyTurnStarted), this, nameof(OnEnemyTurnStarted));
            gameBoard.EnemyPositions[gameBoard.WorldToTile(GlobalPosition)] = this;

            UpdateShield();
        }

        public void Damage()
        {
            GameCamera.Shake();
            if (isInvulnerable) return;
            health--;
            if (health <= 0)
            {
                QueueFree();
            }
        }

        private void UpdateShield()
        {
            if (isInvulnerable)
            {
                var shield = resourcePreloader.InstanceSceneOrNull<ShieldIndicator>();
                AddChild(shield);
                shield.Position = Vector2.Down * 4f;
            }
            else
            {
                this.GetFirstNodeOfType<ShieldIndicator>()?.Die();
            }
        }

        private void DoAttackStraight()
        {
            var directions = new Vector2[] {
                Vector2.Up,
                Vector2.Left,
                Vector2.Down,
                Vector2.Right
            };
            directions = directions.Where(x => x != lastDirection).ToArray();

            var chosenDirectionIndex = MathUtil.RNG.RandiRange(0, directions.Length - 1);
            var chosenDirection = directions[chosenDirectionIndex];

            var chosenAttackIndex = MathUtil.RNG.RandiRange(0, attackTypes.Length - 1);
            var chosenAttack = attackTypes[chosenAttackIndex];

            var currentTile = gameBoard.WorldToTile(GlobalPosition);

            var attackSquares = new List<Vector2>();
            var attackCount = chosenAttack == AttackType.Line ? 1 : 3;

            var rootTile = currentTile + chosenDirection;
            for (int i = 0; i < attackCount; i++)
            {
                if (i == 0)
                {
                    attackSquares.Add(rootTile);
                    continue;
                }
                var perp = chosenDirection.Perpendicular();
                if (i == 2)
                {
                    perp *= -1;
                }

                attackSquares.Add(rootTile + perp);
            }

            foreach (var attackSquare in attackSquares)
            {
                var attack = resourcePreloader.InstanceSceneOrNull<AttackStraight>();
                gameBoard.Entities.AddChild(attack);
                attack.GlobalPosition = gameBoard.TileToWorld(attackSquare);
                attack.SetInitialTile(attackSquare);
                attack.Direction = chosenDirection;
            }

            lastDirection = chosenDirection;
        }

        private async void DoTurn()
        {
            DoAttackStraight();
            await ToSignal(GetTree().CreateTimer(.5f), "timeout");
            gameBoard.TurnManager.EndTurn();
        }

        private void OnPlayerTurnStarted(bool isTenthTurn)
        {
            if (isTenthTurn)
            {
                isInvulnerable = !isInvulnerable;
                UpdateShield();
            }
        }

        private void OnEnemyTurnStarted(bool isTenthTurn)
        {
            if (IsQueuedForDeletion()) return;
            if (isTenthTurn)
            {
                isInvulnerable = !isInvulnerable;
            }
            DoTurn();
        }
    }
}
