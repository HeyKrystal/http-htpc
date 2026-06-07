using System.Net;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting.WindowsServices;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "http-htpc";
});

builder.Services.AddHostedService<HttpHtpcService>();

var host = builder.Build();
host.Run();

public sealed class HttpHtpcService : BackgroundService
{
    // CHANGE THESE
    private const string ListenPrefix = "http://+:8787/";
    private const string SecretToken = "GENERIC_TOKEN";

    private readonly ILogger<HttpHtpcService> _logger;

    public HttpHtpcService(ILogger<HttpHtpcService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(ListenPrefix);
        listener.Start();

        _logger.LogInformation("Http-Htpc listening on {Prefix}", ListenPrefix);

        while (!stoppingToken.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await listener.GetContextAsync().WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequest(context), stoppingToken);
        }

        listener.Stop();
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            if (context.Request.HttpMethod != "GET")
            {
                Write(context, 405, "GET only");
                return;
            }

            var token = context.Request.QueryString["token"];
            if (token != SecretToken)
            {
                Write(context, 401, "Unauthorized");
                return;
            }

            var path = context.Request.Url?.AbsolutePath.Trim('/').ToLowerInvariant();

            switch (path)
            {
                case "playpause":
                    Keyboard.SendMediaKey(VirtualKey.MEDIA_PLAY_PAUSE);
                    break;

                case "next":
                    Keyboard.SendMediaKey(VirtualKey.MEDIA_NEXT_TRACK);
                    break;

                case "previous":
                    Keyboard.SendMediaKey(VirtualKey.MEDIA_PREV_TRACK);
                    break;

                case "left":
                    Keyboard.SendMediaKey(VirtualKey.LEFT_ARROW);
                    break;

                case "right":
                    Keyboard.SendMediaKey(VirtualKey.RIGHT_ARROW);
                    break;

                case "stop":
                    Keyboard.SendMediaKey(VirtualKey.MEDIA_STOP);
                    break;

                case "volume-up":
                    Keyboard.SendMediaKey(VirtualKey.VOLUME_UP);
                    break;

                case "volume-down":
                    Keyboard.SendMediaKey(VirtualKey.VOLUME_DOWN);
                    break;

                case "mute":
                    Keyboard.SendMediaKey(VirtualKey.VOLUME_MUTE);
                    break;

                default:
                    Write(context, 404, "Unknown command");
                    return;
            }

            Write(context, 200, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed");
            Write(context, 500, "Error");
        }
    }

    private static void Write(HttpListenerContext context, int statusCode, string text)
    {
        context.Response.StatusCode = statusCode;
        using var writer = new StreamWriter(context.Response.OutputStream);
        writer.Write(text);
    }
}

public static class Keyboard
{
    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static void SendMediaKey(ushort virtualKey)
    {
        var inputs = new INPUT[2];

        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].ki.wVk = virtualKey;

        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].ki.wVk = virtualKey;
        inputs[1].ki.dwFlags = KEYEVENTF_KEYUP;

        var inputSize = Marshal.SizeOf<INPUT>();
        var sent = SendInput((uint)inputs.Length, inputs, inputSize);

        if (sent != inputs.Length)
        {
            var error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error, $"SendInput failed. Sent {sent} of {inputs.Length} input events. INPUT size was {inputSize}.");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint nInputs,
        INPUT[] pInputs,
        int cbSize
    );

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public int type;

        [FieldOffset(8)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }
}

public static class VirtualKey
{
    public const ushort MEDIA_NEXT_TRACK = 0xB0;
    public const ushort MEDIA_PREV_TRACK = 0xB1;
    public const ushort LEFT_ARROW = 0x25;
    public const ushort RIGHT_ARROW = 0x27;
    public const ushort MEDIA_STOP = 0xB2;
    public const ushort MEDIA_PLAY_PAUSE = 0xB3;
    public const ushort VOLUME_MUTE = 0xAD;
    public const ushort VOLUME_DOWN = 0xAE;
    public const ushort VOLUME_UP = 0xAF;
}