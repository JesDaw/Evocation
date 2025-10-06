using System.Collections;
using UnityEngine;

public class WolfSpawning : SpawningController
{
    public override IEnumerator SpawnLoop()
    {
        Spawn(1);
        Spawn(0);
        Debug.Log("teste");
        yield return new WaitForSeconds(1);
    }
}
