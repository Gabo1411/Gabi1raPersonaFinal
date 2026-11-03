using UnityEngine;

// Cactus
public class Enemy : EnemyBase
{
    protected override EnemyDeathEffects.Kind DeathKind => EnemyDeathEffects.Kind.Cactus;

    protected override void OnStarted()
    {
        // Animación de caminar con fase aleatoria para que no se muevan todos sincronizados
        if (animator != null)
        {
            animator.Play("Cactus_WalkFWD", 0, Random.Range(0f, 1f));
        }
    }

    public override void ClearFromMap()
    {
        GenerarParticulas();
        Destroy(gameObject);
    }
}
