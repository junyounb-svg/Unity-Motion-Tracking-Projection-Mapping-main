using UnityEngine;
using System.IO.Ports;
using System;

/// <summary>
/// Attach this script to your bag object to control it with an Arduino Joystick Module.
/// Movement: left, right, forward, backward via serial data (X,Y) from the joystick.
/// When the Arduino is not connected, uses keyboard (WASD / Arrow keys) for testing.
/// </summary>
[RequireComponent(typeof(Transform))]
public class BagJoystickController : MonoBehaviour
{
    [Header("Serial / Arduino")]
    [Tooltip("Serial port name. Windows: COM3, COM4, etc. Mac: /dev/cu.usbmodem* or /dev/cu.usbserial*")]
    public string portName = "COM3";

    [Tooltip("Baud rate (must match Arduino Serial.begin)")]
    public int baudRate = 9600;

    [Tooltip("If true, script will try to auto-detect Arduino port on start")]
    public bool autoDetectPort = true;

    [Header("Movement")]
    [Tooltip("Speed of movement in units per second")]
    public float moveSpeed = 5f;

    [Tooltip("Use 2D movement (X,Y plane). If false, uses X and Z (top-down 3D)")]
    public bool use2DMovement = true;

    [Tooltip("Dead zone for joystick (0-1). Input below this is ignored to reduce jitter.")]
    [Range(0f, 0.5f)]
    public float deadZone = 0.15f;

    [Header("Optional bounds (0 = no limit)")]
    [Tooltip("Limit position X. 0 = no limit")]
    public float minX = 0f, maxX = 0f;

    [Tooltip("Limit position Y (2D) or Z (3D). 0 = no limit")]
    public float minY = 0f, maxY = 0f;

    SerialPort _serialPort;
    bool _serialReady;
    float _inputX, _inputY; // -1 to 1 from joystick
    string _lastLine = "";

    const int JoystickCenter = 512;   // Arduino analog 0-1023, center ~512
    const int JoystickRange = 512;   // max offset from center

    void Start()
    {
        _serialReady = TryOpenSerial();
        if (!_serialReady)
            Debug.Log("[BagJoystickController] Serial not available. Using keyboard (WASD / Arrows) for testing.");
    }

    bool TryOpenSerial()
    {
        if (autoDetectPort)
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string p in ports)
            {
                if (OpenPort(p)) return true;
            }
            return false;
        }

        return OpenPort(portName);
    }

    bool OpenPort(string port)
    {
        try
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();
            _serialPort = new SerialPort(port, baudRate);
            _serialPort.ReadTimeout = 50;
            _serialPort.Open();
            portName = port;
            Debug.Log("[BagJoystickController] Opened serial: " + port);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BagJoystickController] Could not open " + port + ": " + e.Message);
            return false;
        }
    }

    void Update()
    {
        if (_serialReady)
            ReadSerialInput();
        else
            ReadKeyboardInput();

        ApplyMovement();
    }

    void ReadSerialInput()
    {
        try
        {
            if (_serialPort != null && _serialPort.IsOpen && _serialPort.BytesToRead > 0)
            {
                _lastLine += _serialPort.ReadExisting();
                int idx = _lastLine.IndexOf('\n');
                if (idx >= 0)
                {
                    string line = _lastLine.Substring(0, idx).Trim();
                    _lastLine = _lastLine.Substring(idx + 1);
                    ParseJoystickLine(line);
                }
            }
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Debug.LogWarning("[BagJoystickController] Serial read error: " + e.Message);
            _serialReady = false;
        }
    }

    // Expects "x,y" from Arduino. x,y can be 0-1023 (raw) or -1.0 to 1.0 (normalized)
    void ParseJoystickLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        string[] parts = line.Split(',');
        if (parts.Length < 2) return;

        float x, y;
        if (float.TryParse(parts[0].Trim(), out x) && float.TryParse(parts[1].Trim(), out y))
        {
            // Normalize if raw Arduino values (0-1023)
            if (Mathf.Abs(x) <= 1023 && Mathf.Abs(y) <= 1023)
            {
                x = (x - JoystickCenter) / (float)JoystickRange;
                y = (y - JoystickCenter) / (float)JoystickRange;
            }
            _inputX = Mathf.Clamp(x, -1f, 1f);
            _inputY = Mathf.Clamp(y, -1f, 1f);
        }
    }

    void ReadKeyboardInput()
    {
        float x = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);
        float y = Input.GetKey(KeyCode.W) ? 1f : (Input.GetKey(KeyCode.S) ? -1f : 0f);
        if (x == 0f) x = Input.GetKey(KeyCode.RightArrow) ? 1f : (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f);
        if (y == 0f) y = Input.GetKey(KeyCode.UpArrow) ? 1f : (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);
        _inputX = x;
        _inputY = y;
    }

    void ApplyMovement()
    {
        float dx = _inputX;
        float dy = _inputY;

        if (Mathf.Abs(dx) < deadZone) dx = 0f;
        if (Mathf.Abs(dy) < deadZone) dy = 0f;

        Vector3 move = Vector3.zero;
        if (use2DMovement)
        {
            move.x = dx * moveSpeed * Time.deltaTime;
            move.y = dy * moveSpeed * Time.deltaTime;
        }
        else
        {
            move.x = dx * moveSpeed * Time.deltaTime;
            move.z = dy * moveSpeed * Time.deltaTime;
        }

        transform.position += move;

        if (maxX != minX || maxY != minY)
        {
            Vector3 p = transform.position;
            if (maxX != minX) p.x = Mathf.Clamp(p.x, minX, maxX);
            if (maxY != minY)
            {
                if (use2DMovement) p.y = Mathf.Clamp(p.y, minY, maxY);
                else p.z = Mathf.Clamp(p.z, minY, maxY);
            }
            transform.position = p;
        }
    }

    void OnDestroy()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            try { _serialPort.Close(); } catch { }
            _serialPort.Dispose();
        }
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}
