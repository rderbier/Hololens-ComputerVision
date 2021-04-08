using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveLine : MonoBehaviour
{
    public GameObject line;
    public GameObject background;
    public float velocity = 0.1f;
    private bool started = false;
    // Start is called before the first frame update
    void Start()
    {
        line.SetActive(false);
        background.SetActive(false);

    }
    public void startScanAnimation()
    {
        line.SetActive(true);
        background.SetActive(true);
        started = true;

    }
    public void stopScanAnimation()
    {
        line.SetActive(false);
        background.SetActive(false);
        started = false;

    }

    // Update is called once per frame
    void Update()
    {
        if (started)
        {
            line.transform.Translate(Vector3.up * velocity * Time.deltaTime);

            if (System.Math.Abs(line.transform.localPosition.y) >= 0.5f)
            {
                velocity = -1f * velocity;
            }
        }

    }
}
