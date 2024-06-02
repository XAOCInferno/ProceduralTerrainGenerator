using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnDisableDestroy : MonoBehaviour
{
    private void OnDisable()
    {
        Destroy(gameObject);
    }
}
