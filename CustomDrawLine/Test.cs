using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       
    }
    void OnPostRender()
    {
        LineDrawer.DrawLine(Matrix4x4.identity, Vector3.zero, new Vector3(0, 0, 10), Color.red);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
