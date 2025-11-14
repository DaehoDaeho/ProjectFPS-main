using UnityEngine;

public class RunAwayState : EnemyState
{
    public RunAwayState()
    {

    }

    public RunAwayState(EnemyBrain b)
    {
        brain = b;
    }

    public override string Name()
    {
        return "RunAway";
    }

    public override void OnEnter()
    {
        Vector3 currentForward = brain.transform.forward;
        Vector3 oppositeDir = -currentForward;
        brain.transform.rotation = Quaternion.LookRotation(oppositeDir, Vector3.up);
    }

    public override void OnUpdate(float dt)
    {

    }

    public override void OnExit()
    {
        // ¾øÀ½.
    }
}
