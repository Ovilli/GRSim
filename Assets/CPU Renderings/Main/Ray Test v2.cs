using JetBrains.Annotations;
using NUnit.Framework.Constraints;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using static UnityEditor.PlayerSettings;

public class RayTestv2 : MonoBehaviour
{
    Matrix4x4 MetricTensor;
    Matrix4x4 InvMetricTensor;
    Matrix4x4[] dMetricTensor;
    Matrix4x4[] Christoffelsymbols;
    Matrix4x4 diag;

    public Vector4 CameraPosition = new Vector4(0, 100, 0, 0);
    public Vector3 CameraRotation;
    public float FOV;
    public Vector2Int MonitorSize = new Vector2Int(1080, 1920);

    Matrix4x4 MetricTensorAtCam;
    Matrix4x4 localTetradAtCam;

    public float M = 1;
    public float a = 0.1f;

    public int N = 10;
    public float StepSize = 0.2f;
    public float preciseSteps = 0.1f;
    public GameObject Ball;
    public GameObject Parent;
    public float delT = 0;
    public float refreshTime = 0; //increase for less FPS

    public GameObject BlackHole;
    public float precision;
    public bool simulate;

    public float TempNull;
    public float IntensityNull;
    public float Rsoi;

    public Material CamMaterial;
    Texture2D CamTexture;
    public int maxSteps;
    public bool Render;
    public float margin = 2;
    public int rayX;
    public int rayY;
    public bool focusOne;

    void Start()
    {
        diag = Matrix4x4.identity;
        diag[0, 0] = -1;
        MetricTensor = Matrix4x4.identity;
        dMetricTensor = new Matrix4x4[4];
        Christoffelsymbols = new Matrix4x4[4];
    }

    #region Tests

    public void TestRay()
    {
        BlackHole.transform.localScale = Vector3.one * 4 * M;
        CalcMetricTensor(CameraPosition, CalcR(CameraPosition, a), a, M); //Calculate Metric Tensor at Camera Position
        MetricTensorAtCam = MetricTensor;

        localTetradAtCam = localTetrad(CameraPosition);
        localTetradAtCam = rotateLocalTetrad(localTetradAtCam, CameraRotation);


        for (int i = 0; i < MonitorSize.x; i++)
        {
            for (int j = 0; j < MonitorSize.y; j++)
            {
                LightRay Ray = instantiateRay(CameraPosition, FOV, MonitorSize.x, MonitorSize.y, new Vector2(i, j), localTetradAtCam); //put together later
                Color colour = new Color(i / MonitorSize.x, j / MonitorSize.y, 0);
                StepLightNTimes(Ray, N, colour);
            }
        }
    }

    public void StepLightNTimes(LightRay ray, int n, Color c)
    {
        LightRay nextPos = ray;
        LightRay oldPos = ray;
        for (int i = 0; i < n; i++)
        {
            //Debug.Log("gkk = " + gkk(ray.k));
            //Debug.Log("H = " + CalcH(CalcR(nextPos.pos, a), nextPos.pos.z, M, a));
            //StepSize = precision / CalcH(CalcR(nextPos.pos, a), nextPos.pos.z, M, a); //adjust stepSize to strength of Graavitational Field
            oldPos = nextPos;
            precision = StepSize;
            float r = CalcR(nextPos.pos, a);
            precision = StepSize;
            Debug.Log(r - 2 * M);
            if (r - 2 * M < margin)
            {
                precision = preciseSteps;
                i--;//dummest fix ever... If light is close to black hole bugs are common so step size is drasctly reduced which led to light running out of sim steps, this makes it go on infinte as long it is in the margin... Nothing could ever go wrong here..... suttle foreshadowing
            }
            Debug.Log("precision:" + precision);
            nextPos = StepLight(nextPos, precision); //calculate nextPosition
            if (HitObject(nextPos.pos, oldPos.pos).w != 0)
            {
                return; //Terminate if Hit
            }
            GameObject ball = Instantiate(Ball, new Vector3(nextPos.pos.y, nextPos.pos.z, nextPos.pos.w), transform.rotation); //plan Ball
            ball.transform.parent = Parent.transform;
            ball.GetComponent<Renderer>().material.color = new Color(c.r, c.g, i / n);
        }
    }

    public float gkk(Vector4 k)
    {
        float sum = 0;
        for (int mu = 0; mu < 4; mu++)
        {
            for (int nu = 0; nu < 4; ++nu)
            {
                sum += MetricTensor[mu, nu] * k[mu] * k[nu];
            }
        }
        return sum;
    }

    public Matrix4x4 C(Matrix4x4 Tetrad) //test if local-Tetrad-function actually forms local Tetead, Result: yay
    {
        Matrix4x4 output = new Matrix4x4();
        for (int a = 0; a < 4; a++)
        {
            for (int b = 0; b < 4; b++)
            {
                float sum = 0;
                for (int mu = 0; mu < 4; mu++)
                {
                    for (int nu = 0; nu < 4; nu++)
                    {
                        sum += MetricTensorAtCam[mu, nu] * Tetrad[mu, a] * Tetrad[nu, b];
                    }
                }
                if (sum < 0.0001f && sum > -0.0001f)
                {
                    sum = 0;
                }
                output[a, b] = sum;
            }
        }
        return output;
    }

    #endregion

    #region Debugging Helpers

