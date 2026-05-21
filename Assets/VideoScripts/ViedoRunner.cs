using System.IO;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class VideoRunner : MonoBehaviour
{
    Matrix4x4 MetricTensor;
    Matrix4x4 diag;

    public Vector4 CameraPosition = new Vector4(0, 10, 0, 10);
    public Vector3 CameraRotation = new Vector3(0, 255, 90);
    public float FOV = 1.5f;
    public Vector2Int MonitorSize = new Vector2Int(192, 108);

    Matrix4x4 MetricTensorAtCam;
    Matrix4x4 localTetradAtCam;

    public float M = 2;
    public float a = 0;

    public float StepSize = 0.2f;
    public float preciseSteps = 0.01f;

    public float Rsoi = 30;

    public int maxSteps = 400;
    public float margin = 1;
    public float SuckInMargin = 1.05f;
    public float T_max = 8000;
    public float AccBrightness = 1f;
    public float BackgroundBrightness = 1f;

    public GameObject TargetPlane;
    public ComputeShader fieldCS;
    public Texture2D DeepStarMap;
    Texture2D resultTexture;
    public float RedShiftMul;
    public float Rout;
    public float Sigma_zero;
    public bool RenderDisk;

    public int tileSize = 100;
    int CurrectTileSizeX = 0;
    int CurrectTileSizeY = 0;
    int OffsetX = 0;
    int OffsetY = 0;

    bool isRenderingFrame = false;
    int currentTile = 0;
    int totalPixels = 0;
    int renderedPixels = 0;

    public bool isRenderingVideo = false;
    bool finishedRenderingFrameLastFrame = false;
    int videoNumber;
    public int frame = 0;
    public int FrameCount = 600;
    public int frameRate = 60;
    public bool saveFrames = true;
    public float speed = 1f;
    public bool  useDelFrame = true;

    public float SpectrumOffset = 0f;
    public float RoutWidthPercent = 20f;

    void Start()
    {
        diag = Matrix4x4.identity;
        diag[0, 0] = -1;
        MetricTensor = Matrix4x4.identity;
        Debug.Log(SystemInfo.graphicsDeviceType);
        totalPixels = MonitorSize.x * MonitorSize.y;
    }

    void DelFrame(float t) //enter what you to change per frame
    {
        //sample to rotate camera around the black hole while moving inwards
        Debug.Log(t);
        float radius = Mathf.Max(60 - t * 0.6f, 0);
        float angle = t * 5f;
        Debug.Log(angle);
        CameraPosition = new Vector4(0, radius * Mathf.Cos(angle * Mathf.Deg2Rad), radius * Mathf.Sin(angle * Mathf.Deg2Rad), 10 * Mathf.Cos(angle * Mathf.Deg2Rad));
        CameraRotation = new Vector3(angle, 270, 90);
    }

    void Dispatch()
    {
        var tile = new RenderTexture(CurrectTileSizeX, CurrectTileSizeY, 0);
        tile.enableRandomWrite = true;
        tile.filterMode = FilterMode.Point;
        tile.Create();

        int kernel = fieldCS.FindKernel("CSMain");

        fieldCS.SetTexture(kernel, "Result", tile);
        fieldCS.SetVector("CameraPosition", CameraPosition);
        fieldCS.SetVector("CameraRotation", CameraRotation);
        fieldCS.SetFloat("FOV", FOV);
        fieldCS.SetInts("MonitorSize", MonitorSize.x, MonitorSize.y);
        fieldCS.SetMatrix("MetricTensorAtCam", MetricTensorAtCam);
        fieldCS.SetMatrix("localTetradAtCam", localTetradAtCam);

        fieldCS.SetFloat("M", M);
        fieldCS.SetFloat("a", a);
        fieldCS.SetFloat("StepSize", StepSize);
        fieldCS.SetFloat("preciseSteps", preciseSteps);
        fieldCS.SetFloat("margin", margin);
        fieldCS.SetInt("maxSteps", maxSteps);
        fieldCS.SetFloat("Rsoi", Rsoi);
        fieldCS.SetFloat("SuckInMargin", SuckInMargin);
        fieldCS.SetFloat("T_max", T_max);
        fieldCS.SetFloat("AccBrightness", AccBrightness);
        fieldCS.SetFloat("Rin", CalcRisco());
        fieldCS.SetTexture(kernel, "Starmap", DeepStarMap);
        fieldCS.SetFloat("BackgroundBrightness", BackgroundBrightness);
        fieldCS.SetFloat("RedShiftMul", RedShiftMul);
        fieldCS.SetFloat("Rout", Rout);
        fieldCS.SetFloat("Sigma_zero", Sigma_zero);
        fieldCS.SetBool("RenderDisk", RenderDisk);
        fieldCS.SetInt("OffsetX", OffsetX);
        fieldCS.SetInt("OffsetY", OffsetY);
        fieldCS.SetFloat("SpectrumOffset", SpectrumOffset);
        fieldCS.SetFloat("Rout_width", RoutWidthPercent / 100f * Rout);
        fieldCS.Dispatch(
            kernel,
            Mathf.CeilToInt(CurrectTileSizeX / 8f),
            Mathf.CeilToInt(CurrectTileSizeY / 8f),
            1
        );
        SetTile(OffsetX, OffsetY, CurrectTileSizeX, CurrectTileSizeY, tile);
        // Clean up the RenderTexture to free memory, suggested by copilot
        tile.Release();
        Object.Destroy(tile);
    }

    void SetTile(int Ox, int Oy, int tileSizeX, int tileSizeY, RenderTexture tile)
    {
        Texture2D tileTexture = ToTexture2D(tile);
        for (int x = 0; x < tileSizeX; x++)
        {
            for (int y = 0; y < tileSizeY; y++)
            {
                resultTexture.SetPixel(Ox + x, Oy + y, tileTexture.GetPixel(x, y));
            }
        }
        resultTexture.Apply();
    }

    public Texture2D ToTexture2D(RenderTexture rTex) // Quelle: Das Internet
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = previous;

        return tex;
    }

    private void Update()
    {
        if (isRenderingVideo)
        {
            HandleVideoRendering();
        }
        if (Input.GetKeyDown(KeyCode.P) && !isRenderingVideo) //start Rendering
        {
            Debug.Log("Starting video rendering");
            StartVideoRendering();
        }
    }

    void StartVideoRendering()
    {
        resultTexture = new Texture2D(MonitorSize.x, MonitorSize.y, TextureFormat.RGBA32, false);
        resultTexture.filterMode = FilterMode.Point;
        if(saveFrames)
        {
            while (Directory.Exists(Path.Combine(@"C:\Users\louia\Documents\Projekte\GRSim\VideoExports\Video_" + videoNumber))) //find next available folder to export video
            {
                videoNumber++;
            }
            Directory.CreateDirectory(Path.Combine(@"C:\Users\louia\Documents\Projekte\GRSim\VideoExports\Video_" + videoNumber));
        }
        isRenderingVideo = true;
    }

    void HandleVideoRendering()
    {
        if (isRenderingFrame)
        {
            HandleFrameRendering();
        }
        else if (finishedRenderingFrameLastFrame)
        {
            if (saveFrames)
            {
                SaveFrame();
            }
            if(frame < FrameCount)
            {
                if (useDelFrame)
                {
                    float t = frame / (float)frameRate;
                    DelFrame(t * speed);
                }
                StartRenderingFrame();
                frame++;
            }
            else
            {
                Debug.Log("Finished video rendering");
                isRenderingVideo = false;
                isRenderingFrame = false;
            }
        }
        else 
        {
            StartRenderingFrame();
        }
    }

    void StartRenderingFrame()
    {
        renderedPixels = 0;
        currentTile = 0;
        OffsetX = 0; // just to be sure, should be already 0 in FinishRendeing
        OffsetY = 0;
        resultTexture = new Texture2D(MonitorSize.x, MonitorSize.y, TextureFormat.RGBA32, false);
        resultTexture.filterMode = FilterMode.Point;
        Debug.Log("Started");
        CalcMetricTensor(CameraPosition, CalcR(CameraPosition, a), a, M);
        MetricTensorAtCam = MetricTensor;
        localTetradAtCam = localTetrad(CameraPosition);
        localTetradAtCam = rotateLocalTetrad(localTetradAtCam, CameraRotation);
        isRenderingFrame = true;
    }

    void FinishFrameRendering()
    {
        OffsetY = 0;
        OffsetX = 0;
        isRenderingFrame = false;
        Debug.Log("Finished");
        finishedRenderingFrameLastFrame = true;
    }

    void HandleFrameRendering()
    {
        RenderTile();
        if (OffsetX + CurrectTileSizeX < MonitorSize.x)
            OffsetX += tileSize;
        else
        {
            OffsetX = 0;
            if (OffsetY + CurrectTileSizeY < MonitorSize.y)
                OffsetY += tileSize;
            else
            {
                //finished
                FinishFrameRendering();
                //GetComponent<Renderer>().material.mainTexture = resultTexture;
            }
        }
        GetComponent<Renderer>().material.mainTexture = resultTexture;
    }

    void RenderTile()
    {
        currentTile++;
        Debug.Log(currentTile);
        renderedPixels += CurrectTileSizeX * CurrectTileSizeY;
        Debug.Log("Progress: " + (renderedPixels / (float)totalPixels * 100f) + "%");
        Debug.Log("Rendered pixels: " + renderedPixels + " / " + totalPixels);
        CurrectTileSizeX = Mathf.Min(tileSize, MonitorSize.x - OffsetX); // adjust tile size for edge cases
        CurrectTileSizeY = Mathf.Min(tileSize, MonitorSize.y - OffsetY);
        Dispatch();
    }

    void SaveFrame()
    {
        Debug.Log("Saving Frame...");
        SaveRenderTextureToPNG(resultTexture, Path.Combine(@"C:\Users\louia\Documents\Projekte\GRSim\VideoExports\Video_" + videoNumber, $"frame_{frame:D5}.png")); // save the texture to a PNG file
        Debug.Log("Texture saved to: " + Path.Combine(@"C:\Users\louia\Documents\Projekte\GRSim\VideoExports\Video_" + videoNumber, $"frame_{frame:D5}.png"));
    }


    #region CameraInstantiation
    public float CalcR2(Vector4 pos, float a)
    {
        float rho2 = pos.y * pos.y + pos.z * pos.z + pos.w * pos.w;
        return 0.5f * (rho2 - a * a + Mathf.Sqrt(Mathf.Abs(Mathf.Pow(rho2 - a * a, 2) + 4 * a * a * pos.w * pos.w)));
    }

    public float CalcR(Vector4 pos, float a)
    {
        return Mathf.Sqrt(Mathf.Abs(CalcR2(pos, a)));
    }

    public void CalcMetricTensor(Vector4 position, float r, float a, float M)
    {
        float z = position.w;
        float H = CalcH(r, z, M, a);
        Vector4 l = CalcL(position, r, a);
        for (int mu = 0; mu < 4; mu++)
            for (int nu = 0; nu < 4; nu++)
                MetricTensor[mu, nu] = diag[mu, nu] + 2 * H * l[mu] * l[nu];
    }

    Vector4 CalcL(Vector4 pos, float r, float a)
    {
        return new Vector4(
            -1,
            (r * pos.y + a * pos.z) / (r * r + a * a),
            (r * pos.z - a * pos.y) / (r * r + a * a),
            pos.w / r);
    }

    float CalcH(float r, float z, float M, float a)
    {
        return (M * r * r * r) / (r * r * r * r + a * a * z * z);
    }

    public Matrix4x4 localTetrad(Vector4 pos)
    {
        Vector4 u = new Vector4(1f / Mathf.Sqrt(Mathf.Abs(-MetricTensorAtCam[0, 0])), 0, 0, 0);
        Vector4 v1 = new Vector4(0, 1, 0, 0);
        Vector4 v2 = new Vector4(0, 0, 1, 0);
        Vector4 v3 = new Vector4(0, 0, 0, 1);
        Matrix4x4 v = new Matrix4x4(u, v1, v2, v3);
        Matrix4x4 sqv = Matrix4x4.zero;
        for (int BaseVector = 1; BaseVector < 4; BaseVector++)
        {
            for (int Component = 0; Component < 4; Component++)
            {
                float Q = 0, D = 0;
                for (int alpha = 0; alpha < 4; alpha++)
                    for (int beta = 0; beta < 4; beta++)
                    {
                        Q += MetricTensorAtCam[alpha, beta] * v[alpha, BaseVector] * u[beta];
                        D += MetricTensorAtCam[alpha, beta] * u[alpha] * u[beta];
                    }
                sqv[Component, BaseVector] = v[Component, BaseVector] - Q / D * u[Component];
            }
        }

        Vector4 e0 = u;
        Vector4 e1 = new Vector4(), e2 = new Vector4(), e3 = new Vector4();
        Vector4 sqvs2 = new Vector4(), sqvs3 = new Vector4();

        for (int dim = 0; dim < 4; dim++)
        {
            float sum = 0;
            for (int alpha = 0; alpha < 4; alpha++)
                for (int beta = 0; beta < 4; beta++)
                    sum += MetricTensorAtCam[alpha, beta] * sqv[alpha, 1] * sqv[beta, 1];
            e1[dim] = sqv[dim, 1] / Mathf.Sqrt(Mathf.Abs(sum));
        }
        for (int dim = 0; dim < 4; dim++)
        {
            float sum = 0;
            for (int alpha = 0; alpha < 4; alpha++)
                for (int beta = 0; beta < 4; beta++)
                    sum += MetricTensorAtCam[alpha, beta] * sqv[alpha, 2] * e1[beta];
            sqvs2[dim] = sqv[dim, 2] - sum * e1[dim];
        }
        for (int dim = 0; dim < 4; dim++)
        {
            float sum = 0;
            for (int alpha = 0; alpha < 4; alpha++)
                for (int beta = 0; beta < 4; beta++)
                    sum += MetricTensorAtCam[alpha, beta] * sqvs2[alpha] * sqvs2[beta];
            e2[dim] = sqvs2[dim] / Mathf.Sqrt(Mathf.Abs(sum));
        }
        for (int dim = 0; dim < 4; dim++)
        {
            float sum1 = 0, sum2 = 0;
            for (int alpha = 0; alpha < 4; alpha++)
                for (int beta = 0; beta < 4; beta++)
                {
                    sum1 += MetricTensorAtCam[alpha, beta] * sqv[alpha, 3] * e1[beta];
                    sum2 += MetricTensorAtCam[alpha, beta] * sqv[alpha, 3] * e2[beta];
                }
            sqvs3[dim] = sqv[dim, 3] - sum1 * e1[dim] - sum2 * e2[dim];
        }
        for (int dim = 0; dim < 4; dim++)
        {
            float sum = 0;
            for (int alpha = 0; alpha < 4; alpha++)
                for (int beta = 0; beta < 4; beta++)
                    sum += MetricTensorAtCam[alpha, beta] * sqvs3[alpha] * sqvs3[beta];
            e3[dim] = sqvs3[dim] / Mathf.Sqrt(Mathf.Abs(sum));
        }
        return new Matrix4x4(e0, e1, e2, e3);
    }

    public Matrix4x4 rotateLocalTetrad(Matrix4x4 Tetrad, Vector3 Angles)
    {
        Matrix4x4 Rtest = Matrix4x4.Rotate(Quaternion.Euler(Angles));
        Matrix4x4 R = Matrix4x4.zero;
        for (int i = 1; i < 4; i++)
            for (int j = 1; j < 4; j++)
                R[i, j] = Rtest[i - 1, j - 1];
        R[0, 0] = 1; // do not disturb time

        Matrix4x4 e = new Matrix4x4();
        for (int mu = 0; mu < 4; mu++)
            for (int aa = 0; aa < 4; aa++)
            {
                float sum = 0;
                for (int b = 0; b < 4; b++)
                    sum += Tetrad[mu, b] * R[b, aa];
                e[mu, aa] = sum;
            }
        return e;
    }

    float CalcRisco() //inner most stable orbit https://en.wikipedia.org/wiki/Innermost_stable_circular_orbit#Rotating_black_holes
    {
        float chi = a / M;
        float Z1 = 1 + Mathf.Pow(1 - chi * chi, 1 / 3.0f) * (Mathf.Pow(1 + chi, 1 / 3.0f) + Mathf.Pow(1 - chi, 1 / 3.0f));
        float Z2 = Mathf.Sqrt(Mathf.Abs(3 * chi * chi + Z1 * Z1));
        float Risco = M * (3 + Z2 - Mathf.Sqrt(Mathf.Abs((3 - Z1) * (3 + Z1 + 2 * Z2))));
        return Risco;
    }

    #endregion;

    #region Export Texture

    public static void SaveRenderTextureToPNG(Texture2D tex, string path)
    {
        RenderTexture previous = RenderTexture.active;

        try
        {
            tex.Apply();

            RotateTexture180(tex);

            byte[] png = tex.EncodeToPNG();

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, png);

            Object.Destroy(tex);
        }
        finally
        {
            RenderTexture.active = previous;
        }
    }

    static void RotateTexture180(Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();

        int last = pixels.Length - 1;

        for (int i = 0; i < pixels.Length / 2; i++)
        {
            (pixels[i], pixels[last - i]) = (pixels[last - i], pixels[i]);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
    }

    #endregion
}

