using UnityEngine;
using System.IO;
using System.Text;
using uMMORPG;
public class ServerStateLogger : MonoBehaviour
{
    public float logInterval = 10f;
    string path;

    void Start()
    {
        path = Path.Combine(
            Application.persistentDataPath,
            "server_soak_log.csv"
        );

        File.WriteAllText(path,
            "time,players,memMB,fps\n"
        );

        InvokeRepeating(nameof(LogState), logInterval, logInterval);
    }

    void LogState()
    {
        long mem = System.GC.GetTotalMemory(false) / (1024 * 1024);
        int players = Player.onlinePlayers.Count;
        float fps = 1f / Time.deltaTime;

        File.AppendAllText(path,
            $"{Time.time:F0},{players},{mem},{fps:F1}\n"
        );
    }
}