    public void LogRay(LightRay ray)//function to print Ray entity to console (pos, k)
    {
        Debug.Log("pt:");
        Debug.Log(ray.pos.x);
        Debug.Log("px:");
        Debug.Log(ray.pos.y);
        Debug.Log("py:");
        Debug.Log(ray.pos.z);
        Debug.Log("pz:");
        Debug.Log(ray.pos.w);
        Debug.Log("kt");
        Debug.Log(ray.k.x);
        Debug.Log("kx");
        Debug.Log(ray.k.y);
        Debug.Log("ky");
        Debug.Log(ray.k.z);
        Debug.Log("kz");
        Debug.Log(ray.k.w);
    }

    public void LogTensor(Matrix4x4 m)//function to print Tensor to console
    {
        Debug.Log("(" + m[0, 0] + ", " + m[0, 1] + ", " + m[0, 2] + ", " + m[0, 3] + ")");
        Debug.Log("(" + m[1, 0] + ", " + m[1, 1] + ", " + m[1, 2] + ", " + m[1, 3] + ")");
        Debug.Log("(" + m[2, 0] + ", " + m[2, 1] + ", " + m[2, 2] + ", " + m[2, 3] + ")");
        Debug.Log("(" + m[3, 0] + ", " + m[3, 1] + ", " + m[3, 2] + ", " + m[3, 3] + ")");
    }

    public void LogTensorArray(Matrix4x4[] mArray)//function to print Tensor array(Chritoffel Symbols) to console
    {
        for (int i = 0; i < mArray.Length; i++)
        {
            Debug.Log($"Tensor {i}:");
            LogTensor(mArray[i]);
        }
    }
    public static void CopyMatrixArray(Matrix4x4[] mArray) //puts Array of Matrices into clipboard //function written by ChatGPT for debugging //actually written by copilot, but copilot gives all credit to ChatGPT
    {
        var sb = new StringBuilder();
        for (int index = 0; index < mArray.Length; index++)
        {
            sb.AppendLine($"Matrix {index}:");
            for (int row = 0; row < 4; row++)
            {
                sb.AppendLine(
                    $"{mArray[index][row, 0]}, {mArray[index][row, 1]}, {mArray[index][row, 2]}, {mArray[index][row, 3]}"
                );
            }
        }
        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Matrix array copied to clipboard");
    }

    public static void CopyMatrix(Matrix4x4 m) //same same //function written by ChatGPT for debugging
    {
        var sb = new StringBuilder();
        for (int row = 0; row < 4; row++)
        {
            sb.AppendLine(
                $"{m[row, 0]}, {m[row, 1]}, {m[row, 2]}, {m[row, 3]}"
            );
        }

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Matrix copied to clipboard");
    }

