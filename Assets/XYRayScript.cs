using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class XYRayScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public List<KMSelectable> buttons;
    public GameObject[] solids;
    public Transform rotsphere;
    public Transform translator;
    public Renderer[] leds;
    public Material[] io;

    private readonly int[,,] cube = new int[5, 5, 5]   { 
    { { 2, 1, 3, 5, 1}, { 5, 3, 5, 4, 3}, { 3, 4, 3, 2, 5}, { 4, 2, 5, 4, 1}, { 1, 3, 4, 5, 2}},
    { { 4, 3, 5, 4, 2}, { 1, 2, 3, 1, 5}, { 5, 1, 2, 3, 1}, { 3, 5, 4, 5, 4}, { 4, 2, 5, 2, 1}},
    { { 2, 4, 3, 2, 3}, { 5, 3, 2, 3, 2}, { 4, 5, 1, 4, 5}, { 1, 2, 3, 1, 3}, { 2, 3, 1, 4, 5}},
    { { 4, 1, 2, 4, 2}, { 1, 4, 5, 2, 1}, { 2, 3, 2, 1, 4}, { 3, 1, 5, 2, 1}, { 1, 4, 2, 5, 4}},
    { { 3, 2, 1, 5, 4}, { 4, 3, 2, 1, 2}, { 1, 5, 4, 5, 1}, { 4, 2, 1, 3, 4}, { 2, 5, 3, 1, 5}}};
    private readonly string[,,] snames = new string[3, 3, 3]
    {   { { "Herschel Enneahedron", "Spherical Cone", "Rhombic Triacontahedron"}, { "Oblate Spheroid", "Dodecahedron", "Rhombohedron"}, { "Rhombicuboctahderon", "Trapezohedron", "Bicone"} },
        { { "Pentagonal Prism", "Octahedron", "Cylinder"}, { "Tetrahedron", "Sphere", "Rhombic Dodecahedron"}, { "Cone", "Cube", "Triangular Bipyramid"} },
        { { "Bicylinder", "Gyrobifastigium", "Icosidodecahedron"}, { "Cuboctahedron", "Icosahedron", "Prolate Spheroid"}, { "Gyroelongated Square Bipyramid", "Spherical Wedge", "Rhombotriangular Dodecahedron"} },    };
    private int[] sindex = new int[3];
    private List<int> gridgits = new List<int> { 0, 0, 0};
    private int entry;
    private int stage;
    private int locked = -1;
    private bool reverse;
    private Vector3[] rots = new Vector3[3];

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        foreach (GameObject s in solids)
            s.SetActive(false);
        leds[1].material = io[1];
        for (int i = 0; i < 3; i++)
            sindex[i] = Random.Range(0, 27);
        module.OnActivate = Activate;
    }
    private void Activate()
    { 
        int[] sn = info.GetSerialNumberNumbers().ToArray();
        reverse = sn[0] > sn[1];
        for (int i = 0; i < 3; i++) {
            int s = sindex[i];
            int s2 = sindex[(i + (reverse ? 2 : 1)) % 3];
            rots[i] = new Vector3(Random.Range(0, 360f), Random.Range(0, 360f), Random.Range(0, 360f));
            int[] off = new int[3] { s / 9, (s / 3) % 3, s % 3 };
            gridgits[i] = cube[off[0] + 1, off[1] + 1, off[2] + 1] * 10;
            off = new int[3] { s2 / 9, (s2 / 3) % 3, s2 % 3 }.Select((x, j) => x + off[j]).ToArray();
            gridgits[i] += cube[off[0], off[1], off[2]];
        }
        Debug.LogFormat("[XY-Ray #{0}] The scanned shapes are: {1}", moduleID, string.Join(", ", Enumerable.Range(0, 3).Select(x => snames[sindex[x] / 9, (sindex[x] / 3) % 3, sindex[x] % 3] + " (" + "-0+"[sindex[x] / 9] + "-0+"[(sindex[x] / 3) % 3] + "-0+"[sindex[x] % 3] + ")").ToArray()));
        Debug.LogFormat("[XY-Ray #{0}] The full code is {1}. Enter pairs {2}.", moduleID, string.Join("-", gridgits.Select(i => i.ToString()).ToArray()), reverse ? "backwards" : "forwards");
        StartCoroutine("Scan");
        foreach (KMSelectable button in buttons)
        {
            int b = buttons.IndexOf(button);
            button.OnInteract = delegate ()
            {
                if (!moduleSolved)
                {
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, button.transform);
                    button.AddInteractionPunch(0.6f);
                    entry *= 10;
                    entry += b + 1;
                    if(entry > 10)
                    {
                        if(locked == -1)
                        {
                            if (gridgits.Contains(entry))
                            {
                                stage = 1;
                                locked = gridgits.IndexOf(entry);
                                locked += reverse ? 2 : 1;
                                locked %= 3;
                                Audio.PlaySoundAtTransform("One", transform);
                                leds[0].material = io[0];
                                Debug.LogFormat("[XY-Ray #{0}] {1} accepted. Enter {2} next.", moduleID, entry, gridgits[locked]);
                            }
                            else
                            {
                                module.HandleStrike();
                                Debug.LogFormat("[XY-Ray #{0}] {1} is not an accepted code.", moduleID, entry);
                            }
                        }
                        else
                        {
                            if (gridgits[locked] == entry)
                            {
                                stage++;
                                if (stage > 2)
                                {
                                    moduleSolved = true;
                                    module.HandlePass();
                                    StopCoroutine("Scan");
                                    for (int i = 0; i < 3; i++)
                                        solids[sindex[i]].SetActive(false);
                                    Audio.PlaySoundAtTransform("Three", transform);
                                    leds[2].material = io[0];
                                    Debug.LogFormat("[XY-Ray #{0}] {1} accepted.", moduleID, entry);
                                }
                                else
                                {
                                    locked += reverse ? 2 : 1;
                                    locked %= 3;
                                    Debug.LogFormat("[XY-Ray #{0}] {1} accepted. Enter {2} next.", moduleID, entry, gridgits[locked]);
                                    Audio.PlaySoundAtTransform("Two", transform);
                                    leds[1].material = io[0];
                                }
                            }
                            else
                            {
                                module.HandleStrike();
                                Debug.LogFormat("[XY-Ray #{0}] {1} is not the next required code.", moduleID, entry);
                            }
                        }
                        entry = 0;
                    }
                }
                return false;
            };
        }
    }

    private IEnumerator Scan()
    {
        float e = 0;
        while (!moduleSolved)
        {
            for(int i = 0; i < 3; i++)
            {
                solids[sindex[i]].SetActive(true);
                rotsphere.localEulerAngles = rots[i];
                while(e < 1)
                {
                    e += Time.deltaTime / 3;
                    translator.localPosition = new Vector3(0.0023f, Mathf.Lerp(-0.0665f, 0.0456f, e) ,0.0024f);
                    yield return null;
                }
                e = 0;
                solids[sindex[i]].SetActive(false);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
