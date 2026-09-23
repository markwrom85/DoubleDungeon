using System;
using System.Diagnostics;
#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
using System.IO.Ports;
using System.Threading;
#endif

// No Unity objects are accessed by this worker. Each connection owns its cancellation flag.
internal sealed class ArduinoSerialConnection
{
    internal struct Snapshot
    {
        public int x, y;
        public bool fire, button2, switchSide;
        public long receivedAt;
        public string status;
    }
    private readonly object gate = new object();
    private readonly string portName;
    private Snapshot latest;
    private volatile bool stopping;
#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
    private Thread thread;
    public bool HasStopped => thread == null || !thread.IsAlive;
#else
    public bool HasStopped => true;
#endif
    public ArduinoSerialConnection(string portName) { this.portName = portName; latest.status = "Opening " + portName; }
    public Snapshot ReadSnapshot() { lock (gate) return latest; }
    public void Start()
    {
#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
        thread = new Thread(Read) { IsBackground = true, Name = "Arduino " + portName };
        thread.Start();
#else
        latest.status = "Serial requires desktop API Compatibility Level: .NET Framework";
#endif
    }
    public void Stop()
    {
        bool firstStop = !stopping;
        stopping = true;
#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
        if (firstStop) thread?.Join(250);
#endif
        lock (gate) { latest.receivedAt = 0; latest.fire = latest.button2 = latest.switchSide = false; }
    }

    public static bool IsFresh(long stamp, float seconds)
    {
        return stamp != 0 && (Stopwatch.GetTimestamp() - stamp) / (double)Stopwatch.Frequency < Math.Max(0.1, seconds);
    }

    public static bool TryParse(string line, out int x, out int y, out bool fire)
    {
        return TryParse(line, out x, out y, out fire, out _, out _);
    }

    // x,y[,fire[,button2[,switchSide]]]; missing buttons are released.
    public static bool TryParse(string line, out int x, out int y, out bool fire, out bool button2, out bool switchSide)
    {
        x = y = 0; fire = button2 = switchSide = false;
        if (string.IsNullOrEmpty(line)) return false;
        string[] parts = line.Split(',');
        if ((parts.Length < 2 || parts.Length > 5) || !int.TryParse(parts[0], out x)
            || !int.TryParse(parts[1], out y) || x < 0 || x > 1023 || y < 0 || y > 1023) return false;
        if (parts.Length == 2) return true;
        if (!int.TryParse(parts[2], out int button) || (button != 0 && button != 1)) return false;
        fire = button == 1;
        if (parts.Length >= 4)
        {
            if (!int.TryParse(parts[3], out int second) || (second != 0 && second != 1)) return false;
            button2 = second == 1;
        }
        if (parts.Length == 5)
        {
            if (!int.TryParse(parts[4], out int third) || (third != 0 && third != 1)) return false;
            switchSide = third == 1;
        }
        return true;
    }

#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
    private void Read()
    {
        try
        {
            using (var port = new SerialPort(portName, 115200) { ReadTimeout = 100, DtrEnable = true })
            {
                if (stopping) return;
                port.Open();
                lock (gate) latest.status = "Connected to " + portName + "; waiting for data";
                string pending = "";
                bool discard = false;
                while (!stopping)
                {
                    int value;
                    try { value = port.ReadChar(); } catch (TimeoutException) { continue; }
                    if (value < 0) continue;
                    if (value != '\n')
                    {
                        if (!discard) pending += (char)value;
                        if (pending.Length > 64) { pending = ""; discard = true; }
                        continue;
                    }
                    if (!discard && TryParse(pending, out int x, out int y, out bool fire, out bool button2, out bool switchSide))
                    {
                        lock (gate) latest = new Snapshot { x = x, y = y, fire = fire, button2 = button2, switchSide = switchSide,
                            receivedAt = Stopwatch.GetTimestamp(), status = "Receiving on " + portName };
                    }
                    pending = ""; discard = false;
                }
            }
        }
        catch (Exception exception)
        {
            lock (gate) { latest.receivedAt = 0; latest.fire = latest.button2 = latest.switchSide = false; latest.status = portName + ": " + exception.Message; }
        }
    }
#endif
}