    public void LogBiggestChristoffel()
    {
        float max = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    if (Mathf.Abs(Christoffelsymbols[i][j, k]) > max) max = Christoffelsymbols[i][j, k];
                }
            }
        }
        Debug.Log("biggest christoffel = " + max);
    }

    public void FindAsymetries()
    {
        Vector4 posx = new Vector4(0, 10, 0, 0);
        Vector4 posy = new Vector4(0, 0, 5, 0);
        Vector4 posz = new Vector4(0, 0, 0, 10);
        Vector4 posnx = new Vector4(0, -10, 0, 0);
        Vector4 posny = new Vector4(0, 0, -10, 0);
        Vector4 posnz = new Vector4(0, 0, 0, -10);
        Debug.Log("r");
        Debug.Log(Mathf.Sqrt(CalcR(posx, a)));
        Debug.Log(Mathf.Sqrt(CalcR(posy, a)));
        Debug.Log(Mathf.Sqrt(CalcR(posz, a)));
        Debug.Log(Mathf.Sqrt(CalcR(posnx, a)));
        Debug.Log(Mathf.Sqrt(CalcR(posny, a)));
        Debug.Log(Mathf.Sqrt(CalcR(posnz, a)));
        Debug.Log("DMetric:");
        CalcDelMetricTensor(posx, CalcR(posx, a), a, M);
        Matrix4x4[] m1 = dMetricTensor;
        CalcDelMetricTensor(posy, CalcR(posy, a), a, M);
        Matrix4x4[] m2 = dMetricTensor;
        LogTensorArray(m1);
        LogTensorArray(m2);
        Debug.Log("Dif DelMetric Tensor");
        LogTensorArray(
            matrix4X4ArraySub(
                m1,
                m2));
        Debug.Log("Dif Mag DMetric Tensor");
        float mm1 = TensorArrayMagnitude(m1);
        float mm2 = TensorArrayMagnitude(m2);
        Debug.Log(mm1 - mm2);
    }

    #endregion

    #region structs

    public struct LightRay
    {
        public LightRay(Vector4 pos, Vector4 k)
        {
            this.pos = pos;
            this.k = k;
        }
        public Vector4 pos;
        public Vector4 k; // Richtung

    }

    public struct Matrix3x3 //doch nicht...
    {
        public Matrix3x3(Vector3 column0, Vector3 column1, Vector3 column2)
        {
            this.column0 = column0;
            this.column1 = column1;
            this.column2 = column2;
        }
        public Vector3 column0;
        public Vector3 column1;
        public Vector3 column2;
    }

    #endregion

    #region RaySteppingCalculations

    public float CalcR(Vector4 pos, float a)
    {
        float r = Mathf.Sqrt(CalcR2(pos, a));
        return r;
    }

    public LightRay StepLight(LightRay Y, float h)
    {
        //accleration
        //k_n is stored as LightRay, because it has the same dimensions... But it is build different (x->k,k->a)
        LightRay k1 = new LightRay(new Vector4(0, 0, 0, 0), new Vector4(0, 0, 0, 0));
        LightRay k2 = new LightRay(new Vector4(0, 0, 0, 0), new Vector4(0, 0, 0, 0));
        LightRay k3 = new LightRay(new Vector4(0, 0, 0, 0), new Vector4(0, 0, 0, 0));
        LightRay k4 = new LightRay(new Vector4(0, 0, 0, 0), new Vector4(0, 0, 0, 0));
        //k1
        CalculateChristoffelsymbols(Y.pos, CalcR(Y.pos, a), a, M);
        //LogBiggestChristoffel(); //remove later
        k1 = CalculateA(Y);
        //k2
        LightRay k2TrialState = new LightRay(); //make new approximation
        for (int mu = 0; mu < 4; mu++)
        {
            k2TrialState.pos[mu] = Y.pos[mu] + (h / 2) * k1.pos[mu]; //k1.pos actually k
            k2TrialState.k[mu] = Y.k[mu] + (h / 2) * k1.k[mu]; //k1.k actually a
        }
        CalculateChristoffelsymbols(k2TrialState.pos, CalcR(k2TrialState.pos, a), a, M);
        //LogBiggestChristoffel(); //remove later
        k2 = CalculateA(k2TrialState);
        //k3
        LightRay k3TrialState = new LightRay(new Vector4(0, 0, 0, 0), new Vector4(0, 0, 0, 0));
        for (int mu = 0; mu < 4; mu++)
        {
            k3TrialState.pos[mu] = Y.pos[mu] + (h / 2) * k2.pos[mu]; //k2.pos actually k
            k3TrialState.k[mu] = Y.k[mu] + (h / 2) * k2.k[mu]; //k2.k actually a
        }
        CalculateChristoffelsymbols(k3TrialState.pos, CalcR(k3TrialState.pos, a), a, M);
        //LogBiggestChristoffel(); //remove later
        k3 = CalculateA(k3TrialState);
        //k4
        LightRay k4TrialState = new LightRay(new Vector4(0, 0, 0, 0), new Vector4(0, 0, 0, 0));
        for (int mu = 0; mu < 4; mu++)
        {
            k4TrialState.pos[mu] = Y.pos[mu] + h * k3.pos[mu]; //k2.pos actually k
            k4TrialState.k[mu] = Y.k[mu] + h * k3.k[mu]; //k2.k actually a
        }
        CalculateChristoffelsymbols(k4TrialState.pos, CalcR(k4TrialState.pos, a), a, M);
        //LogBiggestChristoffel(); //remove later
        k4 = CalculateA(k4TrialState);
        float posX = Y.pos.x + (h / 6) * (k1.pos.x + 2 * k2.pos.x + 2 * k3.pos.x + k4.pos.x); //Ich bereue es leicht Copilot deaktiviert zu haben......
        float posY = Y.pos.y + (h / 6) * (k1.pos.y + 2 * k2.pos.y + 2 * k3.pos.y + k4.pos.y);
        float posZ = Y.pos.z + (h / 6) * (k1.pos.z + 2 * k2.pos.z + 2 * k3.pos.z + k4.pos.z);
        float posW = Y.pos.w + (h / 6) * (k1.pos.w + 2 * k2.pos.w + 2 * k3.pos.w + k4.pos.w);
        float kX = Y.k.x + (h / 6) * (k1.k.x + 2 * k2.k.x + 2 * k3.k.x + k4.k.x); //maybe change naming if it makes it less confusing
        float kY = Y.k.y + (h / 6) * (k1.k.y + 2 * k2.k.y + 2 * k3.k.y + k4.k.y);
        float kZ = Y.k.z + (h / 6) * (k1.k.z + 2 * k2.k.z + 2 * k3.k.z + k4.k.z);
        float kW = Y.k.w + (h / 6) * (k1.k.w + 2 * k2.k.w + 2 * k3.k.w + k4.k.w);
        LightRay Y_new = new LightRay(new Vector4(posX, posY, posZ, posW), new Vector4(kX, kY, kZ, kW));
        //Y_new.k.x = AdjustForConstraints(Y_new);
        return Y_new;
    }

    public LightRay CalculateA(LightRay aprxLight) //position, k -> k_n(k, a)
    {
        LightRay Output = new LightRay(new Vector4(0, 0, 0, 0), new Vector4(0, 0, 0, 0));
        for (int mu = 0; mu < 4; mu++)
        {
            for (int alpha = 0; alpha < 4; alpha++)
            {
                for (int beta = 0; beta < 4; beta++)
                {
                    Output.k[mu] += -1 * Christoffelsymbols[mu][alpha, beta] * aprxLight.k[alpha] * aprxLight.k[beta];
                }
            }
        }
        Output.pos = aprxLight.k;
        return Output;
    }

    public Vector4 HitObject(Vector4 newPos, Vector4 oldPos) //Vector3 for color + float for Metainformation
    {
        bool BlackHole = HitBlackHole(newPos);
        Vector4 FlatAccretionDisk = HitFlatAccretionDisk(newPos, oldPos);
        Vector4 SOI = HitSOI(newPos);
        for (int mu = 0; mu < 4; mu++) //sometimes stuff glitches near the black hole and things go NaN, that is counted as sucked into the Black hole
        {
            if (float.IsNaN(newPos[mu]))
            {
                Debug.Log("gone but not forgotten");
                return new Vector4(0, 0, 0, -1); //some jokes must be allowed
            }
        }
        if (BlackHole) // return meta = 1
        {
            Debug.Log("Nomm Nomm");
            return new Vector4(0, 0, 0, 1);
        }
        else if (FlatAccretionDisk.w == 1) //return color + meta = 1 for hit
        {
            Debug.Log("Is it a burd?");
            return FlatAccretionDisk;
        }
        else if (SOI.w == 1)
        {
            Debug.Log("Its a the wall on the edge of the universe!");
            return SOI;
        }
        return new Vector4(0, 0, 0, 0);
    }

    public bool HitBlackHole(Vector4 newPos)
    {
        float EventHorizonRadius = 2 * M;
        if (CalcR(newPos, a) <= EventHorizonRadius * 1.05f)
        {
            return true;
        }
        return false;
    }

    Vector4 HitFlatAccretionDisk(Vector4 newPos, Vector4 oldPos)
    {
        if (newPos.w * oldPos.w >= 0)
        {
            //Debug.Log("nu ugh");
            return new Vector4(0, 0, 0, 0);
        }
        Vector4 InterceptionPoint = newPos + oldPos; //apprx point where it crosses the rotation plain
        InterceptionPoint /= 2; //bessere Schnittpubkt methode
        InterceptionPoint.w = 0;
        float Radius = CalcR(InterceptionPoint, a);
        float Rin = 6 * M;
        float Rout = 9 * M; //some random guess
        if (Radius < Rin || Radius > Rout)
        {
            return new Vector4(0, 0, 0, 0);
        }
        return new Vector4(255 / (Radius - 6 * M), 0, 0, 1);
        //float Temp = TempNull * Mathf.Pow(Rin / Radius, -3 / 4);
        //float Intensity = IntensityNull * Mathf.Pow(Rin / Radius, 3);

    }

    Vector4 HitSOI(Vector4 pos)
    {
        float Radius = CalcR(pos, a);
        if (Radius < Rsoi)
        {
            return new Vector4(0, 0, 0, 0);
        }
        pos.Normalize();
        //float x = Mathf.Atan(pos.x / pos.y); //GPU for wont like this, maybe find suppliment when transferring, scusa mi per mio inglese scarso, mi esercito di piu italiano al momento
        //float y = Mathf.Atan(pos.x / pos.z);
        //float light = Random.Range(0, 255);
        //light = Mathf.Sqrt(light);
        pos.Normalize();
        Vector2 point = GetDirectionAngles(new Vector3(pos.y, pos.z, pos.w));
        float light = Mathf.PerlinNoise(point.x, point.y);
        return new Vector4(light, light, light, 1);
    }

    Vector2 GetDirectionAngles(Vector3 v)
    {
        // Yaw (horizontal angle)
        float yaw = Mathf.Atan2(v.z, v.x);

        // Pitch (vertical angle)
        float horizontalLength = Mathf.Sqrt(v.x * v.x + v.z * v.z);
        float pitch = Mathf.Atan2(v.y, horizontalLength);

        // Convert to degrees
        yaw *= Mathf.Rad2Deg;
        pitch *= Mathf.Rad2Deg;

        return new Vector2(yaw, pitch);
    }

    public float AdjustForConstraints(LightRay Light) //destroys stuff fsr //warum bin ich so schrecklich im Namen geben???
    {
        CalcMetricTensor(Light.pos, Mathf.Sqrt(CalcR2(Light.pos, a)), a, M);
        float pd2 = 0; //p /2
        for (int i = 0; i < 4; i++)
        {
            pd2 += MetricTensor[0, i] * Light.k[i];
        }
        float Q = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Q += MetricTensor[i, j] * Light.k[i] * Light.k[j];
            }
        }
        Q *= MetricTensor[0, 0];
        float k0 = (pd2 + Mathf.Sqrt(pd2 * pd2 - Q)) / (MetricTensor[0, 0]);
        return k0; //Vorher k0 * k0 fsr
    }


    #region Metric Tensor Calculation

    public void CalcDelMetricTensor(Vector4 position, float r, float a, float M)
    {
        Vector4 L = CalcL(position, r, a);
        Vector4 delR = CalcDelR(position, a);
        Vector4[] delL = CalcDelL(position, r, delR, a);
        Vector4 delH = CalcDelH(position, r, M, a);
        float H = CalcH(r, position.w, M, a); //actually z, but (t, x, y, z) -> (x, y, z, w)

        for (int lambda = 1; lambda < 4; lambda++)
        {
            for (int mu = 0; mu < 4; mu++)
            {
                for (int nu = 0; nu < 4; nu++)
                {
                    dMetricTensor[lambda][mu, nu] = 2 * (delH[lambda] * L[mu] * L[nu] + H * delL[lambda][mu] * L[nu] + H * delL[lambda][nu] * L[mu]);
                }
            }
        }
    }
    public void CalcMetricTensor(Vector4 position, float r, float a, float M)
    {
        float z = position.w; //ahhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
        float H = CalcH(r, z, M, a);
        Vector4 l = CalcL(position, r, a);
        for (int mu = 0; mu < 4; mu++)
        {
            for (int nu = 0; nu < 4; nu++)
            {
                MetricTensor[mu, nu] = diag[mu, nu] + 2 * H * l[mu] * l[nu];
            }
            InvMetricTensor = CalcInverseMetric(H, l);
        }
    }

    public Matrix4x4 CalcInverseMetric(float H, Vector4 L)
    {
        Vector4 Lup = new Vector4(-L[0], L[1], L[2], L[3]);
        Matrix4x4 inv = diag;
        for (int mu = 0; mu < 4; mu++)
        {
            for (int nu = 0; nu < 4; nu++)
            {
                inv[mu, nu] -= 2 * H * Lup[mu] * Lup[nu];
            }
        }
        return inv;
    }
    public void CalculateChristoffelsymbols(Vector4 pos, float r, float a, float M)
    {
        for (int mu = 0; mu < 4; mu++)
        {
            CalcMetricTensor(pos, r, a, M);
            CalcDelMetricTensor(pos, r, a, M);
            for (int alpha = 0; alpha < 4; alpha++)
            {
                for (int beta = 0; beta < 4; beta++)
                {
                    Christoffelsymbols[mu][alpha, beta] = 0;
                    for (int nu = 0; nu < 4; nu++)
                    {
                        Christoffelsymbols[mu][alpha, beta] += InvMetricTensor[mu, nu] * (dMetricTensor[alpha][nu, beta] + dMetricTensor[beta][nu, alpha] - dMetricTensor[nu][alpha, beta]);
                    }
                    Christoffelsymbols[mu][alpha, beta] = 0.5f * Christoffelsymbols[mu][alpha, beta];
                }
            }
        }
    }


    #region Auxiliary Functions for Metric Tensor
    Vector4 CalcL(Vector4 pos, float r, float a)
    {
        Vector4 L = new Vector4(-1, (r * pos.y + a * pos.z) / (r * r + a * a), (r * pos.z - a * pos.y) / (r * r + a * a), pos.w / r);
        return L;
    }

    float CalcH(float r, float z, float M, float a) //static strength of deviation from flat Minkowski Spacetime space (H = 0 -> Minkowski) at Horizon H = infinty
    {
        float H = (M * Mathf.Pow(r, 3)) / (Mathf.Pow(r, 4) + Mathf.Pow(a, 2) * Mathf.Pow(z, 2));
        return H;
    }

    float CalcDelH_R(float r, float z, float M, float a)
    {
        float numerator = M * (3 * Mathf.Pow(r, 2) * (Mathf.Pow(r, 4) + Mathf.Pow(a, 2) * Mathf.Pow(z, 2)) - Mathf.Pow(r, 3) * (4 * Mathf.Pow(r, 3)));
        float denominator = Mathf.Pow(Mathf.Pow(r, 4) + Mathf.Pow(a, 2) * Mathf.Pow(z, 2), 2);
        return numerator / denominator;
    }

    float CalcDelH_Z(float r, float z, float M, float a)
    {
        float numerator = -2 * M * Mathf.Pow(r, 3) * Mathf.Pow(a, 2) * z;
        float denominator = Mathf.Pow(Mathf.Pow(r, 4) + Mathf.Pow(a, 2) * Mathf.Pow(z, 2), 2);
        return numerator / denominator;
    }

    Vector4 CalcDelH(Vector4 pos, float r, float M, float a)
    {
        float delH_R = CalcDelH_R(r, pos.w, M, a);
        float delH_Z = CalcDelH_Z(r, pos.w, M, a);
        Vector4 dr = CalcDelR(pos, a);
        float delH_X = delH_R * dr.y; //ahhhhhh
        float delH_Y = delH_R * dr.z;
        float delH_Z_final = delH_R * dr.w + delH_Z;
        return new Vector4(0, delH_X, delH_Y, delH_Z_final);
    }

    #region Derivatives of L

    public Vector4[] CalcDelL(Vector4 pos, float r, Vector4 delR, float a) //
    {
        int[] KroX = new int[4] { 0, 1, 0, 0 }; //Kroenecker X
        int[] KroY = new int[4] { 0, 0, 1, 0 };
        int[] KroZ = new int[4] { 0, 0, 0, 1 };
        Vector4 delLT = new Vector4(0, 0, 0, 0); //partial derivative in all dircetions of the time component of the null covector is zero
        Vector4 delLX = new Vector4(0, 0, 0, 0); //just initialization
        Vector4 delLY = new Vector4(0, 0, 0, 0);
        Vector4 delLZ = new Vector4(0, 0, 0, 0);

        float FactorX = (pos.y * (r * r + a * a) - 2 * r * (r * pos.y + a * pos.z)) / (Mathf.Pow(r * r + a * a, 2));
        for (int dim = 1; dim < 4; dim++)
        {
            delLX[dim] = (r * KroX[dim] + a * KroY[dim]) / (r * r + a * a) + FactorX * delR[dim];
        }
        float FactorY = (pos.z * (r * r + a * a) - 2 * r * (r * pos.z - a * pos.y)) / (Mathf.Pow(r * r + a * a, 2));
        for (int dim = 1; dim < 4; dim++)
        {
            delLY[dim] = (r * KroY[dim] - a * KroX[dim]) / (r * r + a * a) + FactorY * delR[dim];
        }
        for (int dim = 1; dim < 4; dim++)
        {
            delLZ[dim] = KroZ[dim] / r - pos.w / (r * r) * delR[dim];
        }
        Vector4[] Output = new Vector4[4] { delLT, delLX, delLY, delLZ };
        return Output;

    }

    #endregion

    public float CalcR2(Vector4 pos, float a)
    {
        float rho2 = pos.y * pos.y + pos.z * pos.z + pos.w * pos.w;
        return 0.5f * (rho2 - Mathf.Pow(a, 2) + Mathf.Sqrt(Mathf.Pow(rho2 - Mathf.Pow(a, 2), 2) + 4 * Mathf.Pow(a, 2) * Mathf.Pow(pos.w, 2)));
    }

    Vector4 CalcDelR2(Vector4 pos, float a)
    {
        float x = pos.y;
        float y = pos.z;
        float z = pos.w;
        float a2 = a * a;
        float rho2 = x * x + y * y + z * z;
        float delta = Mathf.Pow(rho2 - a2, 2) + 4f * a2 * z * z;
        float sqrtDelta = Mathf.Sqrt(delta);

        float delR2X = 0.5f * (2f * x + (2f * (rho2 - a2) * x) / sqrtDelta);
        float delR2Y = 0.5f * (2f * y + (2f * (rho2 - a2) * y) / sqrtDelta);
        float delR2Z = 0.5f * (2f * z + (2f * (rho2 - a2) * z + 4f * a2 * z) / sqrtDelta);

        float delR2T = 0f;
        return new Vector4(delR2T, delR2X, delR2Y, delR2Z);
    }

    Vector4 CalcDelR(Vector4 pos, float a)
    {
        Vector4 delR2 = CalcDelR2(pos, a);
        float R2 = CalcR2(pos, a);
        float r = Mathf.Sqrt(R2);
        Vector4 delR = new Vector4(0, delR2.y / (2 * r), delR2.z / (2 * r), delR2.w / (2 * r));
        return delR;
    }


    #endregion

    #endregion

    #endregion

    #region Helpers
    public Matrix4x4 MatrixAdd(Matrix4x4 m1, Matrix4x4 m2)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = m1[i, j] + m2[i, j];
            }
        }
        return result;
    }

    public Matrix4x4 MatrixSub(Matrix4x4 m1, Matrix4x4 m2)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = m1[i, j] - m2[i, j];
            }
        }
        return result;
    }

    public Matrix4x4[] matrix4X4ArrayAdd(Matrix4x4[] m1, Matrix4x4[] m2)
    {
        Matrix4x4[] result = new Matrix4x4[4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    result[i][j, k] = m1[i][j, k] + m2[i][j, k];
                }
            }
        }
        return result;
    }

    public Matrix4x4[] matrix4X4ArraySub(Matrix4x4[] m1, Matrix4x4[] m2)
    {
        Matrix4x4[] result = new Matrix4x4[4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    result[i][j, k] = m1[i][j, k] - m2[i][j, k];
                }
            }
        }
        return result;
    }

    public float TensorArrayMagnitude(Matrix4x4[] m)
    {
        float sum = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    sum += m[i][j, k];
                }
            }
        }
        return sum;
    }

    public Matrix4x4 MatrixScalarMul(Matrix4x4 m, float s)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = m[i, j] * s;
            }
        }
        return result;
    }

    public Matrix4x4 OuterProduct(Vector4 v1, Vector4 v2)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = v1[i] * v2[j];
            }
        }
        return result;
    }

    public Vector4 MultVec4EntryByEntry(Vector4 vec1, Vector4 vec2)
    {
        Vector4 res = new Vector4(0, 0, 0, 0);
        for (int i = 0; i < 4; i++)
        {
            res[i] = vec1[i] * vec2[i];
        }
        return res;
    }

    public Vector4 Vec324(Vector3 vec3) //peak Name
    {
        return new Vector4(vec3.x, vec3.y, vec3.z, 0);
    }

    public Vector3 Vec423(Vector4 vec4)
    {
        return new Vector3(vec4.x, vec4.y, vec4.z);
    }

    void DeleteAllChildren(GameObject parent) //not written by me
    {
        // Iterate backwards to be safe
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.transform.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    public Vector4 txyz2xyzw(Vector4 v)
    {
        return new Vector4(v.y, v.z, v.w, v.x);
    }

    public Vector4 xyzw2txyz(Vector4 v)
    {
        return new Vector4(v.w, v.x, v.y, v.z);
    }

    #endregion

    #region Camera

    LightRay instantiateRay(Vector4 CamPos, float FOV, float Width, float Height, Vector2 pos, Matrix4x4 rotatedlocalTetrad) //Height and Widht = n of Pixels on screen, pos = pos of this specific one
    {

        //normalize Coordinates
        float aspectRatio = Width / Height;
        float u = ((pos.x + 0.5f) / Width - 0.5f) * 2f; //o.5 to sample the middle of the pixel and not the corner
        float v = ((pos.y + 0.5f) / Height - 0.5f) * 2f;
        float alpha = Mathf.Tan(FOV / 2) * u * aspectRatio;
        float beta = Mathf.Tan(FOV / 2) * v;
        Vector4 a = new Vector4(0, alpha, beta, 1);
        float A = Mathf.Sqrt(alpha * alpha + beta * beta + 1);
        a[0] = A;
        Vector4 k = Vector4.zero;
        for (int i = 0; i < 4; i++)
        {
            float sum = 0;
            for (int j = 0; j < 4; j++)
            {
                sum += rotatedlocalTetrad[i, j] * a[j];
            }
            k[i] = sum;
        }
        LightRay lightRay = new LightRay(CamPos, k);
        return lightRay;
    }

    public Matrix4x4 localTetrad(Vector4 pos)
    {
        Vector4 u = new Vector4(1 / Mathf.Sqrt(-MetricTensorAtCam[0, 0]), 0, 0, 0);
        Vector4 v1 = new Vector4(0, 1, 0, 0);
        Vector4 v2 = new Vector4(0, 0, 1, 0);
        Vector4 v3 = new Vector4(0, 0, 0, 1);
        Matrix4x4 v = new Matrix4x4(u, v1, v2, v3);
        Matrix4x4 sqv = Matrix4x4.zero; //squiggely v~
        for (int BaseVector = 1; BaseVector < 4; BaseVector++)
        {
            for (int Component = 0; Component < 4; Component++)
            {
                float Q = 0;
                float D = 0;
                for (int alpha = 0; alpha < 4; alpha++)
                {
                    for (int beta = 0; beta < 4; beta++)
                    {
                        Q += MetricTensorAtCam[alpha, beta] * v[alpha, BaseVector] * u[beta];
                        D += MetricTensorAtCam[alpha, beta] * u[alpha] * u[beta];
                    }
                }
                sqv[Component, BaseVector] = v[Component, BaseVector] - Q / D * u[Component]; //check if indeces are the right way around
            }
        }
        Vector4 e0 = u;
        Vector4 e1 = new Vector4();
        Vector4 e2 = new Vector4();
        Vector4 e3 = new Vector4();
        Vector4 sqvs2 = new Vector4(); //squiggely v strich: ~v'_2
        Vector4 sqvs3 = new Vector4(); //squiggely v strich ~v'_3
        for (int dim = 0; dim < 4; dim++) //calc e1
        {
            float sum = 0;
            for (int alpha = 0; alpha < 4; alpha++)
            {
                for (int beta = 0; beta < 4; beta++)
                {
                    sum += MetricTensorAtCam[alpha, beta] * sqv[alpha, 1] * sqv[beta, 1];
                }
            }
            e1[dim] = sqv[dim, 1] / Mathf.Sqrt(sum);
        }
        for (int dim = 0; dim < 4; dim++) //calc sqvs2
        {
            float sum = 0;
            for (int alpha = 0; alpha < 4; alpha++)
            {
                for (int beta = 0; beta < 4; beta++)
                {
                    sum += MetricTensorAtCam[alpha, beta] * sqv[alpha, 2] * e1[beta];
                }
            }
            sqvs2[dim] = sqv[dim, 2] - sum * e1[dim];
        }
        for (int dim = 0; dim < 4; dim++)
        {
            float sum = 0;
            for (int alpha = 0; alpha < 4; alpha++)
            {
                for (int beta = 0; beta < 4; beta++)
                {
                    sum += MetricTensorAtCam[alpha, beta] * sqvs2[alpha] * sqvs2[beta];
                }
            }
            e2[dim] = sqvs2[dim] / Mathf.Sqrt(sum);
        }
        for (int dim = 0; dim < 4; dim++)
        {
            float sum1 = 0;
            float sum2 = 0;
            for (int alpha = 0; alpha < 4; alpha++)
            {
                for (int beta = 0; beta < 4; beta++)
                {
                    sum1 += MetricTensorAtCam[alpha, beta] * sqv[alpha, 3] * e1[beta];
                    sum2 += MetricTensorAtCam[alpha, beta] * sqv[alpha, 3] * e2[beta];
                }
            }
            sqvs3[dim] = sqv[dim, 3] - sum1 * e1[dim] - sum2 * e2[dim];
        }
        for (int dim = 0; dim < 4; dim++)
        {
            float sum = 0;
            for (int alpha = 0; alpha < 4; alpha++)
            {
                for (int beta = 0; beta < 4; beta++)
                {
                    sum += MetricTensorAtCam[alpha, beta] * sqvs3[alpha] * sqvs3[beta];
                }
            }
            e3[dim] = sqvs3[dim] / Mathf.Sqrt(sum);
        }
        Matrix4x4 e = new Matrix4x4(e0, e1, e2, e3);
        return e;

    }

    public Matrix4x4 rotateLocalTetrad(Matrix4x4 Tetrad, Vector3 Angles)
    {
        Matrix4x4 Rtest = Matrix4x4.Rotate(Quaternion.Euler(Angles));  // :skull: ... why did i listen to ChatGPT and deactivate my brain and write down all the rotation matrices myself
        Matrix4x4 R = Matrix4x4.zero;
        for (int i = 1; i < 4; i++)
        {
            for (int j = 1; j < 4; j++)
            {
                R[i, j] = Rtest[i - 1, j - 1];
            }
        }
        R[0, 0] = 1; //do not disturb time
        Matrix4x4 e = new Matrix4x4();
        for (int mu = 0; mu < 4; mu++)
        {
            for (int a = 0; a < 4; a++)
            {
                float sum = 0;
                for (int b = 0; b < 4; b++)
                {
                    sum += Tetrad[mu, b] * R[b, a];
                }
                e[mu, a] = sum;
            }
        }
        return e;
    }

    #endregion

    #region Rendering

    Color TraceRay(LightRay start)
    {
        LightRay nextPos = start;
        LightRay prevPos = start;
        for (int step = 0; step < maxSteps; step++)
        {
            prevPos = nextPos;
            precision = StepSize;
            float r = CalcR(nextPos.pos, a);
            precision = StepSize;
            Debug.Log(r - 2 * M);
            if (r - 2 * M < margin)
            {
                precision = preciseSteps;
                step--;//dummest fix ever... If light is close to black hole bugs are common so step size is drasctly reduced which led to light running out of sim steps, this makes it go on infinte as long it is in the margin... Nothing could ever go wrong here..... suttle foreshadowing
            }
            Debug.Log("precision" + precision);
            nextPos = StepLight(nextPos, precision); //calculate nextPosition
            Vector4 Hit = HitObject(nextPos.pos, prevPos.pos);
            if (Hit.w != 0)
            {
                Color PixelColor = new Color(Hit.x / 255, Hit.y / 255, Hit.z / 255);
                return PixelColor;
            }
        }
        return new Color(0, 1, 0, 0);
    }

    Texture2D RenderTexture()
    {
        CalcMetricTensor(CameraPosition, CalcR(CameraPosition, a), a, M); //Calculate Metric Tensor at Camera Position
        MetricTensorAtCam = MetricTensor;

        localTetradAtCam = localTetrad(CameraPosition);
        localTetradAtCam = rotateLocalTetrad(localTetradAtCam, CameraRotation);
        CamTexture = new Texture2D(MonitorSize.x, MonitorSize.y, TextureFormat.RGBA32, false);
        CamTexture.filterMode = FilterMode.Point;   // crisp pixels
        CamTexture.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < MonitorSize.x; x++)
        {
            for (int y = 0; y < MonitorSize.y; y++)
            {
                LightRay Ray = instantiateRay(CameraPosition, FOV, MonitorSize.x, MonitorSize.y, new Vector2(x, y), localTetradAtCam);
                Color color = TraceRay(Ray);
                if (color == new Color(0, 1, 0))
                    Debug.Log("error at: x: " + x + "y: " + y);
                CamTexture.SetPixel(x, y, color);
            }
        }
        Debug.Log("finished Rendering");
        return CamTexture;
    }

    public void DrawTexture(Texture2D tex, Material targetMaterial, int width, int height) //couldn't get working so I ask ChatGPT for minimal working example and made it fit
    {
        tex.Apply();
        targetMaterial.mainTexture = tex;
    }
    public void TestCamera()
    {
        BlackHole.transform.localScale = Vector3.one * 4 * M; //set test Black hole to apprx size
        CalcMetricTensor(CameraPosition, CalcR(CameraPosition, a), a, M); //Calculate Metric Tensor at Camera Position
        MetricTensorAtCam = MetricTensor;

        localTetradAtCam = localTetrad(CameraPosition); //old version
        localTetradAtCam = rotateLocalTetrad(localTetradAtCam, CameraRotation);
        //localTetradAtCam = CameraTetradV3(); //new Version

        //Debug.DrawLine(txyz2xyzw(CameraPosition), txyz2xyzw(CameraPosition + 5 * localTetradAtCam.GetColumn(1)), Color.red); //Draw local tetrad
        //Debug.DrawLine(txyz2xyzw(CameraPosition), txyz2xyzw(CameraPosition + 5 * localTetradAtCam.GetColumn(2)), Color.green);
        //Debug.DrawLine(txyz2xyzw(CameraPosition), txyz2xyzw(CameraPosition + 5 * localTetradAtCam.GetColumn(3)), Color.blue);
        //Debug.Log("--------------------------------------");
        //Debug.Log("Tetrad:");
        //Debug.Log("x" + localTetradAtCam.GetColumn(1)); //log Tetrad
        //Debug.Log("y" + localTetradAtCam.GetColumn(2));
        //Debug.Log("z" + localTetradAtCam.GetColumn(3));
        //CopyMatrix(localTetradAtCam);
        //Debug.Log("--------------------------------------");
        //Debug.Log("Metric");
        //LogTensor(MetricTensorAtCam);
        //Debug.Log("--------------------------------------");
        //Debug.Log("C:");
        //LogTensor(C(localTetradAtCam));
        //Debug.Log("--------------------------------------");
        //Debug.Log("e00:");
        //Debug.Log(localTetradAtCam[0, 0]);
        if (focusOne)
        {
            LightRay Ray = instantiateRay(CameraPosition, FOV, MonitorSize.x, MonitorSize.y, new Vector2(rayX, rayY), localTetradAtCam); //put together later
            Color colour = new Color((float)rayY / (float)MonitorSize.x, (float)rayY / (float)MonitorSize.y, 0);
            StepLightNTimes(Ray, N, colour);
        }
        else
        {
            for (int x = 0; x < MonitorSize.x; x++)
            {
                for (int y = 0; y < MonitorSize.y; y++)
                {
                    LightRay lightRay;
                    lightRay = instantiateRay(CameraPosition, FOV, MonitorSize.x, MonitorSize.y, new Vector2(x, y), localTetradAtCam);
                    Color colour = new Color((float)x / (float)MonitorSize.x, (float)y / (float)MonitorSize.y, 0, 1);
                    //Debug.DrawLine(txyz2xyzw(lightRay.pos), txyz2xyzw(lightRay.pos + 1000 * lightRay.k), colour, 100);
                    StepLightNTimes(lightRay, N, colour);
                }
            }
        }
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        if (Render)
        {
            DeleteAllChildren(Parent);
            TestCamera();
            Render = false;
        }
    }
}
