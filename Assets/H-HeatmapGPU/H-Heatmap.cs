using Unity.VisualScripting;
using UnityEngine;

public class ScalarFieldVisualizer : MonoBehaviour
{
    public int width = 500;
    public int height = 500;

    public float scale = 1.0f;

    public bool UpdateByTime = true;
    public float UpdateTime;

    public float a;
    public float M;
    public float z;

    public float rim;
    public float rim2;

    public bool enableEventHorizon;
    public bool enableErgosphere;

    public Color Singularity;
    public Color Portal;
    public Color EventHorizon;
    public Color Ergosphere;

    Texture2D texture;

    public float delT = 0;

    [Range(0.0f, 10.0f)]
    public float xgrob; //two sliders for better precision control
    [Range(0.0f, 1.0f)]
    public float xfine;
    public float xpoint;

    void Start()
    {

    }

    public ComputeShader fieldCS;
    RenderTexture target;

    public float CalcEHRadius(float M, float a)
    {
        float r = M + Mathf.Sqrt(M * M - a * a);
        return r;
    }


    public float RAtZ (float r, float z)
    {
        return (r * r) * (1 - (z * z) / (r * r));
    }


    void Dispatch()
    {
        if (target == null)
        {
            target = new RenderTexture(width, height, 0);
            target.enableRandomWrite = true;
            target.filterMode = FilterMode.Point;
            target.Create();
        }

        int kernel = fieldCS.FindKernel("CSMain");

        fieldCS.SetTexture(kernel, "Result", target);
        fieldCS.SetInt("width", width);
        fieldCS.SetInt("height", height);
        fieldCS.SetFloat("scale", scale);
        fieldCS.SetFloat("a", a);
        fieldCS.SetFloat("M", M);
        fieldCS.SetFloat("z", z);
        fieldCS.SetFloat("rim", rim);
        fieldCS.SetFloat("EHRadius", RAtZ(CalcEHRadius(M, a), z));
        fieldCS.SetBool("enableEventHorizon", enableEventHorizon);
        fieldCS.SetBool("enableErgosphere", enableErgosphere);
        fieldCS.SetVector("ColorSingularity", Singularity);
        fieldCS.SetVector("ColorPortal", Portal);
        fieldCS.SetVector("ColorEventHorizon", EventHorizon);
        fieldCS.SetVector("ColorErgosphere", Ergosphere);
        fieldCS.SetFloat("rim2", rim2);
        fieldCS.Dispatch(
            kernel,
            Mathf.CeilToInt(width / 8f),
            Mathf.CeilToInt(height / 8f),
            1
        );

        GetComponent<Renderer>().material.mainTexture = target;
    }


    private void Update()
    {
        xpoint = xgrob + xfine;
        delT += Time.fixedDeltaTime;
        if ( Input.GetKeyDown(KeyCode.P) || (UpdateByTime && delT > UpdateTime)) //if delT = 1 for "continuous" updates 
        {
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            //texture.filterMode = FilterMode.Point;   // crisp pixels
            //texture.wrapMode = TextureWrapMode.Clamp;

            Dispatch();

            //GetComponent<Renderer>().material.mainTexture = texture;
            delT = 0;
        }

    }

}

