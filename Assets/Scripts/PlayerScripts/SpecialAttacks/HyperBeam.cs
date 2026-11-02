using System.Collections;
using UnityEngine;

public class HyperBeam : SpecialAttack
{
    [SerializeField] private GameObject beamObj;
    private HyperBeamCollision hyperBeamCollision;

    void Awake()
    {
        hyperBeamCollision = beamObj.GetComponent<HyperBeamCollision>();
        hyperBeamCollision.damage = spDamage;
        GetComponentInParent<PlayerInfo>().spAttack = this;
    }

    public override void StartAttack()
    {
        if(!canAttack) return;

        Debug.Log("beam attack starting");


        StartCoroutine(BeamAttack());
    }

    private IEnumerator BeamAttack()
    {
        // The beam cannot be used during its active duration or its recovery.
        BeginCooldown(Mathf.Max(0f, spLength) + Mathf.Max(0f, spCooldown));
        beamObj.SetActive(true);
        Debug.Log("starting beam");
        yield return new WaitForSeconds(spLength);
        Debug.Log("turning beam off");
        beamObj.SetActive(false);

        yield return new WaitForSeconds(spCooldown);
        Debug.Log("can special again");
        canAttack = true;
    }
}
