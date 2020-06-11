using UnityEngine;

[ExecuteAlways]
public class ChangeMatProperty :MonoBehaviour
{
    [SerializeField] public Color color;
    private Color _tempColor;

    public float addLight;
    private Material mat;

    private int addLightProp;
    private bool hasAddLightProp=false;
    // Start is called before the first frame update

    void Start()
    {
        mat = this.gameObject.GetComponent<Projector>().material;
        if (!mat)
            return;
        color = mat.color;

            
        addLightProp = Shader.PropertyToID("_AddLight");
        hasAddLightProp = mat.HasProperty(addLightProp);
        if (hasAddLightProp)
        {
            addLight = mat.GetFloat(addLightProp);
        }

        _tempColor = color;
    }

    // Update is called once per frame
    void Update()
    {
        mat.SetColor("_Color", color);
            
        if (hasAddLightProp)
        {
            addLight = Mathf.Clamp(addLight, 0, 100f);
            mat.SetFloat(addLightProp, addLight);
        }
    }
}

