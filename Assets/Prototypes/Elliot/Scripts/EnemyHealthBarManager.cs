using System.Collections.Generic;
using UnityEngine;

public class EnemyHealthBarManager : MonoBehaviour
{
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private List<EnemyHealthBar> avalibleHealthBars = new List<EnemyHealthBar>();
    [SerializeField] private EnemyHealthBar bossHealthBar;
    private List<EnemyHealthBar> m_usedHealthBars = new List<EnemyHealthBar>();
    
    public EnemyHealthBar InitHealthbar()
    {
        if (avalibleHealthBars.Count == 0)
        {
            GameObject newBar; 
            newBar = Instantiate(healthBarPrefab);
            newBar.transform.SetParent(transform);
            m_usedHealthBars.Add(healthBarPrefab.GetComponent<EnemyHealthBar>());
            return newBar.GetComponent<EnemyHealthBar>();
        }
        else
        {
            EnemyHealthBar oldBar = avalibleHealthBars[0];
            avalibleHealthBars.Remove(oldBar);
            oldBar.gameObject.SetActive(true);
            return oldBar;
        }
    }

    public EnemyHealthBar GetBossBar()
    {
        return bossHealthBar;
    }
    
    public void StartBossHealthBar()
    {
        bossHealthBar.gameObject.SetActive(true);
    }
    
    public void SendBarToPool(EnemyHealthBar bar)
    {
        m_usedHealthBars.Remove(bar);
        avalibleHealthBars.Add(bar);
    }
}
