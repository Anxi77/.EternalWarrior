using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bind : AreaSkills
{
    public GameObject bindPrefab;
    private Player player;

    private Dictionary<Monster, BindEffect> boundMonsters = new Dictionary<Monster, BindEffect>();

    public override void Initialize()
    {
        base.Initialize();
        player = GameManager.Instance.PlayerSystem.Player;
        if (player == null)
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
            if (player == null)
            {
                yield return null;
                continue;
            }

            foreach (Monster enemy in GameManager.Instance.Monsters)
            {
                if (enemy == null)
                    continue;

                float distance = Vector2.Distance(
                    player.transform.position,
                    enemy.transform.position
                );

                if (distance <= Radius)
                {
                    if (!boundMonsters.ContainsKey(enemy))
                    {
                        BindMonster(enemy, Duration);

                        BindEffect bindEffect = PoolManager.Instance.Spawn<BindEffect>(
                            bindPrefab,
                            enemy.transform.position,
                            Quaternion.identity
                        );
                        if (bindEffect != null)
                        {
                            bindEffect.transform.SetParent(enemy.transform);
                            bindEffect.transform.localPosition = Vector3.zero;
                            bindEffect.transform.localRotation = Quaternion.identity;
                            boundMonsters[enemy] = bindEffect;

                            Logger.Log(
                                typeof(Bind),
                                $"Bind effect spawned at {enemy.transform.position}, parent: {enemy.name}"
                            );
                        }
                        else
                        {
                            Logger.LogError(typeof(Bind), "Failed to spawn BindEffect!");
                        }
                    }
                }
            }

            var toRemove = new List<Monster>();
            foreach (var pair in boundMonsters)
            {
                if (!GameManager.Instance.Monsters.Contains(pair.Key) || pair.Key == null)
                {
                    PoolManager.Instance.Despawn(pair.Value);
                    toRemove.Add(pair.Key);
                }
            }
            foreach (var m in toRemove)
                boundMonsters.Remove(m);

            yield return new WaitForSeconds(TickRate);
        }
    }

    private void BindMonster(Monster monster, float duration)
    {
        monster.ApplyStun(duration);
        monster.ApplyDotDamage(Damage, duration, 0.2f, this);
    }

    private void OnDrawGizmos()
    {
        if (player != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(player.transform.position, Radius);
        }
    }
}
