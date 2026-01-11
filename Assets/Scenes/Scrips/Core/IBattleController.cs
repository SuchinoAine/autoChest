namespace AutoChess.Core
{
    public interface IBattleController
    {
        void StepUnit(BattleWorld world, Unit u, float dt);
    }
}
