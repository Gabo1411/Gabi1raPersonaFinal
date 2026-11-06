using UnityEngine;

public class EnemyMushroom : EnemyBase
{
    [Header("Hongo")]
    public ParticleSystem particulasMovimiento;

    protected override EnemyDeathEffects.Kind DeathKind => EnemyDeathEffects.Kind.Mushroom;

    // En la arena del jefe brotan como una semilla que se infla; en otro sitio salen de la tierra
    protected override IEntrance StartEntrance()
    {
        if (efectoMuerteArena) return EnemySproutIn.Begin(this, agent, hitCollider);
        return base.StartEntrance();
    }

    protected override void OnStarted()
    {
        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Random.Range(0f, 1f));
        }
    }

    protected override void OnTick()
    {
        if (particulasMovimiento == null || agent == null) return;

        if (agent.velocity.magnitude > 0.2f)
        {
            if (!particulasMovimiento.isPlaying) particulasMovimiento.Play();
        }
        else
        {
            if (particulasMovimiento.isPlaying) particulasMovimiento.Stop();
        }
    }
}
