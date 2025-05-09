using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bind : AreaSkills
{
    public GameObject bindPrefab;
    private List<BindEffect> spawnedBindEffects = new List<BindEffect>();
    private Transform playerTransform;

    public override void Initialize()
    {
        base.Initialize();
        playerTransform = GameManager.Instance.PlayerSystem.Player.transform;
        if (playerTransform == null)
        {
            Logger.LogError(typeof(Bind), "Player not found for Bind skill!");
        }
        StartCoroutine(BindingCoroutine());
    }

    private IEnumerator BindingCoroutine()
    {
        if (PoolManager.Instance == null)
        {
            Logger.LogError(typeof(Bind), "PoolManager not found!");
            yield break;
        }

        while (true)
        {
            if (playerTransform == null)
                continue;

            if (GameManager.Instance.Monsters != null)
            {
                foreach (Monster enemy in GameManager.Instance.Monsters)
                {
                    if (enemy != null)
                    {
                        float distanceToPlayer = Vector2.Distance(
                            playerTransform.position,
                            enemy.transform.position
                        );
                        if (distanceToPlayer <= Radius)
                        {
                            BindMonster(enemy, Duration);

                            Vector3 effectPosition = enemy.transform.position;

                            BindEffect bindEffect = PoolManager.Instance.Spawn<BindEffect>(
                                bindPrefab,
                                effectPosition,
                                Quaternion.identity
                            );

                            if (bindEffect != null)
                            {
                                bindEffect.gameObject.SetActive(false);
                                bindEffect.transform.SetParent(enemy.transform);
                                bindEffect.transform.localPosition = Vector3.zero;
                                bindEffect.transform.localRotation = Quaternion.identity;
                                bindEffect.gameObject.SetActive(true);

                                spawnedBindEffects.Add(bindEffect);

                                Logger.Log(
                                    typeof(Bind),
                                    $"Bind effect spawned at {effectPosition}, parent: {enemy.name}"
                                );
                            }
                            else
                            {
                                Logger.LogError(typeof(Bind), "Failed to spawn BindEffect!");
                            }
                        }
                    }
                }
            }

            foreach (BindEffect effect in spawnedBindEffects)
            {
                if (effect != null)
                {
                    PoolManager.Instance.Despawn(effect);
                }
            }
            spawnedBindEffects.Clear();

            yield return new WaitForSeconds(TickRate);

            if (!IsPersistent)
            {
                break;
            }
        }
    }

    private void BindMonster(Monster monster, float duration)
    {
        monster.ApplyStun(1, duration);
        monster.ApplyDotDamage(Damage, 0.2f, duration);
    }

    private void OnDrawGizmos()
    {
        if (playerTransform != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(playerTransform.position, Radius);
        }
    }
}
