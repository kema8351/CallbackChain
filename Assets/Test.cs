using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
        Chain.Create(() => Debug.Log("normal action")).Run();

        Chain.Create(() => Wait(1f, "enumerator")).Run();

        Chain.Serial(
            () => Debug.Log("serial1"),
            () => Debug.Log("serial2"),
            () => Debug.Log("serial3"),
            () => Debug.Log("serial4"),
            () => Debug.Log("serial5")
        ).Run();

        var parallel = Chain.Parallel(
            Wait(3f, "parallel1"),
            Wait(2f, "parallel2"),
            Wait(4f, "parallel3"),
            Wait(1f, "parallel4"),
            Wait(5f, "parallel5")
        );
        parallel.Run();
        parallel.Run();
    }

    IEnumerator Wait(float waitSeconds, string debug)
    {
        yield return new WaitForSeconds(waitSeconds);
        Debug.Log(debug);
    }
}
