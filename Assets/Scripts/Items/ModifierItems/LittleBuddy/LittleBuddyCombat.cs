using UnityEngine;

public class LittleBuddyCombat : LittleBuddyState
{
    private EnemyController targetEnemy;
    
    public LittleBuddyCombat(LittleBuddyStateController lbsc) : base(lbsc) { }

    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.red;
    }

    public override void CheckTransitions()
    {
        if (lbsc.combatManager != null && !lbsc.combatManager.IsInCombat)
        {
            lbsc.SetState(new LittleBuddyIdle(lbsc));
        }
    }

    public override void Act()
    {
        if (lbsc.transform.parent == null)
            return;

        Vector3 offset = lbsc.transform.localPosition;
        if (offset.sqrMagnitude < 0.0001f)
            offset = Vector3.right;

        offset = Quaternion.Euler(0f, 0f, lbsc.speed * Time.deltaTime) * offset;
        lbsc.transform.localPosition = offset;

        targetEnemy = lbsc.FindClosestEnemy();
        if (targetEnemy == null) return;
        ShootAtEnemy(targetEnemy);
    }

    private void ShootAtEnemy(EnemyController enemy)
    {
        if (enemy == null || !lbsc.canShoot) return;

        Vector2 direction = enemy.transform.position - lbsc.transform.position;
        lbsc.StartCoroutine(lbsc.Shoot(direction));
    }
}
