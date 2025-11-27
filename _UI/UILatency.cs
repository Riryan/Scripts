using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class UILatency : MonoBehaviour
{
    public Text latencyText;

    public float goodThreshold = 0.3f;
    public float okayThreshold = 2;

    public Color goodColor = Color.green;
    public Color okayColor = Color.yellow;
    public Color badColor = Color.red;

    void Update()
    {
        
        latencyText.enabled = NetworkClient.isConnected;

        
        if (NetworkTime.rtt <= goodThreshold)
            latencyText.color = goodColor;
        else if (NetworkTime.rtt <= okayThreshold)
            latencyText.color = okayColor;
        else
            latencyText.color = badColor;

        
        latencyText.text = Mathf.Round((float)NetworkTime.rtt * 1000) + "ms";
    }
}
