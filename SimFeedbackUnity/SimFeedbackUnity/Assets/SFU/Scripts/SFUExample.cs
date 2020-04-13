using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SFUTelemetry;

public class SFUExample : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SimFeedbackUnity.Instance.SetTelemetryTransform(transform);

        SimFeedbackUnity.Instance.SetConnection("127.0.0.1", 4444);

        SimFeedbackUnity.Instance.Activate(true);
    }

}
